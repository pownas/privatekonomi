using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable IDE0057 // Use range operator

// 1. Definitiera sökvägar i utdatamappen
string lockFilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectLockfiles");
string propsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Directory.Packages.props");
string markdownReportPath = GetWikiOutputPath("sbom-report.md");
var extractedPackages = new List<NuGetPackage>();

// Kontroll om man vill se förhandsversioner av NuGet-paket eller ej
bool includeNugetPrerelease = false;
Console.WriteLine($"Inkludera förhandsversioner (Preview): {includeNugetPrerelease}");

// 2. Kontrollera om mappen med lock-filer existerar
if (Directory.Exists(lockFilesFolder))
{
    var lockFiles = Directory.GetFiles(lockFilesFolder, "packages.lock.json", SearchOption.AllDirectories);

    foreach (var file in lockFiles)
    {
        string projectName = Path.GetFileName(Path.GetDirectoryName(file) ?? "Okänt Projekt");
        Console.WriteLine($"Läser in lock-fil för: {projectName}");

        string jsonContent = await File.ReadAllTextAsync(file).ConfigureAwait(true);
        var lockFile = JsonSerializer.Deserialize<LockFile>(jsonContent);

        if (lockFile?.Dependencies is null)
        {
            continue;
        }

        foreach (var framework in lockFile.Dependencies)
        {
            // --- NYTT: Bygg en invers tabell över vem som beror på vem ---
            // Key: Det beroende paketet (transitiva), Value: Lista på paket som drog in det
            var dependentOnTracker = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var package in framework.Value)
            {
                if (package.Value.Dependencies != null)
                {
                    foreach (var childDep in package.Value.Dependencies)
                    {
                        if (!dependentOnTracker.TryGetValue(childDep.Key, out List<string>? value))
                        {
                            value = [];
                            dependentOnTracker[childDep.Key] = value;
                        }

                        value.Add(package.Key);
                    }
                }
            }
            // -------------------------------------------------------------

            foreach (var package in framework.Value)
            {
                if (package.Value.Type == "Project" || string.IsNullOrEmpty(package.Value.ContentHash))
                {
                    continue;
                }

                string packageName = package.Key;
                string packageVersion = package.Value.Resolved ?? "Okänd";

                // --- NYTT: Hitta rötterna (Direct dependencies) som ledde hit ---
                var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (package.Value.Type == "Direct")
                {
                    roots.Add("[Direkt SDK-beroende]");
                }
                else
                {
                    // Hitta alla Direct-paket som i slutändan leder till detta transitiva paket
                    FindDirectRoots(packageName, framework.Value, dependentOnTracker, roots, []);
                }

                string introducedByString = roots.Count > 0 ? string.Join(", ", roots) : "Okänd källa";
                // -------------------------------------------------------------

                // Kontrollera om paketet redan har lagts till i listan (t.ex. från ett annat projekt)
                var existing = extractedPackages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase) && p.Version == packageVersion);

                if (existing == null)
                {
                    // FIXAT: Ändrat till package.Value.ContentHash
                    var nuGetPkg = new NuGetPackage(packageName, packageVersion, package.Value.ContentHash)
                    {
                        IntroducedBy = introducedByString
                    };
                    extractedPackages.Add(nuGetPkg);
                }
                else
                {
                    // Om paketet redan finns men dras in av andra källor i detta projekt/ramverk,
                    // så slår vi ihop dem till en unik lista.
                    if (existing.IntroducedBy != "[Direkt SDK-beroende]" && introducedByString != "[Direkt SDK-beroende]")
                    {
                        var nuSources = existing.IntroducedBy.Split(", ").Concat(roots).Distinct();
                        existing.IntroducedBy = string.Join(", ", nuSources);
                    }
                    else if (introducedByString == "[Direkt SDK-beroende]")
                    {
                        // Om det var transitivt i ett projekt men direkt i ett annat, markera det som direkt
                        existing.IntroducedBy = "[Direkt SDK-beroende]";
                    }
                }
            }
        }
    }
}

// 3. Läs in Directory.Packages.props
if (!File.Exists(propsFilePath))
{
    Console.WriteLine("Kunde inte hitta Directory.Packages.props i utdatamappen.");
    return;
}
else
{
    Console.WriteLine($"Läser in Directory.Packages.props (CPM).");
}

var doc = XDocument.Load(propsFilePath);
var centralPackages = doc.Descendants("PackageVersion")
    .Select(node => new
    {
        Id = node.Attribute("Include")?.Value ?? node.Attribute("Update")?.Value ?? "",
        Version = node.Attribute("Version")?.Value ?? ""
    })
    .Where(p => !string.IsNullOrWhiteSpace(p.Id))
    .ToList();

// Konfigurera HttpClient att automatiskt hantera GZIP/Deflate-komprimering
using var handler = new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
};
using var client = new HttpClient(handler);

Console.WriteLine("Hämtar Service Index från NuGet...");

// 4. Hämta NuGet V3 Service Index
var serviceIndexUrl = "https://api.nuget.org/v3/index.json";
var serviceIndex = await client.GetFromJsonAsync<ServiceIndex>(serviceIndexUrl).ConfigureAwait(true);

// Hämta RegistrationsBaseUrl för att kunna kolla senaste versioner live
var regResourceUrl = serviceIndex?.Resources
    .FirstOrDefault(r => r.Type.StartsWith("RegistrationsBaseUrl/3.6.0", StringComparison.OrdinalIgnoreCase))?.Id;

// 5. Hitta slutpunkten för VulnerabilityInfo i indexet
var vulnResourceUrl = serviceIndex?.Resources
    .FirstOrDefault(r => r.Type.StartsWith("VulnerabilityInfo", StringComparison.OrdinalIgnoreCase))?.Id;

if (vulnResourceUrl is null || regResourceUrl is null)
{
    Console.WriteLine("Kunde inte hitta nödvändiga NuGet-API-slutpunkter.");
    return;
}

// 6. Hämta fillistan
var vulnFiles = await client.GetFromJsonAsync<List<VulnerabilityFile>>(vulnResourceUrl).ConfigureAwait(true);

// 7. Definiera den saknade variabeln
string? baseJsonUrl = vulnFiles?.FirstOrDefault(f => f.Name == "base")?.Id;
if (baseJsonUrl is null)
{
    Console.WriteLine("Kunde inte hitta base.json.");
    return;
}

Console.WriteLine($"Laddar ner sårbarhetsdatabasen från: {baseJsonUrl}");

// 8. Ladda ner sårbarhetsdatabasen
var vulnerabilities = await client.GetFromJsonAsync<Dictionary<string, List<Vulnerability>>>(baseJsonUrl).ConfigureAwait(true);

// 9. Hämta senaste versioner för alla unika paket parallellt
Console.WriteLine($"{Environment.NewLine}Hämtar senaste versioner från NuGet live-API...");
var uniqueLookups = extractedPackages
    .Select(p => new { Name = p.Name, Lower = p.Name.ToLowerInvariant() })
    .DistinctBy(p => p.Lower)
    .ToList();

var lookupTasks = uniqueLookups.Select(async item =>
{
    string latestVersion = "Okänd";
    try
    {
        var packageRegUrl = $"{regResourceUrl.TrimEnd('/')}/{item.Lower}/index.json";
        var regIndex = await client.GetFromJsonAsync<NugetRegistrationIndex>(packageRegUrl).ConfigureAwait(false);

        if (regIndex?.Pages != null && regIndex.Pages.Count > 0)
        {
            // Vi börjar leta i den senaste sidan (oftast den sista, men vi kan loopa bakifrån för säkerhetsskull)
            for (int p = regIndex.Pages.Count - 1; p >= 0; p--)
            {
                var page = regIndex.Pages[p];
                if (page.Items == null || page.Items.Count == 0)
                {
                    continue;
                }

                // Leta bakifrån bland paketen på den sidan (senaste först)
                for (int i = page.Items.Count - 1; i >= 0; i--)
                {
                    string currentVersion = page.Items[i].CatalogEntry.Version;

                    // Om 'includePrerelease' är true tar vi första bästa (vilket är den absolut senaste).
                    // Om den är false, godkänner vi bara versioner som INTE innehåller ett bindestreck.
                    if (includeNugetPrerelease || (!currentVersion.Contains('-') && !currentVersion.Contains('+')))
                    {
                        latestVersion = currentVersion;
                        break;
                    }
                }

                // Om vi hittade en version, avbryt sökningen efter äldre sidor
                if (latestVersion != "Okänd")
                {
                    break;
                }
            }
        }
    }
    catch
    {
        /* Ignorera API-missar, t.ex. interna paket */
    }

    return new { item.Name, LatestVersion = latestVersion };
});

var lookupResults = await Task.WhenAll(lookupTasks).ConfigureAwait(true);
var latestVersionsDict = lookupResults.ToDictionary(x => x.Name, x => x.LatestVersion, StringComparer.OrdinalIgnoreCase);

// 10. Processa och bygg data för rapporten
Console.WriteLine($"{Environment.NewLine}Alla versioner hämtade. Analyserar resultat...");

var reportData = new List<ReportItem>();
var groupedPackages = extractedPackages.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key);

foreach (var packageGroup in groupedPackages)
{
    string packageName = packageGroup.Key;
    string searchKey = packageName.ToLowerInvariant();
    bool hasVersionMismatch = packageGroup.Select(p => p.Version).Distinct().Count() > 1;

    var sortedPackageItems = packageGroup.OrderBy(p => p.Version);

    foreach (var pkg in sortedPackageItems)
    {
        var cpmMatch = centralPackages.FirstOrDefault(c => c.Id.Equals(pkg.Name, StringComparison.OrdinalIgnoreCase));
        string cpmStatus = cpmMatch != null ? $"Ja ({cpmMatch.Version})" : "Nej (Transitivt)";

        latestVersionsDict.TryGetValue(pkg.Name, out string? latestNuGetVersion);
        latestNuGetVersion ??= "Okänd";

        var activeVulnerabilities = new List<Vulnerability>();
        if (vulnerabilities!.TryGetValue(searchKey, out var vulnList))
        {
            foreach (var v in vulnList)
            {
                if (IsVersionAffected(pkg.Version, v.Versions))
                {
                    activeVulnerabilities.Add(v);
                }
            }
        }

        reportData.Add(new ReportItem(
            packageName,
            pkg.Version,
            latestNuGetVersion,
            cpmStatus,
            hasVersionMismatch,
            packageGroup.Select(p => p.Version).Distinct().ToList(),
            activeVulnerabilities,
            vulnList != null && vulnList.Count > 0,
            pkg.IntroducedBy
        ));
    }
}

// 11. Skicka utdata till de två olika kanalerna
PrintToConsole(reportData);
await GenerateMarkdownReportAsync(reportData, markdownReportPath).ConfigureAwait(true);

Console.WriteLine($"{Environment.NewLine}🚀 Skanning klar! Markdown-rapport genererad på: {markdownReportPath}");


#region Utmatningsmetod - Print to Console

static void PrintToConsole(List<ReportItem> items)
{
    Console.WriteLine(new string('=', 111));
    Console.WriteLine("📊 SBOM & SÅRBARHETSRAPPORT (KONSOL)");
    Console.WriteLine(new string('=', 111));

    // Visa konflikter först som en sammanställning
    var conflicts = items.Where(i => i.HasVersionMismatch).Select(i => i.PackageName).Distinct();
    if (conflicts.Any())
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️  FÖLJANDE PAKET HAR VERSIONSKONFLIKTER INOM LÖSNINGEN:");
        foreach (var conflictName in conflicts)
        {
            var versions = items.First(i => i.PackageName == conflictName).AllInstalledVersions;
            Console.WriteLine($"   - {conflictName}: Installerade versioner -> {string.Join(", ", versions)}");
        }
        Console.ResetColor();
        Console.WriteLine(new string('-', 111));
    }

    foreach (var item in items)
    {
        if (item.ActiveVulnerabilities.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CRITICAL] {item.PackageName} (v. {item.InstalledVersion})");
            Console.ResetColor();
            Console.WriteLine($"  -> Hanteras i CPM: {item.CpmStatus}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  -> Senaste live-version: {item.LatestVersion}");
            Console.ResetColor();
            Console.WriteLine($"  -> Aktiva sårbarheter ({item.ActiveVulnerabilities.Count} st):");

            foreach (var vuln in item.ActiveVulnerabilities)
            {
                var severityStr = vuln.Severity switch { 0 => "Låg", 1 => "Medel", 2 => "Hög", 3 => "Kritisk", _ => "Okänd" };
                Console.WriteLine($"     - [{severityStr}] | Intervall: {vuln.Versions} | Länk: {vuln.Url}");
            }
            Console.WriteLine(new string('-', 111));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[OK] ");
            Console.ResetColor();
            Console.Write($"{item.PackageName,-60} | Version: {item.InstalledVersion,-10} | CPM: {item.CpmStatus,-15}");

            if (item.IsSecuredOrPatched)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(" [Patchad historik]");
                Console.ResetColor();
            }

            if (item.LatestVersion != "Okänd" && item.LatestVersion != item.InstalledVersion)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($" [Uppdatering finns: {item.LatestVersion}]");
                Console.ResetColor();
            }
            Console.WriteLine();
        }
    }
}

#endregion

#region Utmatningsmetod - Generera Markdown-rapport

static async Task GenerateMarkdownReportAsync(List<ReportItem> items, string outputPath)
{
    var sb = new StringBuilder();

    sb.AppendLine("# 🛡️ Security & SBOM Scan Report");
    sb.AppendLine($"*Genererad den: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
    sb.AppendLine();

    // 1. Sammanfattning (KPI-Kort)
    int totalPackages = items.Count;
    int vulnerablePackages = items.Count(i => i.ActiveVulnerabilities.Count != 0);
    int versionMismatches = items.Where(i => i.HasVersionMismatch).Select(i => i.PackageName).Distinct().Count();
    int outdatedPackages = items.Count(i => i.LatestVersion != "Okänd" && i.LatestVersion != i.InstalledVersion && i.ActiveVulnerabilities.Count == 0);

    sb.AppendLine("## 📊 Översikt");
    sb.AppendLine("<table width=\"100%\">");
    sb.AppendLine("  <tr>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#f6f8fa; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #d0d7de;\"><b>Totalt skannade</b><br><font size=\"5\" color=\"#1f2328\">{totalPackages}</font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#ffebe9; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #ffc1c0;\"><b>⚠️ Aktiva sårbarheter</b><br><font size=\"5\" color=\"#cf222e\"><b>{vulnerablePackages}</b></font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#fff8c5; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #eee0b0;\"><b>🔄 Versionskonflikter</b><br><font size=\"5\" color=\"#9a6700\"><b>{versionMismatches}</b></font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#ddf4ff; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #b6e3ff;\"><b>📦 Outdaterade paket</b><br><font size=\"5\" color=\"#0969da\"><b>{outdatedPackages}</b></font></td>");
    sb.AppendLine("  </tr>");
    sb.AppendLine("</table>");
    sb.AppendLine();

    // 2. AKUTA ÅTGÄRDER: Tabell för Aktiva Sårbarheter (Om det finns några)
    if (vulnerablePackages > 0)
    {
        sb.AppendLine("## 🚨 Aktiva sårbarheter funna");
        sb.AppendLine("<table width=\"100%\">");
        sb.AppendLine("  <thead>");
        sb.AppendLine("    <tr>");
        sb.AppendLine("      <th align=\"left\">Paketnamn</th>");
        sb.AppendLine("      <th align=\"left\">Installerad</th>");
        sb.AppendLine("      <th align=\"left\">Senaste</th>");
        sb.AppendLine("      <th align=\"left\">Allvarlighetsgrad & Detaljer</th>");
        sb.AppendLine("    </tr>");
        sb.AppendLine("  </thead>");
        sb.AppendLine("  <tbody>");

        foreach (var item in items.Where(i => i.ActiveVulnerabilities.Count != 0))
        {
            sb.AppendLine("    <tr>");
            sb.AppendLine($"      <td><b>{item.PackageName}</b></td>");
            sb.AppendLine($"      <td><code style=\"color:#cf222e; background-color:#ffebe9; padding:2px 4px; border-radius:4px; border:none;\">{item.InstalledVersion}</code></td>");
            sb.AppendLine($"      <td><code style=\"color:#1f883d; background-color:#dafbe1; padding:2px 4px; border-radius:4px; border:none;\">{item.LatestVersion}</code></td>");
            sb.AppendLine("      <td>");

            foreach (var v in item.ActiveVulnerabilities)
            {
                string badgeColor = v.Severity switch { 3 => "#cf222e", 2 => "#bc4c00", 1 => "#9a6700", _ => "#57606a" };
                string severityText = v.Severity switch { 3 => "Kritisk", 2 => "Hög", 1 => "Medel", _ => "Låg" };

                sb.AppendLine($"        <div style=\"margin-bottom:6px;\">");
                sb.AppendLine($"          <span style=\"background-color:{badgeColor}; color:white; padding:2px 6px; border-radius:10px; font-size:11px; font-weight:bold;\">{severityText}</span>");
                sb.AppendLine($"          <span style=\"font-size:12px;\"> Berör intervall: <code>{v.Versions}</code></span> - <a href=\"{v.Url}\" target=\"_blank\">Visa Advisory</a>");
                sb.AppendLine($"        </div>");
            }

            sb.AppendLine("      </td>");
            sb.AppendLine("    </tr>");
        }
        sb.AppendLine("  </tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine();
    }

    // 3. AKUTA ÅTGÄRDER: Tabell för Versionskonflikter (Om det finns några)
    var conflictGroups = items
        .Where(i => i.HasVersionMismatch)
        .GroupBy(i => i.PackageName, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (conflictGroups.Count != 0)
    {
        sb.AppendLine("## 🔄 Versionskonflikter upptäckta");
        sb.AppendLine("<blockquote>💡 <b>Tips:</b> Standardisera dessa paket till en gemensam version i din <code>Directory.Packages.props</code> för att undvika oväntat runtime-beteende.</blockquote>");
        sb.AppendLine("<table width=\"100%\">");
        sb.AppendLine("  <thead>");
        sb.AppendLine("    <tr>");
        sb.AppendLine("      <th align=\"left\" width=\"40%\">Paketnamn</th>");
        sb.AppendLine("      <th align=\"left\" width=\"60%\">Installerade versioner</th>");
        sb.AppendLine("    </tr>");
        sb.AppendLine("  </thead>");
        sb.AppendLine("  <tbody>");

        foreach (var group in conflictGroups)
        {
            var uniqueVersions = group.Select(g => g.InstalledVersion).Distinct().OrderBy(v => v);
            var versionBadges = string.Join(" &nbsp;|&nbsp; ", uniqueVersions.Select(v => $"<code>{v}</code>"));

            sb.AppendLine("    <tr>");
            sb.AppendLine($"      <td><b>{group.Key}</b></td>");
            sb.AppendLine($"      <td>{versionBadges}</td>");
            sb.AppendLine("    </tr>");
        }
        sb.AppendLine("  </tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine();
    }

    // 4. Komplett Paketförteckning (SBOM)
    sb.AppendLine("## 📦 Komplett Paketförteckning (SBOM)");
    sb.AppendLine("<table width=\"100%\">");
    sb.AppendLine("  <thead>");
    sb.AppendLine("    <tr>");
    sb.AppendLine("      <th align=\"left\">Status</th>");
    sb.AppendLine("      <th align=\"left\">Paketnamn</th>");
    sb.AppendLine("      <th align=\"left\">Version</th>");
    sb.AppendLine("      <th align=\"left\">Senaste version</th>");
    sb.AppendLine("      <th align=\"left\">Hanteras i CPM?</th>");
    sb.AppendLine("    </tr>");
    sb.AppendLine("  </thead>");
    sb.AppendLine("  <tbody>");

    foreach (var item in items)
    {
        if (item.ActiveVulnerabilities.Count != 0)
        {
            continue;
        }

        string statusBadge = item.HasVersionMismatch
            ? "<span style=\"background-color:#fff8c5; color:#744210; padding:2px 6px; border-radius:4px; font-size:11px; font-weight:bold; display:inline-block;\">⚠️ Konflikt</span>"
            : item.LatestVersion != "Okänd" && item.LatestVersion != item.InstalledVersion
                ? "<span style=\"background-color:#ddf4ff; color:#0969da; padding:2px 6px; border-radius:4px; font-size:11px; font-weight:bold; display:inline-block;\">🔄 Uppdatering</span>"
                : "<span style=\"background-color:#dafbe1; color:#1f883d; padding:2px 6px; border-radius:4px; font-size:11px; font-weight:bold; display:inline-block;\">✓ OK</span>";

        // --- BYGGER UPPDATERAD CELL FÖR CPM-STATUS OCH URSPRUNG ---
        string cpmCellContent;
        if (item.CpmStatus.StartsWith("Nej", StringComparison.OrdinalIgnoreCase))
        {
            // Om det är transitivt lägger vi till en radbrytning och källpaketen under
            string sources = !string.IsNullOrWhiteSpace(item.IntroducedBy) ? item.IntroducedBy : "Okänd";
            cpmCellContent = $"{item.CpmStatus}<br>{sources}";
        }
        else
        {
            // För direkta paket behåller vi bara standardsträngen (t.ex: "Ja (1.0.0)")
            cpmCellContent = item.CpmStatus;
        }
        // ---------------------------------------------------------

        sb.AppendLine("    <tr>");
        sb.AppendLine($"      <td>{statusBadge}</td>");
        sb.AppendLine($"      <td><b>{item.PackageName}</b></td>");
        sb.AppendLine($"      <td><code>{item.InstalledVersion}</code></td>");
        sb.AppendLine($"      <td><code>{item.LatestVersion}</code></td>");
        sb.AppendLine($"      <td><font size=\"2\">{cpmCellContent}</font></td>");
        sb.AppendLine("    </tr>");
    }

    sb.AppendLine("  </tbody>");
    sb.AppendLine("</table>");

    await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8).ConfigureAwait(false);
}

#endregion


#region Hjälpmetoder

static bool IsVersionAffected(string currentVersionStr, string rangeStr)
{
    if (string.IsNullOrWhiteSpace(rangeStr))
    {
        return false;
    }

    if (!Version.TryParse(currentVersionStr.Split('-')[0], out var currentVersion))
    {
        return false;
    }

    rangeStr = rangeStr.Trim();

    if (rangeStr.StartsWith('(') || rangeStr.StartsWith('['))
    {
        var parts = rangeStr.Substring(1, rangeStr.Length - 2).Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        bool isMinInclusive = rangeStr.StartsWith('[');
        bool isMaxInclusive = rangeStr.EndsWith(']');

        string minStr = parts[0].Trim();
        string maxStr = parts[1].Trim();

        if (!string.IsNullOrEmpty(minStr) && Version.TryParse(minStr.Split('-')[0], out var minVersion))
        {
            if (isMinInclusive && currentVersion < minVersion)
            {
                return false;
            }

            if (!isMinInclusive && currentVersion <= minVersion)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(maxStr) && Version.TryParse(maxStr.Split('-')[0], out var maxVersion))
        {
            if (isMaxInclusive && currentVersion > maxVersion)
            {
                return false;
            }

            if (!isMaxInclusive && currentVersion >= maxVersion)
            {
                return false;
            }
        }

        return true;
    }

    if (rangeStr.StartsWith("<=", StringComparison.OrdinalIgnoreCase))
    {
        if (Version.TryParse(rangeStr.Replace("<=", "").Trim().Split('-')[0], out var v))
        {
            return currentVersion <= v;
        }
    }
    if (rangeStr.StartsWith('<'))
    {
        if (Version.TryParse(rangeStr.Replace("<", "").Trim().Split('-')[0], out var v))
        {
            return currentVersion < v;
        }
    }

    return false;
}

static void FindDirectRoots(
    string currentPackage,
    Dictionary<string, LockDependency> allPackages,
    Dictionary<string, List<string>> dependentOnTracker,
    HashSet<string> roots,
    HashSet<string> visited)
{
    // Förhindra oändliga loopar om det finns cirkulära referenser i graferna
    if (!visited.Add(currentPackage))
    {
        return;
    }

    if (dependentOnTracker.TryGetValue(currentPackage, out var parents))
    {
        foreach (var parent in parents)
        {
            if (allPackages.TryGetValue(parent, out var parentDetails))
            {
                if (parentDetails.Type == "Direct")
                {
                    // Vi hittade det översta direkt-paketet (roten)
                    roots.Add(parent);
                }
                else
                {
                    // Fortsätt gräva uppåt i trädet efter roten
                    FindDirectRoots(parent, allPackages, dependentOnTracker, roots, visited);
                }
            }
        }
    }
}

/// <summary>
/// Navigerar till solution katalogen genom att leta efter .sln-filer uppåt i katalogstrukturen, eller .git-katalogen som en fallback.
/// </summary>
static string GetWikiOutputPath(string fileName = "sbom-report.md")
{
    // Starta i katalogen där appen körs (t.ex. /artifacts/bin/SbomScanner/debug/)
    var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

    // Klättra uppåt tills vi hittar katalogen som innehåller .sln-filen eller .git
    while (currentDir != null)
    {
        if (currentDir.GetFiles("*.sln*").Length != 0 || currentDir.GetDirectories(".git").Length != 0)
        {
            // Vi har hittat Solution Root!
            string wikiDir = Path.Combine(currentDir.FullName, "wiki");

            // Skapa wiki-mappen om den inte redan finns
            if (!Directory.Exists(wikiDir))
            {
                Directory.CreateDirectory(wikiDir);
            }

            return Path.Combine(wikiDir, fileName);
        }

        currentDir = currentDir.Parent;
    }

    // Fallback om vi mot förmodan inte hittar rooten (t.ex. i en container/CI-pipeline)
    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
}

#endregion


#region DataModeller

internal sealed record ReportItem(
    string PackageName,
    string InstalledVersion,
    string LatestVersion,
    string CpmStatus,
    bool HasVersionMismatch,
    List<string> AllInstalledVersions,
    List<Vulnerability> ActiveVulnerabilities,
    bool IsSecuredOrPatched,
    string IntroducedBy
);

internal sealed record ServiceIndex(
    [property: JsonPropertyName("resources")]
    List<Resource> Resources
);

internal sealed record Resource(
    [property: JsonPropertyName("@id")]
    string Id,

    [property: JsonPropertyName("@type")]
    string Type
);

internal sealed record VulnerabilityFile(
    [property: JsonPropertyName("@name")]
    string Name,

    [property: JsonPropertyName("@id")]
    string Id
);

internal sealed record Vulnerability(
    [property: JsonPropertyName("severity")]
    int Severity,

    [property: JsonPropertyName("versions")]
    string Versions,

    [property: JsonPropertyName("url")]
    string Url
);

internal sealed record LockFile(
    [property: JsonPropertyName("dependencies")]
    Dictionary<string, Dictionary<string, LockDependency>> Dependencies
);

internal sealed record LockDependency(
    [property: JsonPropertyName("type")]
    string Type,

    [property: JsonPropertyName("resolved")]
    string Resolved,

    [property: JsonPropertyName("contentHash")]
    string ContentHash,

    [property: JsonPropertyName("dependencies")]
    Dictionary<string, string>? Dependencies
);

internal sealed record NuGetPackage
{
    public string Name { get; init; }
    public string Version { get; init; }
    public string ContentHash { get; init; }
    public string IntroducedBy { get; set; } = "Okänd";

    public NuGetPackage(string name, string version, string contentHash)
    {
        Name = name;
        Version = version;
        ContentHash = contentHash;
    }
}

internal sealed record NugetRegistrationIndex(
    [property: JsonPropertyName("items")]
    List<RegistrationPage> Pages
);

internal sealed record RegistrationPage(
    [property: JsonPropertyName("items")]
    List<RegistrationItem> Items
);

internal sealed record RegistrationItem(
    [property: JsonPropertyName("catalogEntry")]
    CatalogEntry CatalogEntry
);

internal sealed record CatalogEntry(
    [property: JsonPropertyName("version")]
    string Version
);

#endregion
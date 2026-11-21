using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using NuGet.Versioning;

#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable IDE0057 // Use range operator

string solutionRoot = FindSolutionRoot();
string propsFilePath = Path.Combine(solutionRoot, "Directory.Packages.props");
string markdownReportPath = Path.Combine(solutionRoot, "docs", "sbom-report.md");
var extractedPackages = new List<NuGetPackage>();

// Kontroll om man vill se förhandsversioner av NuGet-paket eller ej
bool includeNugetPrerelease = false;
Console.WriteLine($"Inkludera förhandsversioner (Preview): {includeNugetPrerelease}");

var solution = XDocument.Load(Path.Combine(solutionRoot, "Privatekonomi.slnx"));
var projectNames = solution.Descendants("Project")
    .Select(p => p.Attribute("Path")?.Value)
    .Where(p => p != null && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    .Select(p => Path.GetFileNameWithoutExtension(p!.Replace('/', Path.DirectorySeparatorChar)))
    .Where(name => name != "SbomScanner" && name != ".SolutionItems")
    .ToList();

foreach (string projectName in projectNames)
{
    string file = Path.Combine(solutionRoot, "artifacts", "obj", projectName, "project.assets.json");
    if (!File.Exists(file))
    {
        throw new FileNotFoundException($"Återställningsfilen för {projectName} saknas. Kör 'dotnet restore Privatekonomi.slnx' först.", file);
    }

    Console.WriteLine($"Läser in återställningsfil för: {projectName}");
    string jsonContent = await File.ReadAllTextAsync(file).ConfigureAwait(true);
    var assets = JsonSerializer.Deserialize<AssetsFile>(jsonContent)
        ?? throw new InvalidDataException($"Ogiltig återställningsfil: {file}");

    if (assets.Targets.Count == 0)
    {
        throw new InvalidDataException($"Inga ramverk hittades i återställningsfilen: {file}");
    }

    foreach (var target in assets.Targets)
    {
        string frameworkName = target.Key.Split('/')[0];
        if (!assets.Project.Frameworks.TryGetValue(frameworkName, out var framework))
        {
            throw new InvalidDataException($"Ramverket {frameworkName} saknas i {file}.");
        }

        var packages = new Dictionary<string, LockDependency>(StringComparer.OrdinalIgnoreCase);
        foreach (var library in target.Value)
        {
            int separator = library.Key.LastIndexOf('/');
            if (separator <= 0 || !library.Value.Type.Equals("package", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string name = library.Key[..separator];
            string version = library.Key[(separator + 1)..];
            bool direct = framework.Dependencies?.ContainsKey(name) == true;
            packages.Add(name, new LockDependency(direct ? "Direct" : "Transitive", version, library.Value.Dependencies));
        }

        var dependentOnTracker = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in packages)
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

        foreach (var package in packages)
        {
            string packageName = package.Key;
            string packageVersion = package.Value.Resolved;

            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (package.Value.Type == "Direct")
            {
                roots.Add("[Direkt paketberoende]");
            }
            else
            {
                FindDirectRoots(packageName, packages, dependentOnTracker, roots, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            string introducedByString = roots.Count > 0 ? string.Join(", ", roots) : "Okänd källa";

            var existing = extractedPackages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase) && p.Version == packageVersion);

            if (existing == null)
            {
                var nuGetPkg = new NuGetPackage(packageName, packageVersion)
                {
                    IntroducedBy = introducedByString
                };
                extractedPackages.Add(nuGetPkg);
            }
            else
            {
                if (existing.IntroducedBy != "[Direkt paketberoende]" && introducedByString != "[Direkt paketberoende]")
                {
                    var nuSources = existing.IntroducedBy.Split(", ")
                        .Where(source => source != "Okänd källa")
                        .Concat(roots)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    existing.IntroducedBy = nuSources.Count > 0 ? string.Join(", ", nuSources) : "Okänd källa";
                }
                else if (introducedByString == "[Direkt paketberoende]")
                {
                    existing.IntroducedBy = "[Direkt paketberoende]";
                }
            }
        }
    }
}

// 3. Läs in Directory.Packages.props
if (extractedPackages.Count == 0)
{
    throw new InvalidDataException("Inga NuGet-paket hittades i lösningens återställningsfiler.");
}

if (!File.Exists(propsFilePath))
{
    throw new FileNotFoundException("Kunde inte hitta Directory.Packages.props.", propsFilePath);
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
    throw new InvalidDataException("Kunde inte hitta nödvändiga NuGet-API-slutpunkter.");
}

// 6. Hämta fillistan
var vulnFiles = await client.GetFromJsonAsync<List<VulnerabilityFile>>(vulnResourceUrl).ConfigureAwait(true);

if (vulnFiles == null || !vulnFiles.Any(f => f.Name == "base"))
{
    throw new InvalidDataException("Kunde inte hitta NuGets sårbarhetsdatabas.");
}

var vulnerabilities = new Dictionary<string, List<Vulnerability>>(StringComparer.OrdinalIgnoreCase);
foreach (var vulnFile in vulnFiles)
{
    Console.WriteLine($"Laddar ner sårbarhetsdatabasen: {vulnFile.Name}");
    using var page = await client.GetFromJsonAsync<JsonDocument>(vulnFile.Id).ConfigureAwait(true)
        ?? throw new InvalidDataException($"Tom sårbarhetsdatabas: {vulnFile.Id}");
    if (page.RootElement.ValueKind == JsonValueKind.Array && page.RootElement.GetArrayLength() == 0)
    {
        continue;
    }
    var entries = page.RootElement.Deserialize<Dictionary<string, List<Vulnerability>>>()
        ?? throw new InvalidDataException($"Ogiltig sårbarhetsdatabas: {vulnFile.Id}");
    foreach (var entry in entries)
    {
        if (!vulnerabilities.TryGetValue(entry.Key, out var list))
        {
            list = [];
            vulnerabilities.Add(entry.Key, list);
        }
        list.AddRange(entry.Value);
    }
}

// 9. Hämta senaste versioner för alla unika paket parallellt
Console.WriteLine($"{Environment.NewLine}Hämtar senaste versioner från NuGet live-API...");
var uniqueLookups = extractedPackages
    .Select(p => new { Name = p.Name, Lower = p.Name.ToLowerInvariant() })
    .DistinctBy(p => p.Lower)
    .ToList();

using var lookupLimit = new SemaphoreSlim(12);
var lookupTasks = uniqueLookups.Select(async item =>
{
    await lookupLimit.WaitAsync().ConfigureAwait(false);
    try
    {
        var packageRegUrl = $"{regResourceUrl.TrimEnd('/')}/{item.Lower}/index.json";
        var regIndex = await client.GetFromJsonAsync<NugetRegistrationIndex>(packageRegUrl).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Tomt versionsregister för {item.Name}.");
        for (int p = regIndex.Pages.Count - 1; p >= 0; p--)
        {
            var page = regIndex.Pages[p];
            if (page.Items == null)
            {
                page = await client.GetFromJsonAsync<RegistrationPage>(page.Id).ConfigureAwait(false)
                    ?? throw new InvalidDataException($"Tom registersida för {item.Name}.");
            }
            foreach (var entry in (page.Items ?? []).AsEnumerable().Reverse())
            {
                var version = NuGetVersion.Parse(entry.CatalogEntry.Version);
                if (includeNugetPrerelease || !version.IsPrerelease)
                {
                    return new { item.Name, LatestVersion = entry.CatalogEntry.Version };
                }
            }
        }
        return new { item.Name, LatestVersion = "Okänd" };
    }
    finally
    {
        lookupLimit.Release();
    }
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
        string cpmStatus = cpmMatch != null ? $"Ja ({cpmMatch.Version})" : "Nej";

        latestVersionsDict.TryGetValue(pkg.Name, out string? latestNuGetVersion);
        latestNuGetVersion ??= "Okänd";

        var activeVulnerabilities = new List<Vulnerability>();
        if (vulnerabilities.TryGetValue(searchKey, out var vulnList))
        {
            foreach (var v in vulnList)
            {
                if (SbomVersioning.IsVersionAffected(pkg.Version, v.Versions))
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
        Console.WriteLine("⚠️  FÖLJANDE PAKET HAR OLIKA VERSIONER INOM LÖSNINGEN:");
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
            Console.WriteLine($"[SÅRBAR] {item.PackageName} (v. {item.InstalledVersion})");
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

            if (SbomVersioning.IsUpdateAvailable(item.InstalledVersion, item.LatestVersion))
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
    int outdatedPackages = items.Count(i => SbomVersioning.IsUpdateAvailable(i.InstalledVersion, i.LatestVersion) && i.ActiveVulnerabilities.Count == 0);

    sb.AppendLine("## 📊 Översikt");
    sb.AppendLine("<table width=\"100%\">");
    sb.AppendLine("  <tr>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#f6f8fa; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #d0d7de;\"><b>Totalt skannade</b><br><font size=\"5\" color=\"#1f2328\">{totalPackages}</font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#ffebe9; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #ffc1c0;\"><b>⚠️ Aktiva sårbarheter</b><br><font size=\"5\" color=\"#cf222e\"><b>{vulnerablePackages}</b></font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#fff8c5; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #eee0b0;\"><b>🔄 Olika paketversioner</b><br><font size=\"5\" color=\"#9a6700\"><b>{versionMismatches}</b></font></td>");
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
        sb.AppendLine("## 🔄 Olika paketversioner i lösningen");
        sb.AppendLine("<blockquote>Olika projekt kan använda olika versioner utan att det innebär en konflikt. Kontrollera beroenden och kompatibilitet innan versioner ändras.</blockquote>");
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
            ? "<span style=\"background-color:#fff8c5; color:#744210; padding:2px 6px; border-radius:4px; font-size:11px; font-weight:bold; display:inline-block;\">⚠️ Olika versioner</span>"
            : SbomVersioning.IsUpdateAvailable(item.InstalledVersion, item.LatestVersion)
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

static string FindSolutionRoot()
{
    var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
    while (currentDir != null)
    {
        if (File.Exists(Path.Combine(currentDir.FullName, "Privatekonomi.slnx")))
        {
            return currentDir.FullName;
        }
        currentDir = currentDir.Parent;
    }
    throw new DirectoryNotFoundException("Kunde inte hitta Privatekonomi.slnx ovanför skannerns utdatamapp.");
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

internal sealed record AssetsFile(
    [property: JsonPropertyName("targets")] Dictionary<string, Dictionary<string, AssetsLibrary>> Targets,
    [property: JsonPropertyName("project")] AssetsProject Project);

internal sealed record AssetsProject(
    [property: JsonPropertyName("frameworks")] Dictionary<string, AssetsFramework> Frameworks);

internal sealed record AssetsFramework(
    [property: JsonPropertyName("dependencies")] Dictionary<string, JsonElement>? Dependencies);

internal sealed record AssetsLibrary(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("dependencies")] Dictionary<string, string>? Dependencies);

internal sealed record LockDependency(string Type, string Resolved, Dictionary<string, string>? Dependencies);

internal sealed record NuGetPackage
{
    public string Name { get; init; }
    public string Version { get; init; }
    public string IntroducedBy { get; set; } = "Okänd";

    public NuGetPackage(string name, string version)
    {
        Name = name;
        Version = version;
    }
}

internal sealed record NugetRegistrationIndex(
    [property: JsonPropertyName("items")]
    List<RegistrationPage> Pages
);

internal sealed record RegistrationPage(
    [property: JsonPropertyName("@id")]
    string Id,

    [property: JsonPropertyName("items")]
    List<RegistrationItem>? Items
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
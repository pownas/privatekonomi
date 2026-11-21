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
string vulnerabilityCachePath = Path.Combine(solutionRoot, "tests", "SbomScanner", "vulnerabilities-cache.json");
bool offline = args.Contains("--offline", StringComparer.Ordinal);
bool requireOnline = args.Contains("--require-online", StringComparer.Ordinal);
if (args.Any(arg => arg is not "--offline" and not "--require-online") || (offline && requireOnline))
{
    throw new ArgumentException("Använd antingen --offline, --require-online eller inga argument.");
}
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
using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
var source = await SbomDataProvider.LoadAsync(client, "https://api.nuget.org/v3/index.json", vulnerabilityCachePath, offline, requireOnline).ConfigureAwait(true);
var vulnerabilities = source.Cache.Vulnerabilities;
string? regResourceUrl = source.RegistrationUrl;
Console.WriteLine($"Sårbarhetsdata hämtad {source.Cache.UpdatedAtUtc:u} ({(source.IsCached ? "lokal cache" : "NuGet")}).");
Console.WriteLine($"Lokal databas: {vulnerabilityCachePath}");
if (source.IsCached)
{
    Console.Error.WriteLine($"VARNING: {source.Warning} Lokala sårbarhetsdata kan vara inaktuella; paketversioner kan inte kontrolleras.");
}

// 9. Hämta senaste versioner för alla unika paket parallellt
Console.WriteLine($"{Environment.NewLine}Kontrollerar senaste paketversioner...");
var uniqueLookups = extractedPackages
    .Select(p => new { Name = p.Name, Lower = p.Name.ToLowerInvariant() })
    .DistinctBy(p => p.Lower)
    .ToList();

using var lookupLimit = new SemaphoreSlim(12);
using var lookupCancellation = new CancellationTokenSource();
var lookupTasks = uniqueLookups.Select(async item =>
{
    await lookupLimit.WaitAsync(lookupCancellation.Token).ConfigureAwait(false);
    try
    {
        var packageRegUrl = $"{(regResourceUrl ?? throw new InvalidDataException("NuGets versionsregister saknas.")).TrimEnd('/')}/{item.Lower}/index.json";
        var regIndex = await client.GetFromJsonAsync<NugetRegistrationIndex>(packageRegUrl, lookupCancellation.Token).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Tomt versionsregister för {item.Name}.");
        for (int p = regIndex.Pages.Count - 1; p >= 0; p--)
        {
            var page = regIndex.Pages[p];
            if (page.Items == null)
            {
                page = await client.GetFromJsonAsync<RegistrationPage>(page.Id, lookupCancellation.Token).ConfigureAwait(false)
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
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
    {
        await lookupCancellation.CancelAsync().ConfigureAwait(false);
        throw;
    }
    finally
    {
        lookupLimit.Release();
    }
});

var latestVersionsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
bool versionsUnavailable = source.IsCached;
if (!source.IsCached)
{
    try
    {
        var lookupResults = await Task.WhenAll(lookupTasks).ConfigureAwait(true);
        latestVersionsDict = lookupResults.ToDictionary(x => x.Name, x => x.LatestVersion, StringComparer.OrdinalIgnoreCase);
    }
    catch (Exception ex) when (!requireOnline && ex is (HttpRequestException or OperationCanceledException))
    {
        versionsUnavailable = true;
        Console.Error.WriteLine($"VARNING: Paketversioner kunde inte hämtas från NuGet: {ex.Message}");
    }
}

// 10. Processa och bygg data för rapporten
Console.WriteLine($"{Environment.NewLine}Analyserar resultat...");

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
await GenerateMarkdownReportAsync(reportData, markdownReportPath, source, versionsUnavailable).ConfigureAwait(true);

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

            if (item.LatestVersion == "Okänd")
            {
                Console.Write(" [Senaste version ej kontrollerad]");
            }

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

static async Task GenerateMarkdownReportAsync(List<ReportItem> items, string outputPath, VulnerabilitySource source, bool versionsUnavailable)
{
    var sb = new StringBuilder();

    sb.AppendLine("# 🛡️ Security & SBOM Scan Report");
    sb.AppendLine($"*Genererad den: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
    sb.AppendLine();
    sb.AppendLine($"Sårbarhetsdata: {(source.IsCached ? "lokal cache (`tests/SbomScanner/vulnerabilities-cache.json`)" : "NuGet")}, hämtad {source.Cache.UpdatedAtUtc:u}.");
    if (source.IsCached)
    {
        sb.AppendLine("> ⚠️ Den lokala sårbarhetsdatabasen kan vara inaktuell. Nya sårbarheter kan saknas.");
    }
    if (versionsUnavailable)
    {
        sb.AppendLine("> ⚠️ Senaste paketversioner kunde inte kontrolleras mot NuGet.");
    }
    sb.AppendLine();

    // 1. Sammanfattning (KPI-Kort)
    int totalPackages = items.Count;
    int vulnerablePackages = items.Count(i => i.ActiveVulnerabilities.Count != 0);
    int versionMismatches = items.Where(i => i.HasVersionMismatch).Select(i => i.PackageName).Distinct().Count();
    int outdatedPackages = items.Count(i => SbomVersioning.IsUpdateAvailable(i.InstalledVersion, i.LatestVersion) && i.ActiveVulnerabilities.Count == 0);
    string outdatedSummary = versionsUnavailable ? "Ej kontrollerat"
        : items.Any(i => i.LatestVersion == "Okänd") ? $"Minst {outdatedPackages}"
        : outdatedPackages.ToString(System.Globalization.CultureInfo.InvariantCulture);

    sb.AppendLine("## 📊 Översikt");
    sb.AppendLine("<table width=\"100%\">");
    sb.AppendLine("  <tr>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#f6f8fa; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #d0d7de;\"><b>Totalt skannade</b><br><font size=\"5\" color=\"#1f2328\">{totalPackages}</font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#ffebe9; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #ffc1c0;\"><b>⚠️ Aktiva sårbarheter</b><br><font size=\"5\" color=\"#cf222e\"><b>{vulnerablePackages}</b></font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#fff8c5; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #eee0b0;\"><b>🔄 Olika paketversioner</b><br><font size=\"5\" color=\"#9a6700\"><b>{versionMismatches}</b></font></td>");
    sb.AppendLine($"    <td align=\"center\" width=\"25%\" style=\"background-color:#ddf4ff; color:#1f2328; padding:15px; border-radius:6px; border:1px solid #b6e3ff;\"><b>📦 Outdaterade paket</b><br><font size=\"5\" color=\"#0969da\"><b>{outdatedSummary}</b></font></td>");
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
            : item.LatestVersion == "Okänd"
                ? "<span style=\"background-color:#fff8c5; color:#744210; padding:2px 6px; border-radius:4px; font-size:11px; font-weight:bold; display:inline-block;\">⚠️ Version ej kontrollerad</span>"
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

internal static class SbomDataProvider
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new() { WriteIndented = true };

    internal static async Task<VulnerabilitySource> LoadAsync(HttpClient client, string indexUrl, string cachePath, bool offline, bool requireOnline)
    {
        if (offline)
        {
            return await LoadCacheAsync(cachePath, "Offline-läge är valt.").ConfigureAwait(false);
        }

        try
        {
            Console.WriteLine($"Hämtar NuGets serviceindex: {indexUrl}");
            var index = await client.GetFromJsonAsync<ServiceIndex>(indexUrl).ConfigureAwait(false)
                ?? throw new InvalidDataException("NuGets serviceindex är tomt.");
            string registrationUrl = index.Resources.FirstOrDefault(r => r.Type.StartsWith("RegistrationsBaseUrl/3.6.0", StringComparison.OrdinalIgnoreCase))?.Id
                ?? throw new InvalidDataException("NuGets versionsregister saknas i serviceindexet.");
            string vulnerabilityUrl = index.Resources.FirstOrDefault(r => r.Type.StartsWith("VulnerabilityInfo", StringComparison.OrdinalIgnoreCase))?.Id
                ?? throw new InvalidDataException("NuGets sårbarhetsdatabas saknas i serviceindexet.");
            var pages = await client.GetFromJsonAsync<List<VulnerabilityFile>>(vulnerabilityUrl).ConfigureAwait(false);
            if (pages == null || !pages.Any(page => page.Name == "base"))
            {
                throw new InvalidDataException("NuGets sårbarhetsdatabas saknar basfil.");
            }

            var vulnerabilities = new Dictionary<string, List<Vulnerability>>(StringComparer.OrdinalIgnoreCase);
            foreach (var page in pages)
            {
                Console.WriteLine($"Laddar ner sårbarhetsdatabasen: {page.Name}");
                using var json = await client.GetFromJsonAsync<JsonDocument>(page.Id).ConfigureAwait(false)
                    ?? throw new InvalidDataException($"Tom sårbarhetsdatabas: {page.Id}");
                if (json.RootElement.ValueKind == JsonValueKind.Array && json.RootElement.GetArrayLength() == 0)
                {
                    continue;
                }
                var entries = json.RootElement.Deserialize<Dictionary<string, List<Vulnerability>>>()
                    ?? throw new InvalidDataException($"Ogiltig sårbarhetsdatabas: {page.Id}");
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

            var cache = new VulnerabilityCache(DateTimeOffset.UtcNow, vulnerabilities);
            ValidateCache(cache, cachePath);
            string temporaryPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = File.Create(temporaryPath))
                {
                    await JsonSerializer.SerializeAsync(stream, cache, CacheJsonOptions).ConfigureAwait(false);
                }
                File.Move(temporaryPath, cachePath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            return new VulnerabilitySource(cache, false, registrationUrl, null);
        }
        catch (Exception ex) when (!requireOnline && ex is (HttpRequestException or TaskCanceledException))
        {
            return await LoadCacheAsync(cachePath, $"NuGet kunde inte nås: {ex.Message}").ConfigureAwait(false);
        }
    }

    private static async Task<VulnerabilitySource> LoadCacheAsync(string cachePath, string warning)
    {
        if (!File.Exists(cachePath))
        {
            throw new FileNotFoundException($"{warning} Ingen lokal sårbarhetsdatabas finns; kör skannern med nätverksåtkomst först.", cachePath);
        }
        await using var stream = File.OpenRead(cachePath);
        var cache = await JsonSerializer.DeserializeAsync<VulnerabilityCache>(stream).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Den lokala sårbarhetsdatabasen är tom: {cachePath}");
        ValidateCache(cache, cachePath);
        cache = cache with { Vulnerabilities = new Dictionary<string, List<Vulnerability>>(cache.Vulnerabilities, StringComparer.OrdinalIgnoreCase) };
        return new VulnerabilitySource(cache, true, null, warning);
    }

    private static void ValidateCache(VulnerabilityCache cache, string path)
    {
        if (cache.UpdatedAtUtc == default || cache.Vulnerabilities == null || cache.Vulnerabilities.Count == 0 ||
            cache.Vulnerabilities.Values.Any(entries => entries == null))
        {
            throw new InvalidDataException($"Sårbarhetsdatabasen är ofullständig: {path}");
        }
    }
}

internal sealed record VulnerabilityCache(DateTimeOffset UpdatedAtUtc, Dictionary<string, List<Vulnerability>> Vulnerabilities);
internal sealed record VulnerabilitySource(VulnerabilityCache Cache, bool IsCached, string? RegistrationUrl, string? Warning);

internal static class SbomVersioning
{
    internal static bool IsVersionAffected(string currentVersionStr, string rangeStr)
    {
        if (!NuGetVersion.TryParse(currentVersionStr, out var currentVersion) ||
            !VersionRange.TryParse(rangeStr, out var range))
        {
            throw new InvalidDataException($"Ogiltig paketversion eller sårbarhetsintervall: {currentVersionStr}, {rangeStr}");
        }

        return range.Satisfies(currentVersion);
    }

    internal static bool IsUpdateAvailable(string installedVersion, string latestVersion) =>
        NuGetVersion.TryParse(latestVersion, out var latest) &&
        NuGetVersion.TryParse(installedVersion, out var installed) && latest > installed;
}


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
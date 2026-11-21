namespace Privatekonomi.Core.Models;

public enum BankId
{
    AllmanOfx = 0,
    Handelsbanken,
    IcaBanken,
    Nordea,
    Seb,
    Swedbank,
    Avanza,
    Nordnet,
    Lysa,
    Opti,
    Klarna,
    LantmannenFinans,
    Revolut,
    NordiskaBanken,
    Montrose
}

public static class BankNames
{
    public const string AllmanOfx = "OFX (Allmän)";
    public const string Handelsbanken = "Handelsbanken";
    public const string IcaBanken = "ICA-banken";
    public const string Nordea = "Nordea";
    public const string Seb = "SEB";
    public const string Swedbank = "Swedbank";
    public const string Avanza = "Avanza";
    public const string Nordnet = "Nordnet";
    public const string Lysa = "Lysa";
    public const string Opti = "Opti";
    public const string Klarna = "Klarna";
    public const string LantmannenFinans = "Lantmannen Finans";
    public const string Revolut = "Revolut";
    public const string NordiskaBanken = "Nordiska banken";
    public const string Montrose = "Montrose";

    public static string GetName(BankId id) => id switch
    {
        BankId.AllmanOfx => AllmanOfx,
        BankId.Handelsbanken => Handelsbanken,
        BankId.IcaBanken => IcaBanken,
        BankId.Nordea => Nordea,
        BankId.Seb => Seb,
        BankId.Swedbank => Swedbank,
        BankId.Avanza => Avanza,
        BankId.Nordnet => Nordnet,
        BankId.Lysa => Lysa,
        BankId.Opti => Opti,
        BankId.Klarna => Klarna,
        BankId.LantmannenFinans => LantmannenFinans,
        BankId.Revolut => Revolut,
        BankId.NordiskaBanken => NordiskaBanken,
        BankId.Montrose => Montrose,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Okänd bank")
    };

    public static BankId GetId(string name) => name switch
    {
        _ when string.Equals(name, AllmanOfx, StringComparison.OrdinalIgnoreCase) => BankId.AllmanOfx,
        _ when string.Equals(name, Handelsbanken, StringComparison.OrdinalIgnoreCase) => BankId.Handelsbanken,
        _ when string.Equals(name, IcaBanken, StringComparison.OrdinalIgnoreCase) => BankId.IcaBanken,
        _ when string.Equals(name, Nordea, StringComparison.OrdinalIgnoreCase) => BankId.Nordea,
        _ when string.Equals(name, Seb, StringComparison.OrdinalIgnoreCase) => BankId.Seb,
        _ when string.Equals(name, Swedbank, StringComparison.OrdinalIgnoreCase) => BankId.Swedbank,
        _ when string.Equals(name, Avanza, StringComparison.OrdinalIgnoreCase) => BankId.Avanza,
        _ when string.Equals(name, Nordnet, StringComparison.OrdinalIgnoreCase) => BankId.Nordnet,
        _ when string.Equals(name, Lysa, StringComparison.OrdinalIgnoreCase) => BankId.Lysa,
        _ when string.Equals(name, Opti, StringComparison.OrdinalIgnoreCase) => BankId.Opti,
        _ when string.Equals(name, Klarna, StringComparison.OrdinalIgnoreCase) => BankId.Klarna,
        _ when string.Equals(name, LantmannenFinans, StringComparison.OrdinalIgnoreCase) => BankId.LantmannenFinans,
        _ when string.Equals(name, Revolut, StringComparison.OrdinalIgnoreCase) => BankId.Revolut,
        _ when string.Equals(name, NordiskaBanken, StringComparison.OrdinalIgnoreCase) => BankId.NordiskaBanken,
        _ when string.Equals(name, Montrose, StringComparison.OrdinalIgnoreCase) => BankId.Montrose,
        _ => throw new ArgumentException($"Okänd bank: {name}", nameof(name))
    };
}

/// <summary>
/// Information about a supported bank institution
/// </summary>
public class BankInfo
{
    public BankId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    
    public BankInfo(string name, string colorHex, string website)
    {
        Id = BankNames.GetId(name);
        Name = name;
        ColorHex = colorHex;
        Website = website;
    }

    public BankInfo(BankId id, string colorHex, string website)
    {
        Id = id;
        Name = BankNames.GetName(id);
        ColorHex = colorHex;
        Website = website;
    }
}

/// <summary>
/// Centralized configuration for supported banks
/// </summary>
public static class BankRegistry
{
    /// <summary>
    /// List of supported banks with their official brand colors
    /// </summary>
    public static readonly List<BankInfo> SupportedBanks = new()
    {
        new BankInfo(BankId.Handelsbanken, "#003781", "https://www.handelsbanken.se"),
        new BankInfo(BankId.IcaBanken, "#E3000F", "https://www.icabanken.se"),
        new BankInfo(BankId.Nordea, "#0000A0", "https://www.nordea.se"),
        new BankInfo(BankId.Seb, "#60CD18", "https://seb.se"),
        new BankInfo(BankId.Swedbank, "#FF7900", "https://www.swedbank.se"),
        new BankInfo(BankId.Avanza, "#00C281", "https://www.avanza.se"),
        new BankInfo(BankId.Nordnet, "#2E7D32", "https://www.nordnet.se"),
        new BankInfo(BankId.Lysa, "#31A8A0", "https://lysa.se"),
        new BankInfo(BankId.Opti, "#4F46E5", "https://www.opti.se"),
        new BankInfo(BankId.Klarna, "#FFB3C7", "https://www.klarna.com/se"),
        new BankInfo(BankId.LantmannenFinans, "#5B3A29", "https://www.lantmannenfinans.se"),
        new BankInfo(BankId.Revolut, "#191C1F", "https://www.revolut.com/sv-SE"),
        new BankInfo(BankId.NordiskaBanken, "#123B5D", "https://www.nordiska.com/se"),
        new BankInfo(BankId.Montrose, "#6B5B95", "https://www.montrose.io")
    };
    
    /// <summary>
    /// Get bank info by name
    /// </summary>
    public static BankInfo? GetBankByName(string name)
    {
        return SupportedBanks.FirstOrDefault(b => 
            b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static BankInfo? GetBankByWebsite(string website)
    {
        if (string.IsNullOrWhiteSpace(website))
            return null;

        var normalized = NormalizeWebsite(website);
        return SupportedBanks.FirstOrDefault(b =>
            NormalizeWebsite(b.Website).Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeWebsite(string website)
    {
        if (!Uri.TryCreate(website, UriKind.Absolute, out var uri))
        {
            return website.Trim().TrimEnd('/').ToLowerInvariant();
        }

        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host["www.".Length..]
            : uri.Host;
        var path = uri.AbsolutePath.TrimEnd('/');
        return $"{uri.Scheme.ToLowerInvariant()}://{host.ToLowerInvariant()}{path}";
    }
    
    /// <summary>
    /// Get color for a bank
    /// </summary>
    public static string? GetBankColor(string? bankName)
    {
        if (string.IsNullOrEmpty(bankName))
            return null;
            
        return GetBankByName(bankName)?.ColorHex;
    }
}

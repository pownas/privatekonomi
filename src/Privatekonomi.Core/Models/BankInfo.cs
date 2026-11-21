namespace Privatekonomi.Core.Models;

/// <summary>
/// Information about a supported bank institution
/// </summary>
public class BankInfo
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    
    public BankInfo(string name, string colorHex, string website)
    {
        Name = name;
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
        new BankInfo("Handelsbanken", "#003781", "https://www.handelsbanken.se"),
        new BankInfo("ICA-banken", "#E3000F", "https://www.icabanken.se"),
        new BankInfo("Nordea", "#0000A0", "https://www.nordea.se"),
        new BankInfo("SEB", "#60CD18", "https://seb.se"),
        new BankInfo("Swedbank", "#FF7900", "https://www.swedbank.se"),
        new BankInfo("Avanza", "#00C281", "https://www.avanza.se"),
        new BankInfo("Nordnet", "#2E7D32", "https://www.nordnet.se"),
        new BankInfo("Lysa", "#31A8A0", "https://lysa.se"),
        new BankInfo("Opti", "#4F46E5", "https://www.opti.se"),
        new BankInfo("Klarna", "#FFB3C7", "https://www.klarna.com/se"),
        new BankInfo("Lantmannen Finans", "#5B3A29", "https://www.lantmannenfinans.se"),
        new BankInfo("Revolut", "#191C1F", "https://www.revolut.com/sv-SE"),
        new BankInfo("Nordiska banken", "#123B5D", "https://www.nordiska.com/se"),
        new BankInfo("Montrose", "#6B5B95", "https://www.montrose.io")
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

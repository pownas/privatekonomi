namespace Privatekonomi.Web.Services;

public sealed class EmailSettings
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string FromAddress { get; set; } = "noreply@privatekonomi.local";
    public string FromName { get; set; } = "Privatekonomi";
    public string? PublicBaseUrl { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);
}

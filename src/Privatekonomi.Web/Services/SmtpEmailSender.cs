using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Web.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailSettings> settings,
    ILogger<SmtpEmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Bekräfta din e-postadress", "Bekräfta e-postadressen för ditt konto via länken nedan:", confirmationLink);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Återställ ditt lösenord", "Återställ lösenordet för ditt konto via länken nedan:", resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Återställ ditt lösenord", $"Ange följande kod för att återställa lösenordet: {resetCode}", null);

    private async Task SendAsync(string email, string subject, string message, string? link)
    {
        var options = settings.Value;
        if (!options.IsConfigured)
        {
            logger.LogWarning("Email delivery is unavailable because SMTP is not configured.");
            throw new InvalidOperationException("E-postleverans är inte konfigurerad.");
        }

        var body = link is null
            ? $"<p>{WebUtility.HtmlEncode(message)}</p>"
            : $"<p>{WebUtility.HtmlEncode(message)}</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(link)}</a></p>";

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        emailMessage.To.Add(MailboxAddress.Parse(email));
        emailMessage.Subject = subject;
        emailMessage.Body = new BodyBuilder { HtmlBody = body, TextBody = $"{message}{(link is null ? string.Empty : $"\n\n{link}")}" }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(options.SmtpHost, options.SmtpPort, SecureSocketOptions.StartTls);
        if (!string.IsNullOrWhiteSpace(options.SmtpUser))
        {
            await client.AuthenticateAsync(options.SmtpUser, options.SmtpPassword);
        }

        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }
}

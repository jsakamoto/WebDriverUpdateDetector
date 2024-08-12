using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using WebDriverUpdateDetector.Internal;

namespace WebDriverUpdateDetector;

internal static class Mail
{
    public static async ValueTask SendAsync(IConfiguration configuration, string subject, string body)
    {
        var version = typeof(Mail).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        var smtpConfig = configuration.GetSection<SmtpConfig>("Smtp");
        var notifyMailConfig = configuration.GetSection<NotifyMailConfig>("NotifyMail");

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(smtpConfig.Host, smtpConfig.Port, smtpConfig.UseSSL);
        await smtpClient.AuthenticateAsync(smtpConfig.UserName, smtpConfig.Password);
        await smtpClient.SendAsync(new MimeMessage
        {
            From = { new MailboxAddress(notifyMailConfig.From, notifyMailConfig.From) },
            To = { new MailboxAddress(notifyMailConfig.To, notifyMailConfig.To) },
            Subject = $"[WebDriver Update v{version}] {subject}",
            Body = new TextPart { Text = body }
        });
        await smtpClient.DisconnectAsync(quit: true);
    }
}

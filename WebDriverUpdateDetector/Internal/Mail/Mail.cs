using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using WebDriverUpdateDetector.Services;

namespace WebDriverUpdateDetector.Internal.Mail;

internal class Mail : IMail
{
    private readonly IOptionsMonitor<SmtpConfig> _smtpOptions;

    private readonly IOptionsMonitor<NotifyMailConfig> _notifyMailOptions;

    public Mail(IOptionsMonitor<SmtpConfig> smtpOptions, IOptionsMonitor<NotifyMailConfig> notifyMailOptions)
    {
        this._smtpOptions = smtpOptions;
        this._notifyMailOptions = notifyMailOptions;
    }

    public async ValueTask SendAsync(string subject, string body)
    {
        var version = typeof(Mail).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        var smtpConfig = this._smtpOptions.CurrentValue;
        var notifyMailConfig = this._notifyMailOptions.CurrentValue;

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(smtpConfig.Host, smtpConfig.Port, smtpConfig.UseSSL);
        await smtpClient.AuthenticateAsync(smtpConfig.UserName, smtpConfig.Password);
        await smtpClient.SendAsync(new MimeMessage
        {
            From = { new MailboxAddress(notifyMailConfig.From, notifyMailConfig.From) },
            To = { new MailboxAddress(notifyMailConfig.To, notifyMailConfig.To) },
            Subject = $"{subject} | WebDriver Update Detactor v{version}]",
            Body = new TextPart { Text = body }
        });
        await smtpClient.DisconnectAsync(quit: true);
    }
}

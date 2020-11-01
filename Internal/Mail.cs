using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace WebDriverUpdateDetector
{
    internal static class Mail
    {
        public static void Send(IConfiguration configuration, string subject, string body)
        {
            using var smtpClient = new SmtpClient
            {
                Host = configuration["Smtp:Host"],
                Port = configuration.GetValue<int>("Smtp:Port"),
                EnableSsl = true,
                Credentials = new NetworkCredential
                {
                    UserName = configuration["Smtp:UserName"],
                    Password = configuration["Smtp:Password"]
                }
            };

            smtpClient.Send(
                configuration["NotifyMail:From"],
                configuration["NotifyMail:To"],
                subject,
                body);
        }
    }
}

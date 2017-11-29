using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace WebDriverUpdateDetector
{
    internal static class Mail
    {
        public static void Send(string subject, string body)
        {
            using (var smtpClient = new SmtpClient
            {
                Host = ConfigurationManager.AppSettings["Smtp.Host"],
                Port = int.Parse(ConfigurationManager.AppSettings["Smtp.Port"]),
                Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["Smtp.UserName"],
                        ConfigurationManager.AppSettings["Smtp.Password"]
                    )
            })
            {
                smtpClient.Send(
                    ConfigurationManager.AppSettings["NotifyMail.From"],
                    ConfigurationManager.AppSettings["NotifyMail.To"],
                    subject,
                    body);
            }
        }
    }
}

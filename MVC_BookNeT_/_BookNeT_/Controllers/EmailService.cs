using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;

namespace _BookNeT_.Services
{
    public class EmailService
    {
        private static readonly string SmtpServer   = "smtp.gmail.com";
        private static readonly int    SmtpPort     = 587;
        private static readonly string SmtpUsername = ConfigurationManager.AppSettings["Smtp:Username"];
        private static readonly string SmtpPassword = ConfigurationManager.AppSettings["Smtp:Password"];

        /// <summary>שליחת מייל synchronous</summary>
        public void Send(string to, string subject, string body)
        {
            try
            {
                using (var mail = BuildMessage(to, subject, body))
                using (var client = BuildClient())
                {
                    client.Send(mail);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailService] Send failed to {to}: {ex.Message}");
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        /// <summary>שליחת מייל asynchronous</summary>
        public async Task<bool> SendAsync(string to, string subject, string body)
        {
            try
            {
                using (var mail = BuildMessage(to, subject, body))
                using (var client = BuildClient())
                {
                    await client.SendMailAsync(mail);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailService] SendAsync failed to {to}: {ex.Message}");
                return false;
            }
        }

        private MailMessage BuildMessage(string to, string subject, string body)
        {
            var mail = new MailMessage
            {
                From       = new MailAddress(SmtpUsername, "BookNeT"),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = true
            };
            mail.To.Add(to);
            return mail;
        }

        private SmtpClient BuildClient()
        {
            return new SmtpClient(SmtpServer, SmtpPort)
            {
                Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl   = true
            };
        }
    }
}

using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ForUpworkRestaurentManagement.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var from = _config["Smtp:From"];
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];
            var enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                Console.WriteLine("[Email] Skipping send: toEmail is empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            {
                Console.WriteLine("[Email] SMTP not configured (Host/From). Skipping email send. Set Smtp:Host and Smtp:From in appsettings.");
                return; // no-op when not configured
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = string.IsNullOrWhiteSpace(user) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(user, pass)
                };

                using var msg = new MailMessage(from, toEmail)
                {
                    Subject = subject ?? string.Empty,
                    Body = htmlBody ?? string.Empty,
                    IsBodyHtml = true
                };
                await client.SendMailAsync(msg);
                Console.WriteLine($"[Email] Sent to {toEmail} with subject '{subject}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] Failed to send to {toEmail}: {ex.Message}");
                // Swallow to avoid breaking business flow
            }
        }
    }
}

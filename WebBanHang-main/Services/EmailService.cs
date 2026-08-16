using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebBanHang.Services
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var settings = _config.GetSection("EmailSettings");
            var host = settings["SmtpHost"] ?? "smtp.gmail.com";
            var port = int.Parse(settings["SmtpPort"] ?? "587");
            var senderEmail = settings["SenderEmail"] ?? "";
            var senderName = settings["SenderName"] ?? "Bách Hóa XANH";
            var appPassword = settings["AppPassword"] ?? "";

            _logger.LogInformation("Sending email to {To} via {Host}:{Port} from {From}",
                email, host, port, senderEmail);

            // Use SmtpClient with explicit SSL (STARTTLS on port 587)
            using var client = new SmtpClient(host, port)
            {
                // EnableSsl = true causes STARTTLS negotiation on port 587
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, appPassword),
                Timeout = 30000 // 30 seconds
            };

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(new MailAddress(email));

            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to {To}", email);
        }
    }
}

using MailKit.Net.Smtp;
using MimeKit;
namespace SearchAgentService.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _smtpHost = configuration["SMTP_HOST"] ?? "localhost";
            _smtpPort = int.TryParse(configuration["SMTP_PORT"], out var port) ? port : 1025;
            _fromEmail = configuration["SMTP_FROM"] ?? "searchagent@searchengine.dk";
        }

        public async Task SendAgentResultAsync(
            string toEmail,
            string[] searchWords,
            int hits,
            List<string> documentUrls)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"Søgeagent match: {string.Join(", ", searchWords)} ({hits} hits)";

            var body = $"Din søgeagent fandt {hits} resultater for søgeordet: {string.Join(", ", searchWords)}\n\n";
            body += "Dokumenter:\n";

            foreach (var url in documentUrls)
            {
                body += $"  - {url}\n";
            }

            body += "\nDenne agent er nu afsluttet og slettet.\n";
            body += "Opret en ny søgeagent hvis du vil overvåge igen.";

            message.Body = new TextPart("plain") { Text = body };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, false);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation(
                    "Email sent | To: {Email} | Words: {Words} | Hits: {Hits}",
                    toEmail, string.Join(",", searchWords), hits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email | To: {Email} | Words: {Words}",
                    toEmail, string.Join(",", searchWords));
            }
        }
    }
}

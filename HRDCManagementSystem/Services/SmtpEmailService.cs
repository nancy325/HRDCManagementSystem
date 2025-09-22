using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading.Tasks;

namespace HRDCManagementSystem.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public SmtpEmailService(EmailSettings emailSettings)
        {
            _emailSettings = emailSettings;
        }               

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            await SendEmailAsync(new[] { to }, subject, body, isHtml);
        }

        public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true)
        {
            await SendEmailWithAttachmentAsync(to, subject, body, null, isHtml);
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, IEnumerable<string> attachmentPaths, bool isHtml = true)
        {
            await SendEmailWithAttachmentAsync(new[] { to }, subject, body, attachmentPaths, isHtml);
        }

        public async Task SendEmailWithAttachmentAsync(IEnumerable<string> to, string subject, string body, IEnumerable<string> attachmentPaths, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                
                // Add multiple recipients
                foreach (var recipient in to)
                {
                    message.To.Add(new MailboxAddress("", recipient));
                }
                
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                // Add attachments if provided
                if (attachmentPaths != null)
                {
                    foreach (var attachmentPath in attachmentPaths)
                    {
                        if (File.Exists(attachmentPath))
                        {
                            bodyBuilder.Attachments.Add(attachmentPath);
                        }
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Configure SSL certificate validation
                    client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => 
                    {
                        // In production, you should validate the certificate properly
                        // For now, accept all certificates for Gmail
                        return true;
                    };

                    // Determine the SecureSocketOptions based on UseSsl setting and port
                    SecureSocketOptions secureSocketOptions;
                    if (_emailSettings.UseSsl)
                    {
                        // Port 465 typically uses SSL from the start
                        // Port 587 typically uses StartTLS
                        secureSocketOptions = _emailSettings.Port == 465 
                            ? SecureSocketOptions.SslOnConnect 
                            : SecureSocketOptions.StartTls;
                    }
                    else
                    {
                        secureSocketOptions = SecureSocketOptions.None;
                    }

                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, secureSocketOptions);

                    // Only authenticate if credentials are provided
                    if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
                    {
                        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                    }
                    
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to inject ILogger here for better logging)
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}
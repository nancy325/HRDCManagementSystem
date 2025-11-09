namespace HRDCManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true);
        Task SendEmailWithAttachmentAsync(string to, string subject, string body, IEnumerable<string> attachmentPaths, bool isHtml = true);
        Task SendEmailWithAttachmentAsync(IEnumerable<string> to, string subject, string body, IEnumerable<string> attachmentPaths, bool isHtml = true);

        // New method for sending email with System.Net.Mail.Attachment
        Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, System.Net.Mail.Attachment attachment);
    }
}
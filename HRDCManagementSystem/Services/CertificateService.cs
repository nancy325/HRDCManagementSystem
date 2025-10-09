using HRDCManagementSystem.Models.Entities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net.Mime;

namespace HRDCManagementSystem.Services
{
    public interface ICertificateService
    {
        Task<string> GenerateCertificateAsync(TrainingRegistration registration);
        Task<bool> SendCertificateEmailAsync(string employeeEmail, string employeeName, string trainingTitle, string certificatePath);
        Task<bool> GenerateAllCertificatesForTrainingAsync(int trainingSysId);
    }

    public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public CertificateService(
            ILogger<CertificateService> logger,
            IWebHostEnvironment hostingEnvironment,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<string> GenerateCertificateAsync(TrainingRegistration registration)
        {
            try
            {
                // Create directories if they don't exist
                var certificatesDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates");
                if (!Directory.Exists(certificatesDirectory))
                {
                    Directory.CreateDirectory(certificatesDirectory);
                }

                var signatureDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "images", "signature");
                if (!Directory.Exists(signatureDirectory))
                {
                    Directory.CreateDirectory(signatureDirectory);
                }

                // Make sure we have all required data
                if (registration.EmployeeSys == null || registration.TrainingSys == null)
                {
                    throw new InvalidOperationException("Registration must include Employee and Training details");
                }

                // Prepare file paths
                string templatePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates", "template.jpg");
                string signaturePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates", "sign.jpg");
                
                // Create default template if it doesn't exist
                if (!File.Exists(templatePath))
                {
                    await CreateDefaultTemplateAsync(templatePath);
                }

                // Create default signature if it doesn't exist
                if (!File.Exists(signaturePath))
                {
                    await CreateDefaultSignatureAsync(signaturePath);
                }

                // Set up the PDF document
                string fileName = $"{registration.TrainingRegSysID}.pdf";
                string outputPath = Path.Combine(certificatesDirectory, fileName);
                
                // Generate certificate using iTextSharp
                Document document = new Document(PageSize.A4.Rotate()); // Landscape orientation
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                document.Open();

                // Add the background image
                if (File.Exists(templatePath))
                {
                    using var imageStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
                    iTextSharp.text.Image backgroundImage = iTextSharp.text.Image.GetInstance(imageStream);
                    backgroundImage.ScaleToFit(document.PageSize.Width, document.PageSize.Height);
                    backgroundImage.SetAbsolutePosition(0, 0);
                    document.Add(backgroundImage);
                }

                // Font setup
                string fontPath = Path.Combine(_hostingEnvironment.WebRootPath, "fonts");
                if (!Directory.Exists(fontPath))
                {
                    Directory.CreateDirectory(fontPath);
                }

                // Using standard fonts
                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                Font titleFont = new Font(baseFont, 24, Font.BOLD);
                Font headerFont = new Font(baseFont, 18, Font.BOLD);
                Font normalFont = new Font(baseFont, 14);
                Font nameFont = new Font(baseFont, 22, Font.BOLD);

                // Calculate center positions
                float centerX = document.PageSize.Width / 2;

                // Add content
                // Organization Name
                Paragraph orgName = new Paragraph("Human Resource Development Centre (HRDC), CHARUSAT", titleFont);
                orgName.Alignment = Element.ALIGN_CENTER;
                orgName.SpacingAfter = 20;
                document.Add(orgName);

                // Certificate Title
                Paragraph title = new Paragraph("CERTIFICATE OF COMPLETION", headerFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 40;
                document.Add(title);

                // This is to certify that
                Paragraph certifyText = new Paragraph("This is to certify that", normalFont);
                certifyText.Alignment = Element.ALIGN_CENTER;
                certifyText.SpacingAfter = 20;
                document.Add(certifyText);

                // Employee Name
                string fullName = $"{registration.EmployeeSys.FirstName} {registration.EmployeeSys.MiddleName} {registration.EmployeeSys.LastName}";
                Paragraph name = new Paragraph(fullName, nameFont);
                name.Alignment = Element.ALIGN_CENTER;
                name.SpacingAfter = 20;
                document.Add(name);

                // Department and Designation
                Paragraph position = new Paragraph($"{registration.EmployeeSys.Designation}, {registration.EmployeeSys.Department}", normalFont);
                position.Alignment = Element.ALIGN_CENTER;
                position.SpacingAfter = 30;
                document.Add(position);

                // Training text
                Paragraph trainingText = new Paragraph("has successfully completed the training program on", normalFont);
                trainingText.Alignment = Element.ALIGN_CENTER;
                trainingText.SpacingAfter = 20;
                document.Add(trainingText);

                // Training Title
                Paragraph trainingTitle = new Paragraph(registration.TrainingSys.Title, headerFont);
                trainingTitle.Alignment = Element.ALIGN_CENTER;
                trainingTitle.SpacingAfter = 20;
                document.Add(trainingTitle);

                // Training Duration
                string duration = $"conducted from {registration.TrainingSys.StartDate.ToString("MMMM d, yyyy")} to {registration.TrainingSys.EndDate.ToString("MMMM d, yyyy")}";
                Paragraph durationText = new Paragraph(duration, normalFont);
                durationText.Alignment = Element.ALIGN_CENTER;
                durationText.SpacingAfter = 50;
                document.Add(durationText);

                // Date and Signature
                DateOnly issueDate = DateOnly.FromDateTime(DateTime.Today);
                
                // Add signature image on the right side
                if (File.Exists(signaturePath))
                {
                    using var signStream = new FileStream(signaturePath, FileMode.Open, FileAccess.Read);
                    iTextSharp.text.Image signatureImage = iTextSharp.text.Image.GetInstance(signStream);
                    
                    // Set signature size (adjust as needed)
                    signatureImage.ScaleToFit(150, 75);
                    
                    // Position at bottom right with some margin
                    float signatureX = document.PageSize.Width - 200; // From right margin
                    float signatureY = 100; // From bottom margin
                    signatureImage.SetAbsolutePosition(signatureX, signatureY);
                    
                    document.Add(signatureImage);
                    
                    // Add signature text
                    Paragraph signatureText = new Paragraph("Director, HRDC", normalFont);
                    signatureText.Alignment = Element.ALIGN_RIGHT;
                    signatureText.IndentationRight = 80;
                    document.Add(signatureText);
                }

                // Date on the left side
                Paragraph issueDateText = new Paragraph($"Issue Date: {issueDate.ToString("MMMM d, yyyy")}", normalFont);
                issueDateText.Alignment = Element.ALIGN_LEFT;
                issueDateText.IndentationLeft = 80;
                document.Add(issueDateText);

                // Close the document
                document.Close();
                writer.Close();

                // Return the relative path to the certificate
                return $"/images/certificates/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for registration {RegistrationId}", registration.TrainingRegSysID);
                throw;
            }
        }

        public async Task<bool> SendCertificateEmailAsync(string employeeEmail, string employeeName, string trainingTitle, string certificatePath)
        {
            try
            {
                // Construct the physical path to the certificate
                string physicalPath = Path.Combine(_hostingEnvironment.WebRootPath, certificatePath.TrimStart('/'));
                
                if (!File.Exists(physicalPath))
                {
                    _logger.LogError("Certificate file not found at {Path}", physicalPath);
                    return false;
                }

                // Prepare email content
                string subject = "Your HRDC Training Certificate";
                string body = $@"
                    <html>
                    <body>
                        <p>Dear {employeeName},</p>
                        <p>Your certificate for <strong>{trainingTitle}</strong> has been generated. You can download it from your HRDC portal.</p>
                        <p>The certificate is also attached to this email for your reference.</p>
                        <p>Regards,</p>
                        <p>Human Resource Development Centre (HRDC)<br/>CHARUSAT</p>
                    </body>
                    </html>";

                // Create email attachment
                Attachment attachment = new Attachment(physicalPath, MediaTypeNames.Application.Pdf);
                
                // Send the email using the email service
                bool success = await _emailService.SendEmailWithAttachmentAsync(
                    employeeEmail, 
                    subject, 
                    body, 
                    attachment);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending certificate email to {Email}", employeeEmail);
                return false;
            }
        }

        public async Task<bool> GenerateAllCertificatesForTrainingAsync(int trainingSysId)
        {
            // This will be implemented in the controller class that has access to DbContext
            // We'll just define the interface method here
            throw new NotImplementedException("This method is implemented in the controller");
        }

        private async Task CreateDefaultTemplateAsync(string templatePath)
        {
            try
            {
                // Create a basic template if one doesn't exist
                using (var bitmap = new System.Drawing.Bitmap(1754, 1240)) // A4 landscape at 150 DPI
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.White), 0, 0, bitmap.Width, bitmap.Height);
                        
                        // Add a border
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 180, 180), 5))
                        {
                            g.DrawRectangle(pen, 20, 20, bitmap.Width - 40, bitmap.Height - 40);
                        }

                        // Add some decorative elements (e.g., a corner design)
                        using (var cornerPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 100, 220), 3))
                        {
                            // Top-left corner
                            g.DrawLine(cornerPen, 20, 60, 100, 60);
                            g.DrawLine(cornerPen, 60, 20, 60, 100);
                            
                            // Top-right corner
                            g.DrawLine(cornerPen, bitmap.Width - 100, 60, bitmap.Width - 20, 60);
                            g.DrawLine(cornerPen, bitmap.Width - 60, 20, bitmap.Width - 60, 100);
                            
                            // Bottom-left corner
                            g.DrawLine(cornerPen, 20, bitmap.Height - 60, 100, bitmap.Height - 60);
                            g.DrawLine(cornerPen, 60, bitmap.Height - 100, 60, bitmap.Height - 20);
                            
                            // Bottom-right corner
                            g.DrawLine(cornerPen, bitmap.Width - 100, bitmap.Height - 60, bitmap.Width - 20, bitmap.Height - 60);
                            g.DrawLine(cornerPen, bitmap.Width - 60, bitmap.Height - 100, bitmap.Width - 60, bitmap.Height - 20);
                        }
                    }

                    // Make sure the directory exists
                    string directory = Path.GetDirectoryName(templatePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    bitmap.Save(templatePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default certificate template");
            }
        }

        private async Task CreateDefaultSignatureAsync(string signaturePath)
        {
            try
            {
                // Create a basic signature image if one doesn't exist
                using (var bitmap = new System.Drawing.Bitmap(300, 100))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.White), 0, 0, bitmap.Width, bitmap.Height);
                        
                        // Draw some lines to simulate a signature
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
                        {
                            // Simulated signature pattern
                            g.DrawLine(pen, 20, 60, 80, 40);
                            g.DrawLine(pen, 80, 40, 120, 70);
                            g.DrawLine(pen, 120, 70, 180, 30);
                            g.DrawLine(pen, 180, 30, 260, 80);
                        }
                    }

                    // Make sure the directory exists
                    string directory = Path.GetDirectoryName(signaturePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    bitmap.Save(signaturePath, System.Drawing.Imaging.ImageFormat.Png);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default signature image");
            }
        }
    }
}
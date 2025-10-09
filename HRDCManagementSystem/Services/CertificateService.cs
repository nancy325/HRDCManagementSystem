using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Utilities;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Exceptions;
using iText.Kernel.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.IO;

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
                _logger.LogInformation("Starting certificate generation for registration ID: {RegistrationId}", registration.TrainingRegSysID);
                
                // Log OS information
                string osInfo = RuntimeInformation.OSDescription;
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                _logger.LogInformation("Running on OS: {OS}, IsWindows: {IsWindows}", osInfo, isWindows);
                
                // Create directories if they don't exist
                var certificatesDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates");
                _logger.LogInformation("Certificate directory path: {Path}", certificatesDirectory);
                
                if (!Directory.Exists(certificatesDirectory))
                {
                    _logger.LogInformation("Creating certificates directory");
                    Directory.CreateDirectory(certificatesDirectory);
                }

                var signatureDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "images", "signature");
                _logger.LogInformation("Signature directory path: {Path}", signatureDirectory);
                
                if (!Directory.Exists(signatureDirectory))
                {
                    _logger.LogInformation("Creating signature directory");
                    Directory.CreateDirectory(signatureDirectory);
                }

                // Make sure we have all required data
                if (registration.EmployeeSys == null)
                {
                    _logger.LogError("Registration {RegistrationId} is missing employee data", registration.TrainingRegSysID);
                    throw new InvalidOperationException("Registration is missing employee details");
                }
                
                if (registration.TrainingSys == null)
                {
                    _logger.LogError("Registration {RegistrationId} is missing training data", registration.TrainingRegSysID);
                    throw new InvalidOperationException("Registration is missing training details");
                }

                // Prepare file paths
                string templatePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates", "template.jpg");
                _logger.LogInformation("Template path: {Path}, Exists: {Exists}", templatePath, File.Exists(templatePath));
                
                string signaturePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates", "sign.jpg");
                _logger.LogInformation("Signature path: {Path}, Exists: {Exists}", signaturePath, File.Exists(signaturePath));
                
                // Create default template if it doesn't exist
                if (!File.Exists(templatePath))
                {
                    _logger.LogInformation("Creating default certificate template");
                    bool templateCreated = await ImageUtility.CreateDefaultCertificateTemplateAsync(templatePath, _logger);
                    _logger.LogInformation("Template creation result: {Result}", templateCreated);
                    
                    // Double check template was created
                    if (!File.Exists(templatePath))
                    {
                        _logger.LogError("Failed to create template file at {Path}", templatePath);
                        throw new FileNotFoundException("Certificate template file could not be created", templatePath);
                    }
                }

                // Create default signature if it doesn't exist
                if (!File.Exists(signaturePath))
                {
                    _logger.LogInformation("Creating default signature");
                    bool signatureCreated = await ImageUtility.CreateDefaultSignatureAsync(signaturePath, _logger);
                    _logger.LogInformation("Signature creation result: {Result}", signatureCreated);
                    
                    // Double check signature was created
                    if (!File.Exists(signaturePath))
                    {
                        _logger.LogError("Failed to create signature file at {Path}", signaturePath);
                        throw new FileNotFoundException("Signature file could not be created", signaturePath);
                    }
                }

                // Set up the PDF document
                string fileName = $"{registration.TrainingRegSysID}.pdf";
                string outputPath = Path.Combine(certificatesDirectory, fileName);
                _logger.LogInformation("Output PDF path: {Path}", outputPath);
                
                // Make sure the output directory exists
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir) && !string.IsNullOrEmpty(outputDir))
                {
                    _logger.LogInformation("Creating output directory: {Directory}", outputDir);
                    Directory.CreateDirectory(outputDir);
                }
                
                // Check if we have write permission
                try
                {
                    // Test if we can create a file in the directory
                    string testFilePath = Path.Combine(certificatesDirectory, "write_test.txt");
                    File.WriteAllText(testFilePath, "Testing write permissions");
                    File.Delete(testFilePath);
                    _logger.LogInformation("Write permission check passed for directory: {Directory}", certificatesDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write test file to directory {Directory}", certificatesDirectory);
                    throw new UnauthorizedAccessException($"Cannot write to certificate directory {certificatesDirectory}: {ex.Message}", ex);
                }
                
                // Delete existing file if it exists to avoid any issues
                if (File.Exists(outputPath))
                {
                    _logger.LogInformation("Removing existing certificate file at {Path}", outputPath);
                    try
                    {
                        File.Delete(outputPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete existing certificate file at {Path}", outputPath);
                        // Continue anyway - we'll try to overwrite it
                    }
                }
                
                // Check template and signature files
                if (new FileInfo(templatePath).Length == 0)
                {
                    _logger.LogError("Template file is empty at {Path}", templatePath);
                    throw new System.IO.IOException("Certificate template file is empty or corrupted");
                }
                
                if (new FileInfo(signaturePath).Length == 0)
                {
                    _logger.LogError("Signature file is empty at {Path}", signaturePath);
                    throw new System.IO.IOException("Signature file is empty or corrupted");
                }

                try
                {
                    // Generate certificate using iText7
                    _logger.LogInformation("Creating PDF writer and document");
                    
                    // Create a new PDF document
                    using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        PdfWriter writer = new PdfWriter(fs);
                        PdfDocument pdf = new PdfDocument(writer);
                        
                        try
                        {
                            // Landscape orientation
                            pdf.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4.Rotate());
                            Document document = new Document(pdf);

                            // Add the background image
                            if (File.Exists(templatePath))
                            {
                                _logger.LogInformation("Adding background template to PDF");
                                try
                                {
                                    ImageData imageData = ImageDataFactory.Create(templatePath);
                                    iText.Layout.Element.Image backgroundImage = new iText.Layout.Element.Image(imageData);
                                    backgroundImage.SetFixedPosition(0, 0);
                                    backgroundImage.SetWidth(pdf.GetDefaultPageSize().GetWidth());
                                    backgroundImage.SetHeight(pdf.GetDefaultPageSize().GetHeight());
                                    document.Add(backgroundImage);
                                    _logger.LogInformation("Background template added successfully");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error adding background template to PDF");
                                    // Continue without the background
                                }
                            }

                            // Font setup - Using standard fonts
                            _logger.LogInformation("Setting up fonts");
                            PdfFont standardFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                            // Add content
                            _logger.LogInformation("Adding certificate content");
                            
                            // Organization Name
                            Paragraph orgName = new Paragraph("Human Resource Development Centre (HRDC), CHARUSAT")
                                .SetFont(boldFont)
                                .SetFontSize(24)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20);
                            document.Add(orgName);

                            // Certificate Title
                            Paragraph title = new Paragraph("CERTIFICATE OF COMPLETION")
                                .SetFont(boldFont)
                                .SetFontSize(18)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(40);
                            document.Add(title);

                            // This is to certify that
                            Paragraph certifyText = new Paragraph("This is to certify that")
                                .SetFont(standardFont)
                                .SetFontSize(14)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20);
                            document.Add(certifyText);

                            // Employee Name - Handle null values safely
                            string firstName = registration.EmployeeSys.FirstName ?? string.Empty;
                            string middleName = registration.EmployeeSys.MiddleName ?? string.Empty;
                            string lastName = registration.EmployeeSys.LastName ?? string.Empty;
                            string fullName = $"{firstName} {middleName} {lastName}".Trim();
                            if (string.IsNullOrWhiteSpace(fullName))
                            {
                                fullName = "Employee"; // Default if name is empty
                            }
                            
                            _logger.LogInformation("Adding employee name: {Name}", fullName);
                            
                            Paragraph name = new Paragraph(fullName)
                                .SetFont(boldFont)
                                .SetFontSize(22)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20);
                            document.Add(name);

                            // Department and Designation - Handle null values safely
                            string designation = registration.EmployeeSys.Designation ?? "Employee";
                            string department = registration.EmployeeSys.Department ?? "Department";
                            
                            Paragraph position = new Paragraph($"{designation}, {department}")
                                .SetFont(standardFont)
                                .SetFontSize(14)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(30);
                            document.Add(position);

                            // Training text
                            Paragraph trainingText = new Paragraph("has successfully completed the training program on")
                                .SetFont(standardFont)
                                .SetFontSize(14)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20);
                            document.Add(trainingText);

                            // Training Title
                            string trainingTitle = registration.TrainingSys.Title ?? "Training Program";
                            _logger.LogInformation("Adding training title: {Title}", trainingTitle);
                            
                            Paragraph trainingTitlePara = new Paragraph(trainingTitle)
                                .SetFont(boldFont)
                                .SetFontSize(18)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20);
                            document.Add(trainingTitlePara);

                            // Training Duration
                            string startDateStr = registration.TrainingSys.StartDate.ToString("MMMM d, yyyy");
                            string endDateStr = registration.TrainingSys.EndDate.ToString("MMMM d, yyyy");
                            string duration = $"conducted from {startDateStr} to {endDateStr}";
                            _logger.LogInformation("Adding duration: {Duration}", duration);
                            

                            Paragraph durationText = new Paragraph(duration)
                                .SetFont(standardFont)
                                .SetFontSize(14)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(50);
                            document.Add(durationText);

                            // Date and Signature
                            DateOnly issueDate = DateOnly.FromDateTime(DateTime.Today);
                            
                            // Add signature image on the right side
                            if (File.Exists(signaturePath))
                            {
                                _logger.LogInformation("Adding signature image");
                                try
                                {
                                    ImageData signData = ImageDataFactory.Create(signaturePath);
                                    iText.Layout.Element.Image signatureImage = new iText.Layout.Element.Image(signData);
                                    
                                    // Set signature size and position
                                    float signatureWidth = 150;
                                    float signatureHeight = 75;
                                    signatureImage.ScaleToFit(signatureWidth, signatureHeight);
                                    
                                    // Position at bottom right with some margin
                                    float pageWidth = pdf.GetDefaultPageSize().GetWidth();
                                    signatureImage.SetFixedPosition(pageWidth - 200, 100);
                                    document.Add(signatureImage);
                                    
                                    // Add signature text
                                    Paragraph signatureText = new Paragraph("Director, HRDC")
                                        .SetFont(standardFont)
                                        .SetFontSize(14)
                                        .SetTextAlignment(TextAlignment.RIGHT)
                                        .SetMarginRight(80);
                                    document.Add(signatureText);
                                    _logger.LogInformation("Signature added successfully");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error adding signature to PDF");
                                    // Continue without the signature
                                }
                            }

                            // Date on the left side
                            Paragraph issueDateText = new Paragraph($"Issue Date: {issueDate.ToString("MMMM d, yyyy")}")
                                .SetFont(standardFont)
                                .SetFontSize(14)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginLeft(80);
                            document.Add(issueDateText);

                            _logger.LogInformation("Closing PDF document");
                            document.Close();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during PDF document creation/writing");
                            
                            // Make sure resources are disposed
                            try { pdf.Close(); } catch { }
                            try { writer.Close(); } catch { }
                            
                            throw;
                        }
                    }
                }
                catch (System.IO.IOException ioEx)
                {
                    _logger.LogError(ioEx, "System IO Exception when creating PDF: {Message}", ioEx.Message);
                    throw new System.IO.IOException($"Failed to create PDF file: {ioEx.Message}", ioEx);
                }
                catch (iText.IO.Exceptions.IOException ioEx)
                {
                    _logger.LogError(ioEx, "iText IO Exception when creating PDF: {Message}", ioEx.Message);
                    throw;
                }
                catch (PdfException pdfEx)
                {
                    _logger.LogError(pdfEx, "PDF Exception: {Message}", pdfEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during PDF document creation: {ExType}: {Message}", ex.GetType().Name, ex.Message);
                    throw;
                }

                // Verify the file was created successfully
                if (!File.Exists(outputPath))
                {
                    _logger.LogError("PDF file was not created at {Path}", outputPath);
                    throw new FileNotFoundException("Certificate PDF file was not created", outputPath);
                }
                
                // Check if the file is valid
                try
                {
                    var fileInfo = new FileInfo(outputPath);
                    if (fileInfo.Length == 0)
                    {
                        _logger.LogError("Created PDF file is empty at {Path}", outputPath);
                        throw new System.IO.IOException("Certificate PDF file was created but is empty");
                    }
                    _logger.LogInformation("Certificate file created successfully with size: {Size} bytes", fileInfo.Length);
                }
                catch (Exception ex) when (!(ex is System.IO.IOException))
                {
                    _logger.LogError(ex, "Error checking PDF file at {Path}", outputPath);
                    // Continue anyway since the file was created
                }

                _logger.LogInformation("Certificate generation completed successfully");
                // Return the relative path to the certificate
                return $"/images/certificates/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for registration {RegistrationId}: {ExType}: {Message}", 
                    registration.TrainingRegSysID, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        public async Task<bool> SendCertificateEmailAsync(string employeeEmail, string employeeName, string trainingTitle, string certificatePath)
        {
            try
            {
                _logger.LogInformation("Sending certificate email to: {Email}", employeeEmail);
                
                // Construct the physical path to the certificate
                string physicalPath = Path.Combine(_hostingEnvironment.WebRootPath, certificatePath.TrimStart('/'));
                _logger.LogInformation("Certificate physical path: {Path}, Exists: {Exists}", physicalPath, File.Exists(physicalPath));
                
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
                _logger.LogInformation("Creating email attachment");
                using Attachment attachment = new Attachment(physicalPath, MediaTypeNames.Application.Pdf);
                
                // Send the email using the email service
                _logger.LogInformation("Sending email with attachment");
                bool success = await _emailService.SendEmailWithAttachmentAsync(
                    employeeEmail, 
                    subject, 
                    body, 
                    attachment);

                _logger.LogInformation("Email sending result: {Success}", success);
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
    }
}
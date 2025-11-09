using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using HRDCManagementSystem.Utilities;
using iText.Kernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace HRDCManagementSystem.Controllers
{
    public class CertificateController : Controller
    {
        private readonly HRDCContext _context;
        private readonly ICertificateService _certificateService;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CertificateController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public CertificateController(
            HRDCContext context,
            ICertificateService certificateService,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            ILogger<CertificateController> logger,
            IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _certificateService = certificateService;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Fetching all certificates for admin view");

                // First check if we have any certificates at all (for debugging)
                var allCertificatesCount = await _context.Certificates.CountAsync();
                _logger.LogInformation($"Total certificates in database: {allCertificatesCount}");

                // Get the filtered certificates with all required relations
                var certificates = await _context.Certificates
                    .Include(c => c.RegSys)
                    .ThenInclude(r => r.EmployeeSys)
                    .Include(c => c.RegSys.TrainingSys)
                    .Where(c => c.RecStatus == "active")
                    .OrderByDescending(c => c.CreateDateTime)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {certificates.Count} active certificates");

                // Filter generated certificates in memory to debug potential issues with IsGenerated field
                var generatedCertificates = certificates.Where(c => c.IsGenerated == true).ToList();
                _logger.LogInformation($"After filtering for IsGenerated=true: {generatedCertificates.Count} certificates");

                // Log any certificate where IsGenerated is not true for debugging
                var nonGeneratedCerts = certificates.Where(c => c.IsGenerated != true).ToList();
                if (nonGeneratedCerts.Any())
                {
                    _logger.LogWarning($"Found {nonGeneratedCerts.Count} certificates with IsGenerated != true");
                    foreach (var cert in nonGeneratedCerts.Take(5)) // Log first 5 for debugging
                    {
                        _logger.LogWarning($"Certificate {cert.CertificateSysID}: IsGenerated={cert.IsGenerated}, RecStatus={cert.RecStatus}");
                    }
                }

                // Check if any certificate has null navigation properties
                var invalidCerts = certificates.Where(c =>
                    c.RegSys == null ||
                    c.RegSys.EmployeeSys == null ||
                    c.RegSys.TrainingSys == null).ToList();

                if (invalidCerts.Any())
                {
                    _logger.LogWarning($"Found {invalidCerts.Count} certificates with missing related entities");
                }

                // Transform valid certificates to view models
                var viewModel = new List<CertificateViewModel>();

                foreach (var certificate in certificates)
                {
                    try
                    {
                        // Only process certificates with all required navigation properties
                        if (certificate.RegSys?.EmployeeSys != null && certificate.RegSys?.TrainingSys != null)
                        {
                            string firstName = certificate.RegSys.EmployeeSys.FirstName ?? string.Empty;
                            string middleName = certificate.RegSys.EmployeeSys.MiddleName ?? string.Empty;
                            string lastName = certificate.RegSys.EmployeeSys.LastName ?? string.Empty;
                            string employeeName = $"{firstName} {middleName} {lastName}".Trim();
                            if (string.IsNullOrWhiteSpace(employeeName))
                            {
                                employeeName = "Unknown Employee";
                            }

                            viewModel.Add(new CertificateViewModel
                            {
                                CertificateSysID = certificate.CertificateSysID,
                                TrainingRegSysID = certificate.TrainingRegSysID,
                                EmployeeName = employeeName,
                                TrainingTitle = certificate.RegSys.TrainingSys.Title ?? "Unknown Training",
                                IssueDate = certificate.IssueDate ?? DateOnly.FromDateTime(certificate.CreateDateTime ?? DateTime.Now),
                                CertificatePath = certificate.CertificatePath,
                                IsGenerated = certificate.IsGenerated ?? false
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing certificate {certificate.CertificateSysID}");
                    }
                }

                _logger.LogInformation($"Returning {viewModel.Count} certificate view models");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates for admin view");
                TempData["ErrorMessage"] = "An error occurred while retrieving certificates.";
                return View(new List<CertificateViewModel>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TrainingCertificates(int trainingId)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == trainingId && t.RecStatus == "active");

            if (training == null)
            {
                TempData["ErrorMessage"] = "Training not found.";
                return RedirectToAction("Index", "Training");
            }

            // Get all registrations that are confirmed for this training
            var registrations = await _context.TrainingRegistrations
                .Include(r => r.EmployeeSys)
                .ThenInclude(e => e.UserSys)
                .Where(r => r.TrainingSysID == trainingId && r.RecStatus == "active" && r.Confirmation)
                .ToListAsync();

            // Get certificate status for each registration
            var certificateStatusList = new List<RegistrationCertificateViewModel>();

            foreach (var reg in registrations)
            {
                // Check if certificate exists
                var certificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.TrainingRegSysID == reg.TrainingRegSysID && c.RecStatus == "active");

                string firstName = reg.EmployeeSys.FirstName ?? string.Empty;
                string middleName = reg.EmployeeSys.MiddleName ?? string.Empty;
                string lastName = reg.EmployeeSys.LastName ?? string.Empty;
                string employeeName = $"{firstName} {middleName} {lastName}".Trim();
                if (string.IsNullOrWhiteSpace(employeeName))
                {
                    employeeName = "Unknown Employee";
                }

                certificateStatusList.Add(new RegistrationCertificateViewModel
                {
                    TrainingRegSysID = reg.TrainingRegSysID,
                    EmployeeSysID = reg.EmployeeSysID,
                    EmployeeName = employeeName,
                    Department = reg.EmployeeSys.Department ?? string.Empty,
                    Designation = reg.EmployeeSys.Designation ?? string.Empty,
                    Email = reg.EmployeeSys.UserSys?.Email,
                    HasCertificate = certificate != null && certificate.IsGenerated == true,
                    CertificateSysID = certificate?.CertificateSysID,
                    CertificatePath = certificate?.CertificatePath,
                    IssueDate = certificate?.IssueDate
                });
            }

            var viewModel = new TrainingCertificatesViewModel
            {
                TrainingSysID = trainingId,
                TrainingTitle = training.Title,
                StartDate = training.StartDate,
                EndDate = training.EndDate,
                Registrations = certificateStatusList
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCertificate(int trainingRegId)
        {
            try
            {
                _logger.LogInformation("GenerateCertificate: Starting certificate generation for registration ID {RegId}", trainingRegId);

                // Get the registration with all required data
                var registration = await _context.TrainingRegistrations
                    .Include(r => r.EmployeeSys)
                    .ThenInclude(e => e.UserSys)
                    .Include(r => r.TrainingSys)
                    .FirstOrDefaultAsync(r => r.TrainingRegSysID == trainingRegId && r.RecStatus == "active");

                if (registration == null)
                {
                    _logger.LogWarning("Registration {RegId} not found or not active", trainingRegId);
                    TempData["ErrorMessage"] = "Registration not found.";
                    return RedirectToAction("Index", "Training");
                }

                // Check if this is a confirmed registration
                if (!registration.Confirmation)
                {
                    _logger.LogWarning("Registration {RegId} is not confirmed", trainingRegId);
                    TempData["ErrorMessage"] = "Cannot generate certificate for unconfirmed registration.";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                // Validate registration has required related entities
                if (registration.EmployeeSys == null)
                {
                    _logger.LogError("Registration {RegId} is missing employee data", trainingRegId);
                    TempData["ErrorMessage"] = "Registration is missing employee details. Please ensure the employee record exists.";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                if (registration.TrainingSys == null)
                {
                    _logger.LogError("Registration {RegId} is missing training data", trainingRegId);
                    TempData["ErrorMessage"] = "Registration is missing training details. Please ensure the training record exists.";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                // Check if certificate already exists
                var existingCertificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.TrainingRegSysID == trainingRegId && c.RecStatus == "active");

                Certificate certificate;
                string certificatePath = string.Empty;

                try
                {
                    // Generate the certificate using the updated service
                    _logger.LogInformation("Calling certificate service to generate PDF");
                    certificatePath = await _certificateService.GenerateCertificateAsync(registration);
                    _logger.LogInformation("Certificate generated successfully at {Path}", certificatePath);
                }
                catch (PdfException pdfEx)
                {
                    _logger.LogError(pdfEx, "PDF Exception during certificate generation: {Message}", pdfEx.Message);
                    TempData["ErrorMessage"] = $"Failed to generate PDF certificate: {pdfEx.Message}";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }
                catch (System.IO.IOException ioEx)
                {
                    _logger.LogError(ioEx, "IO Exception during certificate generation: {Message}", ioEx.Message);
                    TempData["ErrorMessage"] = $"File access error during certificate generation: {ioEx.Message}";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }
                catch (iText.IO.Exceptions.IOException ioEx)
                {
                    _logger.LogError(ioEx, "iText IO Exception during certificate generation: {Message}", ioEx.Message);
                    TempData["ErrorMessage"] = $"PDF file access error during certificate generation: {ioEx.Message}";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling certificate service to generate PDF");
                    TempData["ErrorMessage"] = $"Failed to generate certificate: {ex.GetType().Name} - {ex.Message}";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                // Get current date
                DateOnly issueDate = DateOnly.FromDateTime(DateTime.Today);

                // Verify the certificate file was created
                string fullPath = Path.Combine(_hostingEnvironment.WebRootPath, certificatePath.TrimStart('/'));
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogError("Certificate file not found at {Path} after generation", fullPath);
                    TempData["ErrorMessage"] = "Certificate file was not created successfully.";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                if (existingCertificate != null)
                {
                    // Update existing certificate
                    _logger.LogInformation("Updating existing certificate record");
                    existingCertificate.IssueDate = issueDate;
                    existingCertificate.IsGenerated = true;
                    existingCertificate.CertificatePath = certificatePath;
                    existingCertificate.ModifiedDateTime = DateTime.Now;
                    existingCertificate.ModifiedUserId = _currentUserService.GetCurrentUserId();

                    _context.Certificates.Update(existingCertificate);
                    certificate = existingCertificate;
                }
                else
                {
                    // Create new certificate entry
                    _logger.LogInformation("Creating new certificate record");
                    certificate = new Certificate
                    {
                        TrainingRegSysID = trainingRegId,
                        IssueDate = issueDate,
                        IsGenerated = true,
                        CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                        CreatedTime = TimeOnly.FromDateTime(DateTime.Now),
                        CertificatePath = certificatePath,
                        RecStatus = "active", // Ensure record status is set to active
                    };

                    _context.Certificates.Add(certificate);
                }

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Certificate record saved to database successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving certificate to database");
                    TempData["ErrorMessage"] = "Certificate was generated but could not be saved to database.";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                // Send email with certificate to employee if email is available
                if (registration.EmployeeSys?.UserSys?.Email != null)
                {
                    try
                    {
                        string employeeName = $"{registration.EmployeeSys.FirstName} {registration.EmployeeSys.LastName}";
                        _logger.LogInformation("Sending certificate email to {Email}", registration.EmployeeSys.UserSys.Email);

                        bool emailSent = await _certificateService.SendCertificateEmailAsync(
                            registration.EmployeeSys.UserSys.Email,
                            employeeName,
                            registration.TrainingSys.Title,
                            certificatePath);

                        if (!emailSent)
                        {
                            _logger.LogWarning("Failed to send certificate email to {Email}", registration.EmployeeSys.UserSys.Email);
                        }

                        // Send notification to employee about certificate generation
                        await NotificationUtility.NotifyCertificateGenerated(
                            _notificationService,
                            certificate
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending certificate email");
                        // Continue anyway since the certificate was generated
                    }
                }
                else
                {
                    _logger.LogWarning("Employee {EmployeeId} has no email address for sending certificate",
                        registration.EmployeeSysID);
                }

                TempData["SuccessMessage"] = "Certificate generated successfully.";
                return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error generating certificate for registration {RegId}", trainingRegId);
                TempData["ErrorMessage"] = $"Error generating certificate: {ex.GetType().Name} - {ex.Message}";
                return RedirectToAction("Index", "Training");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAllCertificates(int trainingId)
        {
            try
            {
                var training = await _context.TrainingPrograms
                    .FirstOrDefaultAsync(t => t.TrainingSysID == trainingId && t.RecStatus == "active");

                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training not found.";
                    return RedirectToAction("Index", "Training");
                }

                // Get all confirmed registrations for this training
                var registrations = await _context.TrainingRegistrations
                    .Include(r => r.EmployeeSys)
                    .ThenInclude(e => e.UserSys)
                    .Include(r => r.TrainingSys)
                    .Where(r => r.TrainingSysID == trainingId && r.RecStatus == "active" && r.Confirmation == true)
                    .ToListAsync();

                if (!registrations.Any())
                {
                    TempData["ErrorMessage"] = "No confirmed registrations found for this training.";
                    return RedirectToAction("TrainingCertificates", new { trainingId });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var registration in registrations)
                {
                    try
                    {
                        // Generate the certificate using the updated service
                        string certificatePath = await _certificateService.GenerateCertificateAsync(registration);

                        // Get current date
                        DateOnly issueDate = DateOnly.FromDateTime(DateTime.Today);

                        // Check if certificate already exists
                        var existingCertificate = await _context.Certificates
                            .FirstOrDefaultAsync(c => c.TrainingRegSysID == registration.TrainingRegSysID && c.RecStatus == "active");

                        Certificate certificate;

                        if (existingCertificate != null)
                        {
                            // Update existing certificate
                            existingCertificate.IssueDate = issueDate;
                            existingCertificate.IsGenerated = true;
                            existingCertificate.CertificatePath = certificatePath;
                            existingCertificate.ModifiedDateTime = DateTime.Now;
                            existingCertificate.ModifiedUserId = _currentUserService.GetCurrentUserId();

                            _context.Certificates.Update(existingCertificate);
                            certificate = existingCertificate;
                        }
                        else
                        {
                            // Create new certificate entry
                            certificate = new Certificate
                            {
                                TrainingRegSysID = registration.TrainingRegSysID,
                                IssueDate = issueDate,
                                IsGenerated = true,
                                CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                                CreatedTime = TimeOnly.FromDateTime(DateTime.Now),
                                CertificatePath = certificatePath,
                                RecStatus = "active", // Ensure record status is set to active
                            };

                            _context.Certificates.Add(certificate);
                        }

                        await _context.SaveChangesAsync();

                        // Send email with certificate to employee if email is available
                        if (registration.EmployeeSys?.UserSys?.Email != null)
                        {
                            try
                            {
                                string employeeName = $"{registration.EmployeeSys.FirstName} {registration.EmployeeSys.LastName}";
                                await _certificateService.SendCertificateEmailAsync(
                                    registration.EmployeeSys.UserSys.Email,
                                    employeeName,
                                    registration.TrainingSys.Title,
                                    certificatePath);

                                // Send notification to employee about certificate generation
                                await NotificationUtility.NotifyCertificateGenerated(
                                    _notificationService,
                                    certificate
                                );
                            }
                            catch (Exception)
                            {
                                // Continue to next certificate even if email fails
                            }
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating certificate for registration {RegId}", registration.TrainingRegSysID);
                        failCount++;
                    }
                }

                if (failCount > 0)
                {
                    TempData["WarningMessage"] = $"Generated {successCount} certificates successfully. Failed to generate {failCount} certificates.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Generated {successCount} certificates successfully.";
                }

                return RedirectToAction("TrainingCertificates", new { trainingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating all certificates for training {TrainingId}", trainingId);
                TempData["ErrorMessage"] = $"Error generating certificates: {ex.Message}";
                return RedirectToAction("TrainingCertificates", new { trainingId });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyCertificates()
        {
            var currentUserId = _currentUserService.GetCurrentUserId();

            // Get employee ID
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserSysID == currentUserId);

            if (employee == null)
            {
                return NotFound("Employee record not found");
            }

            // Get all certificates for this employee
            var certificates = await _context.Certificates
                .Include(c => c.RegSys)
                .ThenInclude(r => r.TrainingSys)
                .Where(c => c.RegSys.EmployeeSysID == employee.EmployeeSysID &&
                       c.IsGenerated == true && c.RecStatus == "active")
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();

            var viewModel = certificates.Select(c => new MyCertificateViewModel
            {
                CertificateSysID = c.CertificateSysID,
                TrainingTitle = c.RegSys?.TrainingSys?.Title ?? "Unknown Training",
                StartDate = c.RegSys?.TrainingSys?.StartDate ?? DateOnly.FromDateTime(DateTime.Today),
                EndDate = c.RegSys?.TrainingSys?.EndDate ?? DateOnly.FromDateTime(DateTime.Today),
                IssueDate = c.IssueDate ?? DateOnly.FromDateTime(c.CreateDateTime ?? DateTime.Now),
                CertificatePath = c.CertificatePath
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Download(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.RegSys)
                .ThenInclude(r => r.TrainingSys)
                .Include(c => c.RegSys.EmployeeSys)
                .FirstOrDefaultAsync(c => c.CertificateSysID == id && c.IsGenerated == true && c.RecStatus == "active");

            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Certificate not found.";
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("MyCertificates");
                }
            }

            // Check authorization - admin can download any certificate, employee can only download their own
            if (User.IsInRole("Employee"))
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserSysID == currentUserId);

                if (employee == null || certificate.RegSys.EmployeeSysID != employee.EmployeeSysID)
                {
                    return Forbid();
                }
            }

            // Check if the certificate file exists
            if (string.IsNullOrEmpty(certificate.CertificatePath))
            {
                TempData["ErrorMessage"] = "Certificate file path not found.";
                return RedirectToAction(User.IsInRole("Admin") ? "Index" : "MyCertificates");
            }

            string filePath = Path.Combine(_hostingEnvironment.WebRootPath, certificate.CertificatePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Certificate file not found.";
                return RedirectToAction(User.IsInRole("Admin") ? "Index" : "MyCertificates");
            }

            // Generate a filename for download
            string firstName = certificate.RegSys?.EmployeeSys?.FirstName ?? string.Empty;
            string lastName = certificate.RegSys?.EmployeeSys?.LastName ?? string.Empty;
            string employeeName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                employeeName = "Employee";
            }
            string trainingTitle = certificate.RegSys?.TrainingSys?.Title ?? "Training";
            // Sanitize filename - remove invalid characters
            string sanitizedEmployeeName = employeeName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
            string sanitizedTrainingTitle = trainingTitle.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
            string fileName = $"{sanitizedEmployeeName}_{sanitizedTrainingTitle}_Certificate.pdf";

            // Return the file
            return PhysicalFile(filePath, "application/pdf", fileName);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Diagnostics()
        {
            try
            {
                // Create a diagnostic report on the certificate generation environment
                var diagnosticInfo = new Dictionary<string, string>();

                // System info
                diagnosticInfo["OS"] = RuntimeInformation.OSDescription;
                diagnosticInfo["Framework"] = RuntimeInformation.FrameworkDescription;
                diagnosticInfo["IsWindows"] = RuntimeInformation.IsOSPlatform(OSPlatform.Windows).ToString();

                // Directory paths
                diagnosticInfo["WebRootPath"] = _hostingEnvironment.WebRootPath;
                diagnosticInfo["ContentRootPath"] = _hostingEnvironment.ContentRootPath;

                string certificatesDir = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates");
                string signatureDir = Path.Combine(_hostingEnvironment.WebRootPath, "images", "signature");

                diagnosticInfo["CertificatesDir"] = certificatesDir;
                diagnosticInfo["SignatureDir"] = signatureDir;

                diagnosticInfo["CertificatesDirExists"] = Directory.Exists(certificatesDir).ToString();
                diagnosticInfo["SignatureDirExists"] = Directory.Exists(signatureDir).ToString();

                // Check template files
                string templatePath = Path.Combine(certificatesDir, "template.jpg");
                string signaturePath = Path.Combine(certificatesDir, "sign.jpg");

                diagnosticInfo["TemplateFilePath"] = templatePath;
                diagnosticInfo["SignatureFilePath"] = signaturePath;

                diagnosticInfo["TemplateFileExists"] = System.IO.File.Exists(templatePath).ToString();
                diagnosticInfo["SignatureFileExists"] = System.IO.File.Exists(signaturePath).ToString();

                // Check permissions
                try
                {
                    // Try to create and delete a test file
                    var testFilePath = Path.Combine(certificatesDir, "test_permissions.txt");
                    System.IO.File.WriteAllText(testFilePath, "Test content");
                    System.IO.File.Delete(testFilePath);
                    diagnosticInfo["WritePermissions"] = "OK";
                }
                catch (Exception ex)
                {
                    diagnosticInfo["WritePermissions"] = $"Error: {ex.Message}";
                }

                // Check for iText7 assemblies
                try
                {
                    var iText7Assembly = typeof(iText.Kernel.Pdf.PdfDocument).Assembly;
                    diagnosticInfo["iText7Assembly"] = iText7Assembly.FullName;
                }
                catch
                {
                    diagnosticInfo["iText7Assembly"] = "Not found or error loading";
                }

                // Check for System.Drawing.Common if on Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        var drawingAssembly = typeof(System.Drawing.Bitmap).Assembly;
                        diagnosticInfo["SystemDrawingAssembly"] = drawingAssembly.FullName;
                    }
                    catch
                    {
                        diagnosticInfo["SystemDrawingAssembly"] = "Not found or error loading";
                    }
                }

                // List any existing certificates
                try
                {
                    var certFiles = Directory.GetFiles(certificatesDir, "*.pdf").Take(5).ToArray();
                    diagnosticInfo["ExistingCertificates"] = certFiles.Length > 0
                        ? string.Join(", ", certFiles.Select(Path.GetFileName))
                        : "None found";
                }
                catch
                {
                    diagnosticInfo["ExistingCertificates"] = "Error listing certificate files";
                }

                return View(diagnosticInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Diagnostics action");
                return Content($"Error in diagnostics: {ex.Message}");
            }
        }

        // For immediate debugging, accessible only in development
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestImageGeneration()
        {
            if (!_hostingEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                _logger.LogInformation("Testing image generation");

                // Define paths
                string certificatesDir = Path.Combine(_hostingEnvironment.WebRootPath, "images", "certificates");
                if (!Directory.Exists(certificatesDir))
                {
                    Directory.CreateDirectory(certificatesDir);
                }

                string templatePath = Path.Combine(certificatesDir, "template_test.jpg");
                string signaturePath = Path.Combine(certificatesDir, "signature_test.jpg");

                // Generate test images
                bool templateResult = await ImageUtility.CreateDefaultCertificateTemplateAsync(templatePath, _logger);
                bool signatureResult = await ImageUtility.CreateDefaultSignatureAsync(signaturePath, _logger);

                var results = new Dictionary<string, string>
                {
                    ["TemplateGeneration"] = templateResult ? "Success" : "Failed",
                    ["SignatureGeneration"] = signatureResult ? "Success" : "Failed",
                    ["TemplateExists"] = System.IO.File.Exists(templatePath).ToString(),
                    ["SignatureExists"] = System.IO.File.Exists(signaturePath).ToString()
                };

                if (System.IO.File.Exists(templatePath))
                {
                    results["TemplateFileSize"] = new FileInfo(templatePath).Length.ToString() + " bytes";
                }

                if (System.IO.File.Exists(signaturePath))
                {
                    results["SignatureFileSize"] = new FileInfo(signaturePath).Length.ToString() + " bytes";
                }

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing image generation");
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }
}
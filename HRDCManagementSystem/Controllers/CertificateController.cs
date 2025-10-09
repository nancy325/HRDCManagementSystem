using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Controllers
{
    public class CertificateController : Controller
    {
        private readonly HRDCContext _context;
        private readonly ICertificateService _certificateService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CertificateController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public CertificateController(
            HRDCContext context,
            ICertificateService certificateService,
            ICurrentUserService currentUserService,
            ILogger<CertificateController> logger,
            IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _certificateService = certificateService;
            _currentUserService = currentUserService;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

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
                            viewModel.Add(new CertificateViewModel
                            {
                                CertificateSysID = certificate.CertificateSysID,
                                TrainingRegSysID = certificate.TrainingRegSysID,
                                EmployeeName = $"{certificate.RegSys.EmployeeSys.FirstName} {certificate.RegSys.EmployeeSys.MiddleName} {certificate.RegSys.EmployeeSys.LastName}",
                                TrainingTitle = certificate.RegSys.TrainingSys.Title,
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
                .Where(r => r.TrainingSysID == trainingId && r.RecStatus == "active" && r.Confirmation)
                .ToListAsync();

            // Get certificate status for each registration
            var certificateStatusList = new List<RegistrationCertificateViewModel>();

            foreach (var reg in registrations)
            {
                // Check if certificate exists
                var certificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.TrainingRegSysID == reg.TrainingRegSysID && c.RecStatus == "active");

                certificateStatusList.Add(new RegistrationCertificateViewModel
                {
                    TrainingRegSysID = reg.TrainingRegSysID,
                    EmployeeSysID = reg.EmployeeSysID,
                    EmployeeName = $"{reg.EmployeeSys.FirstName} {reg.EmployeeSys.MiddleName} {reg.EmployeeSys.LastName}",
                    Department = reg.EmployeeSys.Department,
                    Designation = reg.EmployeeSys.Designation,
                    Email = reg.EmployeeSys.UserSys?.Email,
                    HasCertificate = certificate != null && certificate.IsGenerated == true,
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
        public async Task<IActionResult> GenerateCertificate(int trainingRegId)
        {
            try
            {
                // Get the registration with all required data
                var registration = await _context.TrainingRegistrations
                    .Include(r => r.EmployeeSys)
                    .ThenInclude(e => e.UserSys)
                    .Include(r => r.TrainingSys)
                    .FirstOrDefaultAsync(r => r.TrainingRegSysID == trainingRegId && r.RecStatus == "active");

                if (registration == null)
                {
                    TempData["ErrorMessage"] = "Registration not found.";
                    return RedirectToAction("Index", "Training");
                }

                // Check if this is a confirmed registration
                if (!registration.Confirmation)
                {
                    TempData["ErrorMessage"] = "Cannot generate certificate for unconfirmed registration.";
                    return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
                }

                // Check if certificate already exists
                var existingCertificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.TrainingRegSysID == trainingRegId && c.RecStatus == "active");

                // Generate the certificate
                string certificatePath = await _certificateService.GenerateCertificateAsync(registration);

                // Get current date
                DateOnly issueDate = DateOnly.FromDateTime(DateTime.Today);

                if (existingCertificate != null)
                {
                    // Update existing certificate
                    existingCertificate.IssueDate = issueDate;
                    existingCertificate.IsGenerated = true;
                    existingCertificate.CertificatePath = certificatePath;
                    existingCertificate.ModifiedDateTime = DateTime.Now;
                    existingCertificate.ModifiedUserId = _currentUserService.GetCurrentUserId();
                    
                    _context.Certificates.Update(existingCertificate);
                }
                else
                {
                    // Create new certificate entry
                    var certificate = new Certificate
                    {
                        TrainingRegSysID = trainingRegId,
                        IssueDate = issueDate,
                        IsGenerated = true,
                        CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                        CreatedTime = TimeOnly.FromDateTime(DateTime.Now),
                        CertificatePath = certificatePath,
                    };

                    _context.Certificates.Add(certificate);
                }

                await _context.SaveChangesAsync();

                // Send email with certificate to employee if email is available
                if (registration.EmployeeSys?.UserSys?.Email != null)
                {
                    string employeeName = $"{registration.EmployeeSys.FirstName} {registration.EmployeeSys.LastName}";
                    await _certificateService.SendCertificateEmailAsync(
                        registration.EmployeeSys.UserSys.Email,
                        employeeName,
                        registration.TrainingSys.Title,
                        certificatePath);
                }

                TempData["SuccessMessage"] = "Certificate generated successfully.";
                return RedirectToAction("TrainingCertificates", new { trainingId = registration.TrainingSysID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for registration {RegId}", trainingRegId);
                TempData["ErrorMessage"] = $"Error generating certificate: {ex.Message}";
                return RedirectToAction("Index", "Training");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
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
                    .Where(r => r.TrainingSysID == trainingId && r.RecStatus == "active" && r.Confirmation)
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
                        // Generate the certificate
                        string certificatePath = await _certificateService.GenerateCertificateAsync(registration);

                        // Get current date
                        DateOnly issueDate = DateOnly.FromDateTime(DateTime.Today);

                        // Check if certificate already exists
                        var existingCertificate = await _context.Certificates
                            .FirstOrDefaultAsync(c => c.TrainingRegSysID == registration.TrainingRegSysID && c.RecStatus == "active");

                        if (existingCertificate != null)
                        {
                            // Update existing certificate
                            existingCertificate.IssueDate = issueDate;
                            existingCertificate.IsGenerated = true;
                            existingCertificate.CertificatePath = certificatePath;
                            existingCertificate.ModifiedDateTime = DateTime.Now;
                            existingCertificate.ModifiedUserId = _currentUserService.GetCurrentUserId();
                            
                            _context.Certificates.Update(existingCertificate);
                        }
                        else
                        {
                            // Create new certificate entry
                            var certificate = new Certificate
                            {
                                TrainingRegSysID = registration.TrainingRegSysID,
                                IssueDate = issueDate,
                                IsGenerated = true,
                                CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                                CreatedTime = TimeOnly.FromDateTime(DateTime.Now),
                                CertificatePath = certificatePath
                            };

                            _context.Certificates.Add(certificate);
                        }

                        await _context.SaveChangesAsync();

                        // Send email with certificate to employee if email is available
                        if (registration.EmployeeSys?.UserSys?.Email != null)
                        {
                            string employeeName = $"{registration.EmployeeSys.FirstName} {registration.EmployeeSys.LastName}";
                            await _certificateService.SendCertificateEmailAsync(
                                registration.EmployeeSys.UserSys.Email,
                                employeeName,
                                registration.TrainingSys.Title,
                                certificatePath);
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
                TrainingTitle = c.RegSys.TrainingSys.Title,
                StartDate = c.RegSys.TrainingSys.StartDate,
                EndDate = c.RegSys.TrainingSys.EndDate,
                IssueDate = c.IssueDate ?? DateOnly.FromDateTime(c.CreateDateTime ?? DateTime.Now),
                CertificatePath = c.CertificatePath
            }).ToList();

            return View(viewModel);
        }

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
                return RedirectToAction("Index");
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
            string employeeName = certificate.RegSys.EmployeeSys?.FirstName ?? "Employee";
            string trainingTitle = certificate.RegSys.TrainingSys?.Title ?? "Training";
            string fileName = $"{employeeName}_{trainingTitle}_Certificate.pdf".Replace(" ", "_");

            // Return the file
            return PhysicalFile(filePath, "application/pdf", fileName);
        }
    }
}
using HRDCManagementSystem.Data;
using HRDCManagementSystem.Helpers;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly HRDCContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        public EmployeeController(HRDCContext context, IWebHostEnvironment env, IEmailService emailService)
        {
            _context = context;
            _env = env;
            _emailService = emailService;
        }

        // GET: Employees
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.UserSys)
                .Where(e => e.RecStatus == "active")
                .ToListAsync();

            return View("Employee", employees);
        }

        // GET: Employees/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View("EmployeeCreate", new Employee());
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Employee employee, IFormFile ProfilePhoto, string Email, string Role)
        {
            // First, check email uniqueness before processing other validations
            if (await _context.UserMasters.AnyAsync(u => u.Email == Email && u.RecStatus == "active"))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View("EmployeeCreate", employee);
            }

            // Automatically generate a secure password
            string generatedPassword = GenerateRandomPassword();

            // Create UserMaster with hashed password
            var passwordHasher = new PasswordHasher<UserMaster>();
            var user = new UserMaster
            {
                Email = Email,
                Role = Role,
                RecStatus = "active",
                CreateDateTime = DateTime.Now
            };
            user.Password = passwordHasher.HashPassword(user, generatedPassword);

            try
            {
                // Add and save UserMaster to get the auto-incremented UserSysID
                _context.UserMasters.Add(user);
                await _context.SaveChangesAsync();

                // Now assign the generated UserSysID to the employee
                employee.UserSysID = user.UserSysID;

                // Process profile photo if provided
                if (ProfilePhoto != null)
                {
                    var result = ValidateAndSavePhoto(ProfilePhoto);
                    if (!result.Success)
                    {
                        ModelState.AddModelError("ProfilePhotoPath", result.ErrorMessage);
                        return View("EmployeeCreate", employee);
                    }
                    employee.ProfilePhotoPath = result.FileName;
                }

                // Set employee fields and save
                employee.RecStatus = "active";
                employee.CreateDateTime = DateTime.Now;
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Send welcome email with credentials
                try
                {
                    string emailBody = EmailTemplates.GetWelcomeEmailTemplate(employee.FirstName, Email, generatedPassword);
                    await _emailService.SendEmailAsync(Email, "Welcome to HRDC Management System", emailBody);
                    TempData["EmailSent"] = true;
                }
                catch (Exception ex)
                {
                    TempData["EmailSent"] = false;
                    TempData["EmailError"] = ex.Message;
                }

                // Store success message and credentials for display
                TempData["Success"] = "Employee created successfully. A secure password has been automatically generated and sent to the employee.";
                TempData["NewEmployeeEmail"] = Email;
                TempData["NewEmployeePassword"] = generatedPassword;

                return Redirect("/Admin/Employees");
            }
            catch (Exception ex)
            {
                // If an error occurs while saving the employee, we should clean up the user
                if (user.UserSysID != 0)
                {
                    var createdUser = await _context.UserMasters.FindAsync(user.UserSysID);
                    if (createdUser != null)
                    {
                        _context.UserMasters.Remove(createdUser);
                        await _context.SaveChangesAsync();
                    }
                }

                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View("EmployeeCreate", employee);
            }
        }

        // GET: Employees/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View("EmployeeEdit", employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Employee employee, IFormFile ProfilePhoto)
        {
            if (id != employee.EmployeeSysID)
            {
                ModelState.AddModelError("", "Employee not found.");
                return View("EmployeeEdit", employee);
            }

            // Important: Retrieve the complete employee with UserSys relationship
            var dbEmployee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.EmployeeSysID == id);

            if (dbEmployee == null)
            {
                ModelState.AddModelError("", "Employee not found in database.");
                return View("EmployeeEdit", employee);
            }

            // Remove validation errors for these fields since we handle them specially
            ModelState.Remove("UserSys");
            ModelState.Remove("ProfilePhotoPath");

            // Clear any ProfilePhoto related validation errors to ensure it's treated as optional
            var profilePhotoKeys = ModelState.Keys.Where(k => k.Contains("ProfilePhoto")).ToList();
            foreach (var key in profilePhotoKeys)
            {
                ModelState.Remove(key);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Only update photo if a new one is uploaded
                    if (ProfilePhoto != null)
                    {
                        var result = ValidateAndSavePhoto(ProfilePhoto);
                        if (!result.Success)
                        {
                            ModelState.AddModelError("ProfilePhotoPath", result.ErrorMessage);
                            return View("EmployeeEdit", employee);
                        }
                        dbEmployee.ProfilePhotoPath = result.FileName;
                    }
                    // ProfilePhotoPath remains unchanged if no new photo

                    // Update employee fields
                    dbEmployee.FirstName = employee.FirstName;
                    dbEmployee.MiddleName = employee.MiddleName;
                    dbEmployee.LastName = employee.LastName;
                    dbEmployee.Department = employee.Department;
                    dbEmployee.Designation = employee.Designation;
                    dbEmployee.Institute = employee.Institute;
                    dbEmployee.Type = employee.Type;
                    dbEmployee.PhoneNumber = employee.PhoneNumber;
                    dbEmployee.AlternatePhone = employee.AlternatePhone;
                    dbEmployee.JoinDate = employee.JoinDate;
                    dbEmployee.LeftDate = employee.LeftDate;
                    dbEmployee.ModifiedDateTime = DateTime.Now;

                    // UserSysID remains unchanged - we don't assign employee.UserSysID to dbEmployee.UserSysID

                    _context.Update(dbEmployee);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Employee updated successfully.";
                    return Redirect("/Admin/Employees");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred while updating: {ex.Message}");
                }
            }

            // If we reach here, there was a validation error
            // Make sure to populate navigation properties for view
            return View("EmployeeEdit", employee);
        }

        // GET: Employees/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeSysID == id && e.RecStatus == "active");
            if (employee == null) return NotFound();
            return View("EmployeeDetail", employee);
        }

        // GET: Employees/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View("EmployeeDelete", employee);
        }

        // POST: Employees/Delete/5 (Soft Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Include UserSys to access the related user record
            var employee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.EmployeeSysID == id);

            if (employee != null)
            {
                // Set employee record to inactive
                employee.RecStatus = "inactive";
                employee.ModifiedDateTime = DateTime.Now;

                // Also set the related UserMaster record to inactive
                if (employee.UserSys != null)
                {
                    employee.UserSys.RecStatus = "inactive";
                    employee.UserSys.ModifiedDateTime = DateTime.Now;
                }

                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee deleted successfully.";
            }
            return Redirect("/Admin/Employees");
        }

        // POST: Employees/ResendCredentials/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCredentials(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.EmployeeSysID == id && e.RecStatus == "active");

            if (employee == null || employee.UserSys == null)
            {
                TempData["Error"] = "Employee or user not found.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Generate a new password using the same method as employee creation
            string newPassword = GenerateRandomPassword();

            // 2. Update password hash
            var passwordHasher = new PasswordHasher<UserMaster>();
            employee.UserSys.Password = passwordHasher.HashPassword(employee.UserSys, newPassword);
            await _context.SaveChangesAsync();

            // 3. Send email with new password using the password reset template
            string emailBody = EmailTemplates.GetPasswordResetEmailTemplate(employee.FirstName, employee.UserSys.Email, newPassword);
            try
            {
                await _emailService.SendEmailAsync(employee.UserSys.Email, "HRDC Management System - Password Reset", emailBody);
                TempData["Success"] = "Credentials resent successfully.";
                TempData["NewEmployeeEmail"] = employee.UserSys.Email;
                TempData["NewEmployeePassword"] = newPassword;
                TempData["EmailSent"] = true;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to resend credentials: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Employee/Profile
        [HttpGet]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Profile()
        {
            // Get the currently logged in user ID
            var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the employee based on user email
            var employee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUser && e.RecStatus == "active");

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employee/EditProfile
        [HttpGet]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> EditProfile()
        {
            // Get the currently logged in user ID
            var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the employee based on user email
            var employee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUser && e.RecStatus == "active");

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> EditProfile(Employee employee, IFormFile ProfilePhoto)
        {
            // Get the currently logged in user ID
            var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the employee based on user email
            var dbEmployee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUser && e.RecStatus == "active");

            if (dbEmployee == null)
            {
                return NotFound();
            }

            // Ensure the user is only editing their own profile
            if (dbEmployee.EmployeeSysID != employee.EmployeeSysID)
            {
                TempData["Error"] = "You can only edit your own profile.";
                return View(employee);
            }

            // Remove validation errors for fields that employees cannot edit and optional fields
            ModelState.Remove("UserSys");
            ModelState.Remove("ProfilePhotoPath");
            ModelState.Remove("Department");
            ModelState.Remove("Designation");
            ModelState.Remove("Institute");
            ModelState.Remove("Type");
            ModelState.Remove("JoinDate");
            ModelState.Remove("LeftDate");
            ModelState.Remove("UserSysID");

            // Clear any ProfilePhoto related validation errors to ensure it's treated as optional
            var profilePhotoKeys = ModelState.Keys.Where(k => k.Contains("ProfilePhoto")).ToList();
            foreach (var key in profilePhotoKeys)
            {
                ModelState.Remove(key);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Only update photo if a new one is uploaded
                    if (ProfilePhoto != null)
                    {
                        var result = ValidateAndSavePhoto(ProfilePhoto);
                        if (!result.Success)
                        {
                            ModelState.AddModelError("ProfilePhotoPath", result.ErrorMessage);
                            return View(employee);
                        }
                        dbEmployee.ProfilePhotoPath = result.FileName;
                    }

                    // Update editable fields - limited for employee self-update
                    dbEmployee.FirstName = employee.FirstName;
                    dbEmployee.MiddleName = employee.MiddleName;
                    dbEmployee.LastName = employee.LastName;
                    dbEmployee.PhoneNumber = employee.PhoneNumber;
                    dbEmployee.AlternatePhone = employee.AlternatePhone;
                    dbEmployee.ModifiedDateTime = DateTime.Now;

                    _context.Update(dbEmployee);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Your profile has been updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred while updating: {ex.Message}");
                }
            }

            return View(employee);
        }

        // GET: Employee/Dashboard
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get the currently logged in user email
                var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Debug: Log all claims for troubleshooting
                var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();

                if (string.IsNullOrEmpty(currentUser))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Find the employee based on user email
                var employee = await _context.Employees
                    .Include(e => e.UserSys)
                    .FirstOrDefaultAsync(e => e.UserSys.Email == currentUser && e.RecStatus == "active");

                if (employee == null)
                {
                    // Try to get user by UserSysID as fallback
                    if (int.TryParse(currentUser, out int userSysId))
                    {
                        employee = await _context.Employees
                            .Include(e => e.UserSys)
                            .FirstOrDefaultAsync(e => e.UserSysID == userSysId && e.RecStatus == "active");
                    }

                    if (employee == null)
                    {
                        return NotFound("Employee record not found. Please contact administrator.");
                    }
                }

                // Current date for comparisons
                var currentDate = DateOnly.FromDateTime(DateTime.Now);

                // Get upcoming trainings for this employee - Fix: Split query into two parts
                var upcomingTrainingData = await _context.TrainingRegistrations
                    .Include(tr => tr.TrainingSys)
                    .Where(tr => tr.EmployeeSysID == employee.EmployeeSysID &&
                                tr.TrainingSys.StartDate > currentDate &&
                                tr.RecStatus == "active" &&
                                tr.TrainingSys.RecStatus == "active")
                    .OrderBy(tr => tr.TrainingSys.StartDate)
                    .ToListAsync();

                var upcomingTrainings = upcomingTrainingData
                    .Select(tr => new TrainingViewModel
                    {
                        TrainingSysID = tr.TrainingSys.TrainingSysID,
                        Title = tr.TrainingSys.Title,
                        StartDate = tr.TrainingSys.StartDate,
                        EndDate = tr.TrainingSys.EndDate,
                        FromTime = tr.TrainingSys.fromTime,
                        ToTime = tr.TrainingSys.toTime,
                        Venue = tr.TrainingSys.Venue,
                        TrainerName = tr.TrainingSys.TrainerName,
                        Mode = tr.TrainingSys.Mode,
                        Status = tr.TrainingSys.Status,
                        Capacity = tr.TrainingSys.Capacity,
                        EligibilityType = tr.TrainingSys.EligibilityType,
                        ValidTill = tr.TrainingSys.Validtill ?? tr.TrainingSys.EndDate,
                        MarksOutOf = tr.TrainingSys.MarksOutOf,
                        IsMarksEntry = tr.TrainingSys.IsMarksEntry,
                        ExistingPath = tr.TrainingSys.FilePath
                    })
                    .ToList();

                // Get in-progress trainings (started but not ended yet) - Fix: Split query into two parts
                var inProgressTrainingData = await _context.TrainingRegistrations
                    .Include(tr => tr.TrainingSys)
                    .Where(tr => tr.EmployeeSysID == employee.EmployeeSysID &&
                                tr.TrainingSys.StartDate <= currentDate &&
                                tr.TrainingSys.EndDate >= currentDate &&
                                tr.RecStatus == "active" &&
                                tr.TrainingSys.RecStatus == "active")
                    .OrderBy(tr => tr.TrainingSys.StartDate)
                    .ToListAsync();

                var inProgressTrainings = inProgressTrainingData
                    .Select(tr => new TrainingViewModel
                    {
                        TrainingSysID = tr.TrainingSys.TrainingSysID,
                        Title = tr.TrainingSys.Title,
                        StartDate = tr.TrainingSys.StartDate,
                        EndDate = tr.TrainingSys.EndDate,
                        FromTime = tr.TrainingSys.fromTime,
                        ToTime = tr.TrainingSys.toTime,
                        Venue = tr.TrainingSys.Venue,
                        TrainerName = tr.TrainingSys.TrainerName,
                        Mode = tr.TrainingSys.Mode,
                        Status = tr.TrainingSys.Status,
                        Capacity = tr.TrainingSys.Capacity,
                        EligibilityType = tr.TrainingSys.EligibilityType,
                        ValidTill = tr.TrainingSys.Validtill ?? tr.TrainingSys.EndDate,
                        MarksOutOf = tr.TrainingSys.MarksOutOf,
                        IsMarksEntry = tr.TrainingSys.IsMarksEntry,
                        ExistingPath = tr.TrainingSys.FilePath
                    })
                    .ToList();

                // Get completed trainings - Fix: Split query into two parts
                var completedTrainingData = await _context.TrainingRegistrations
                    .Include(tr => tr.TrainingSys)
                    .Where(tr => tr.EmployeeSysID == employee.EmployeeSysID &&
                                tr.TrainingSys.EndDate < currentDate &&
                                tr.RecStatus == "active" &&
                                tr.TrainingSys.RecStatus == "active")
                    .OrderByDescending(tr => tr.TrainingSys.StartDate)
                    .ToListAsync();

                var completedTrainings = completedTrainingData
                    .Select(tr => new TrainingViewModel
                    {
                        TrainingSysID = tr.TrainingSys.TrainingSysID,
                        Title = tr.TrainingSys.Title,
                        StartDate = tr.TrainingSys.StartDate,
                        EndDate = tr.TrainingSys.EndDate,
                        FromTime = tr.TrainingSys.fromTime,
                        ToTime = tr.TrainingSys.toTime,
                        Venue = tr.TrainingSys.Venue,
                        TrainerName = tr.TrainingSys.TrainerName,
                        Mode = tr.TrainingSys.Mode,
                        Status = tr.TrainingSys.Status,
                        Capacity = tr.TrainingSys.Capacity,
                        EligibilityType = tr.TrainingSys.EligibilityType,
                        ValidTill = tr.TrainingSys.Validtill ?? tr.TrainingSys.EndDate,
                        MarksOutOf = tr.TrainingSys.MarksOutOf,
                        IsMarksEntry = tr.TrainingSys.IsMarksEntry,
                        ExistingPath = tr.TrainingSys.FilePath
                    })
                    .ToList();

                // Get certificates count
                var certificatesCount = await _context.Certificates
                    .CountAsync(c => c.RegSys.EmployeeSysID == employee.EmployeeSysID &&
                                   c.IsGenerated == true &&
                                   c.RecStatus == "active");

                // Create the dashboard view model
                var viewModel = new EmployeeDashboardViewModel
                {
                    Employee = employee,
                    UpcomingTrainings = upcomingTrainings,
                    InProgressTrainings = inProgressTrainings,
                    CompletedTrainings = completedTrainings,
                    CertificatesCount = certificatesCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: Employee/Certificates
        [HttpGet]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Certificates()
        {
            // Get the currently logged in user email
            var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the employee based on user email
            var employee = await _context.Employees
                .Include(e => e.UserSys)
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUser && e.RecStatus == "active");

            if (employee == null)
            {
                return NotFound();
            }

            // Get all certificates for this employee
            var certificates = await _context.Certificates
                .Include(c => c.RegSys)
                .ThenInclude(r => r.TrainingSys)
                .Where(c => c.RegSys.EmployeeSysID == employee.EmployeeSysID &&
                            c.IsGenerated == true &&
                            c.RecStatus == "active")
                .Select(c => new CertificateViewModel
                {
                    CertificateSysID = c.CertificateSysID,
                    TrainingTitle = c.RegSys.TrainingSys.Title,
                    IssueDate = c.IssueDate,
                    CertificatePath = c.CertificatePath
                })
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();

            return View(certificates);
        }

        // Helper for photo validation and saving
        private (bool Success, string FileName, string ErrorMessage) ValidateAndSavePhoto(IFormFile file)
        {
            // Your existing implementation
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return (false, null, "Only .jpg, .jpeg, .png files are allowed.");

            if (file.Length > 2 * 1024 * 1024)
                return (false, null, "File size must be less than 2 MB.");

            var fileName = Guid.NewGuid() + ext;
            var savePath = Path.Combine(_env.WebRootPath, "images/profilephoto", fileName);

            // Make sure directory exists
            var directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            return (true, fileName, null);
        }

        // Helper method to generate a random password
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789"; // Excludes confusing characters like 0, O, I, l, 1
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpGet("/Admin/Employees")]
        public async Task<IActionResult> Employees()
        {
            return await Index();
        }
    }
}

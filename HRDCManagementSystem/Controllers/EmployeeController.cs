using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using HRDCManagementSystem.Services;
using HRDCManagementSystem.Helpers;
using Microsoft.AspNetCore.Identity;

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

            return View("~/Views/Admin/Employees.cshtml", employees);
        }

        // GET: Employees/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/EmployeeCreate.cshtml", new Employee());
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Employee employee, IFormFile ProfilePhoto, string Email, string Password, string Role)
        {
            // First, check email uniqueness before processing other validations
            if (await _context.UserMasters.AnyAsync(u => u.Email == Email && u.RecStatus == "active"))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View("~/Views/Admin/EmployeeCreate.cshtml", employee);
            }

            // Create UserMaster with hashed password
            var passwordHasher = new PasswordHasher<UserMaster>();
            var user = new UserMaster
            {
                Email = Email,
                Role = Role,
                RecStatus = "active",
                CreateDateTime = DateTime.Now
            };
            user.Password = passwordHasher.HashPassword(user, Password);

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
                        return View("~/Views/Admin/EmployeeCreate.cshtml", employee);
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
                    string emailBody = EmailTemplates.GetWelcomeEmailTemplate(employee.FirstName, Email, Password);
                    await _emailService.SendEmailAsync(Email, "Welcome to HRDC Management System", emailBody);
                    TempData["EmailSent"] = true;
                }
                catch (Exception ex)
                {
                    TempData["EmailSent"] = false;
                    TempData["EmailError"] = ex.Message;
                }

                // Store success message and credentials for display
                TempData["Success"] = "Employee created successfully.";
                TempData["NewEmployeeEmail"] = Email;
                TempData["NewEmployeePassword"] = Password;

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
                return View("~/Views/Admin/EmployeeCreate.cshtml", employee);
            }
        }

        // GET: Employees/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View("~/Views/Admin/EmployeeEdit.cshtml", employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Employee employee, IFormFile ProfilePhoto)
        {
            if (id != employee.EmployeeSysID) return NotFound();

            if (ModelState.IsValid)
            {
                var dbEmployee = await _context.Employees.FindAsync(id);
                if (dbEmployee == null) return NotFound();

                if (ProfilePhoto != null)
                {
                    var result = ValidateAndSavePhoto(ProfilePhoto);
                    if (!result.Success)
                    {
                        ModelState.AddModelError("ProfilePhotoPath", result.ErrorMessage);
                        return View("~/Views/Admin/EmployeeEdit.cshtml", employee);
                    }
                    dbEmployee.ProfilePhotoPath = result.FileName;
                }

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
                dbEmployee.ModifiedDateTime = DateTime.Now;

                _context.Update(dbEmployee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/EmployeeEdit.cshtml", employee);
        }

        // GET: Employees/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeSysID == id && e.RecStatus == "active");
            if (employee == null) return NotFound();
            return View("~/Views/Admin/EmployeeDetails.cshtml", employee);
        }

        // GET: Employees/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View("~/Views/Admin/EmployeeDelete.cshtml", employee);
        }

        // POST: Employees/Delete/5 (Soft Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.RecStatus = "inactive";
                employee.ModifiedDateTime = DateTime.Now;
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
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

            // 1. Generate a new password
            string newPassword = Guid.NewGuid().ToString("N").Substring(0, 8); // 8-char random password

            // 2. Update password hash
            var passwordHasher = new PasswordHasher<UserMaster>();
            employee.UserSys.Password = passwordHasher.HashPassword(employee.UserSys, newPassword);
            await _context.SaveChangesAsync();

            // 3. Send email with new password
            string emailBody = EmailTemplates.GetWelcomeEmailTemplate(employee.FirstName, employee.UserSys.Email, newPassword);
            try
            {
                await _emailService.SendEmailAsync(employee.UserSys.Email, "Your HRDC Credentials", emailBody);
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

        [HttpGet("/Admin/Employees")]
        public async Task<IActionResult> Employees()
        {
            return await Index();
        }
    }
}

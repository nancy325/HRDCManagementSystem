using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    public class TrainingRegistrationController : Controller
    {
        private readonly HRDCContext _context;
        private readonly ICurrentUserService _currentUserService;

        public TrainingRegistrationController(HRDCContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Admin")]
        [ActionName("Register")]
        public async Task<IActionResult> RegisterForTraining(int trainingId)
        {
            try
            {
                // Get current user ID
                var currentUserId = _currentUserService.GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Get current user email for employee lookup
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    TempData["ErrorMessage"] = "User email not found. Please log in again.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Find the employee record
                var employee = await _context.Employees
                    .Include(e => e.UserSys)
                    .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee record not found. Please contact administrator.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Check if training exists and is active
                var training = await _context.TrainingPrograms
                    .FirstOrDefaultAsync(t => t.TrainingSysID == trainingId && t.RecStatus == "active");

                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training not found or no longer available.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Check if employee is already registered for this training
                var existingRegistration = await _context.TrainingRegistrations
                    .FirstOrDefaultAsync(tr => tr.EmployeeSysID == employee.EmployeeSysID && 
                                             tr.TrainingSysID == trainingId && 
                                             tr.RecStatus == "active");

                if (existingRegistration != null)
                {
                    TempData["ErrorMessage"] = "You are already registered for this training.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Check if registration is still valid (before ValidTill date)
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                if (training.Validtill.HasValue && training.Validtill.Value < currentDate)
                {
                    TempData["ErrorMessage"] = "Registration period has expired for this training.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Check if training has capacity
                var currentRegistrations = await _context.TrainingRegistrations
                    .CountAsync(tr => tr.TrainingSysID == trainingId && tr.RecStatus == "active");

                if (currentRegistrations >= training.Capacity)
                {
                    TempData["ErrorMessage"] = "This training is full. No more registrations are accepted.";
                    return RedirectToAction("Details", "Training", new { id = trainingId });
                }

                // Create new registration with the specified default values
                var registration = new TrainingRegistration
                {
                    EmployeeSysID = employee.EmployeeSysID,
                    TrainingSysID = trainingId,
                    Registration = true,
                    // Pending by default => admin needs to approve/reject
                    Confirmation = null,
                    RecStatus = "active",
                    CreateUserId = employee.EmployeeSysID,
                    CreateDateTime = DateTime.Now
                };

                _context.TrainingRegistrations.Add(registration);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Successfully registered for the training!";
                return RedirectToAction("Details", "Training", new { id = trainingId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while registering: {ex.Message}";
                return RedirectToAction("Details", "Training", new { id = trainingId });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> MyRegistrations()
        {
            try
            {
                // Get current user email for employee lookup
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    TempData["ErrorMessage"] = "User email not found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Find the employee record
                var employee = await _context.Employees
                    .Include(e => e.UserSys)
                    .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee record not found. Please contact administrator.";
                    return RedirectToAction("Login", "Account");
                }

                // Get all registrations for this employee
                var registrations = await _context.TrainingRegistrations
                    .Include(tr => tr.TrainingSys)
                    .Where(tr => tr.EmployeeSysID == employee.EmployeeSysID && tr.RecStatus == "active")
                    .OrderByDescending(tr => tr.CreateDateTime)
                    .ToListAsync();

                return View(registrations);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving registrations: {ex.Message}";
                return RedirectToAction("Dashboard", "Employee");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Admin")]
        [ActionName("Cancel")]
        public async Task<IActionResult> CancelRegistration(int registrationId)
        {
            try
            {
                // Get current user email for employee lookup
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    TempData["ErrorMessage"] = "User email not found. Please log in again.";
                    return RedirectToAction("MyRegistrations");
                }

                // Find the employee record
                var employee = await _context.Employees
                    .Include(e => e.UserSys)
                    .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee record not found. Please contact administrator.";
                    return RedirectToAction("MyRegistrations");
                }

                // Find the registration
                var registration = await _context.TrainingRegistrations
                    .FirstOrDefaultAsync(tr => tr.TrainingRegSysID == registrationId && 
                                             tr.EmployeeSysID == employee.EmployeeSysID && 
                                             tr.RecStatus == "active");

                if (registration == null)
                {
                    TempData["ErrorMessage"] = "Registration not found or you don't have permission to cancel it.";
                    return RedirectToAction("MyRegistrations");
                }

                // Check if training has already started
                var training = await _context.TrainingPrograms
                    .FirstOrDefaultAsync(t => t.TrainingSysID == registration.TrainingSysID);

                if (training != null)
                {
                    var currentDate = DateOnly.FromDateTime(DateTime.Now);
                    if (training.StartDate <= currentDate)
                    {
                        TempData["ErrorMessage"] = "Cannot cancel registration as the training has already started.";
                        return RedirectToAction("MyRegistrations");
                    }
                }

                // Soft delete the registration
                registration.RecStatus = "inactive";
                registration.ModifiedDateTime = DateTime.Now;
                registration.ModifiedUserId = employee.EmployeeSysID;

                _context.Update(registration);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration cancelled successfully.";
                return RedirectToAction("MyRegistrations");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while cancelling registration: {ex.Message}";
                return RedirectToAction("MyRegistrations");
            }
        }
    }
}

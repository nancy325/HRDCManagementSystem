using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Services;
using HRDCManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    [Authorize]
    public class TrainingRegistrationController : Controller
    {
        private readonly HRDCContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<TrainingRegistrationController> _logger;

        public TrainingRegistrationController(
            HRDCContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<TrainingRegistrationController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Register(int trainingId)
        {
            // Get current user
            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                TempData["ErrorMessage"] = "You must be logged in to register for trainings.";
                return RedirectToAction("Login", "Account");
            }

            // Get the employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Your employee record could not be found.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }

            // Get the training
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == trainingId && t.RecStatus == "active");

            if (training == null)
            {
                TempData["ErrorMessage"] = "The requested training could not be found.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }

            // Check if training is past its registration date
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            if (training.Validtill < currentDate)
            {
                TempData["ErrorMessage"] = "Registration for this training has closed.";
                return RedirectToAction("Details", "Training", new { id = trainingId });
            }

            // Check if employee is already registered
            var existingRegistration = await _context.TrainingRegistrations
                .AnyAsync(tr => tr.EmployeeSysID == employee.EmployeeSysID &&
                              tr.TrainingSysID == trainingId &&
                              tr.RecStatus == "active");

            if (existingRegistration)
            {
                TempData["ErrorMessage"] = "You are already registered for this training.";
                return RedirectToAction("Details", "Training", new { id = trainingId });
            }

            // Check if training capacity is reached
            var currentRegistrations = await _context.TrainingRegistrations
                .CountAsync(tr => tr.TrainingSysID == trainingId && tr.RecStatus == "active");

            if (currentRegistrations >= training.Capacity)
            {
                TempData["ErrorMessage"] = "This training has reached its maximum capacity.";
                return RedirectToAction("Details", "Training", new { id = trainingId });
            }

            // Create the registration
            var registration = new TrainingRegistration
            {
                EmployeeSysID = employee.EmployeeSysID,
                TrainingSysID = trainingId,
                Registration = true,
                // Set to null initially (pending), will be approved or rejected by admin
                Confirmation = null
            };

            _context.TrainingRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Send notification to admin
            try
            {
                await _notificationService.CreateNotificationAsync(
                    null,
                    "Admin",
                    "New Training Registration",
                    $"Employee {employee.FirstName} {employee.LastName} has registered for training '{training.Title}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admin notification for registration from employee {EmployeeId} for training {TrainingId}",
                    employee.EmployeeSysID, trainingId);
                // Continue anyway, this shouldn't block the registration
            }

            TempData["SuccessMessage"] = "You have successfully registered for this training. Your registration is pending approval.";
            return RedirectToAction("MyRegistrations");
        }

        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyRegistrations()
        {
            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Your employee record could not be found.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }

            var registrations = await _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Where(tr => tr.EmployeeSysID == employee.EmployeeSysID && tr.RecStatus == "active")
                .ToListAsync();

            return View("MyRegistrations", registrations);
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Cancel(int registrationId)
        {
            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Your employee record could not be found.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }

            var registration = await _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .FirstOrDefaultAsync(tr => tr.TrainingRegSysID == registrationId && tr.EmployeeSysID == employee.EmployeeSysID && tr.RecStatus == "active");

            if (registration == null)
            {
                TempData["ErrorMessage"] = "Registration not found.";
                return RedirectToAction("MyRegistrations");
            }

            if (registration.TrainingSys.StartDate <= DateOnly.FromDateTime(DateTime.Now))
            {
                TempData["ErrorMessage"] = "Cannot cancel registration for a training that has already started.";
                return RedirectToAction("MyRegistrations");
            }

            registration.RecStatus = "inactive";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration cancelled successfully.";
            return RedirectToAction("MyRegistrations");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HandleApproval([FromBody] ApprovalRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request format." });
            }

            try
            {
                var registration = await _context.TrainingRegistrations
                    .Include(tr => tr.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(tr => tr.TrainingSys)
                    .FirstOrDefaultAsync(tr => tr.TrainingRegSysID == request.RegistrationId);

                if (registration == null)
                {
                    return Json(new { success = false, message = "Registration not found." });
                }

                // Check if training is completed
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var isTrainingCompleted = registration.TrainingSys.EndDate < currentDate;

                if (isTrainingCompleted)
                {
                    return Json(new { success = false, message = "Cannot approve or reject registration for a completed training." });
                }

                // Check if status is already set
                if (registration.Confirmation != null)
                {
                    return Json(new { success = false, message = "Registration status has already been set. Cannot change it." });
                }

                string status;
                if (request.Action == "approve")
                {
                    registration.Confirmation = true;
                    status = "Approved";
                }
                else if (request.Action == "reject")
                {
                    registration.Confirmation = false;
                    status = "Rejected";
                }
                else
                {
                    return Json(new { success = false, message = "Invalid action. Use 'approve' or 'reject'." });
                }

                await _context.SaveChangesAsync();

                // Send notification and email to employee
                try
                {
                    await NotificationUtility.NotifyRegistrationStatusChange(
                        _notificationService,
                        registration,
                        status,
                        _emailService,
                        _logger);

                    _logger.LogInformation("Registration {Status} notification sent to employee {EmployeeId} for training '{TrainingTitle}'",
                        status, registration.EmployeeSysID, registration.TrainingSys.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send notification for registration status change. Registration ID: {RegistrationId}, Status: {Status}",
                        registration.TrainingRegSysID, status);
                    // Continue anyway, this shouldn't block the approval process
                }

                return Json(new { success = true, message = $"Registration has been {status.ToLower()} and notification sent to the employee." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling registration approval for request: {RequestId}", request.RegistrationId);
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkApprove([FromBody] BulkApprovalRequest request)
        {
            if (!ModelState.IsValid || request.RegistrationIds == null || !request.RegistrationIds.Any())
            {
                return Json(new { success = false, message = "Invalid request format or no registrations selected." });
            }

            try
            {
                var registrations = await _context.TrainingRegistrations
                    .Include(tr => tr.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(tr => tr.TrainingSys)
                    .Where(tr => request.RegistrationIds.Contains(tr.TrainingRegSysID))
                    .ToListAsync();

                if (!registrations.Any())
                {
                    return Json(new { success = false, message = "No registrations found." });
                }

                string status = request.Action == "approve" ? "Approved" : "Rejected";
                bool confirmationValue = request.Action == "approve";

                foreach (var registration in registrations)
                {
                    registration.Confirmation = confirmationValue;
                }

                await _context.SaveChangesAsync();

                // Send notifications and emails
                try
                {
                    var notificationTasks = registrations.Select(async registration =>
                    {
                        try
                        {
                            await NotificationUtility.NotifyRegistrationStatusChange(
                                _notificationService,
                                registration,
                                status,
                                _emailService,
                                _logger);

                            _logger.LogDebug("Bulk {Status} notification sent to employee {EmployeeId} for training '{TrainingTitle}'",
                                status, registration.EmployeeSysID, registration.TrainingSys.Title);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send notification for bulk registration status change. Registration ID: {RegistrationId}",
                                registration.TrainingRegSysID);
                        }
                    });

                    await Task.WhenAll(notificationTasks);

                    _logger.LogInformation("Completed sending bulk {Status} notifications for {Count} registrations",
                        status, registrations.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send some notifications for bulk registration status change");
                    // Continue anyway
                }

                return Json(new
                {
                    success = true,
                    message = $"{registrations.Count} registrations have been {request.Action}d.",
                    count = registrations.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk approval: {Message}", ex.Message);
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, int? trainingId, string startDate, string endDate, string status)
        {
            try
            {
                var registration = await _context.TrainingRegistrations
                    .Include(tr => tr.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(tr => tr.TrainingSys)
                    .FirstOrDefaultAsync(tr => tr.TrainingRegSysID == id);

                if (registration == null)
                {
                    TempData["ErrorMessage"] = "Registration not found.";
                    return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
                }

                // Check if training is completed
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var isTrainingCompleted = registration.TrainingSys.EndDate < currentDate;

                if (isTrainingCompleted)
                {
                    TempData["ErrorMessage"] = "Cannot approve registration for a completed training.";
                    return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
                }

                // Check if status is already set
                if (registration.Confirmation != null)
                {
                    TempData["ErrorMessage"] = "Registration status has already been set.";
                    return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
                }

                registration.Confirmation = true;
                await _context.SaveChangesAsync();

                // Send notification and email to employee
                try
                {
                    await NotificationUtility.NotifyRegistrationStatusChange(
                        _notificationService,
                        registration,
                        "Approved",
                        _emailService,
                        _logger);

                    _logger.LogInformation("Registration approved and notification sent to employee {EmployeeId} for training '{TrainingTitle}'",
                        registration.EmployeeSysID, registration.TrainingSys.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send notification for registration approval. Registration ID: {RegistrationId}",
                        registration.TrainingRegSysID);
                    // Continue anyway, this shouldn't block the approval process
                }

                TempData["SuccessMessage"] = "Registration approved successfully and notification sent to the employee.";
                return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving registration {RegistrationId}", id);
                TempData["ErrorMessage"] = $"An error occurred while approving the registration: {ex.Message}";
                return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, int? trainingId, string startDate, string endDate, string status)
        {
            try
            {
                var registration = await _context.TrainingRegistrations
                    .Include(tr => tr.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(tr => tr.TrainingSys)
                    .FirstOrDefaultAsync(tr => tr.TrainingRegSysID == id);

                if (registration == null)
                {
                    TempData["ErrorMessage"] = "Registration not found.";
                    return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
                }

                // Check if training is completed
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var isTrainingCompleted = registration.TrainingSys.EndDate < currentDate;

                if (isTrainingCompleted)
                {
                    TempData["ErrorMessage"] = "Cannot reject registration for a completed training.";
                    return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
                }

                // Check if status is already set
                if (registration.Confirmation != null)
                {
                    TempData["ErrorMessage"] = "Registration status has already been set.";
                    return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
                }

                registration.Confirmation = false;
                await _context.SaveChangesAsync();

                // Send notification and email to employee
                try
                {
                    await NotificationUtility.NotifyRegistrationStatusChange(
                        _notificationService,
                        registration,
                        "Rejected",
                        _emailService,
                        _logger);

                    _logger.LogInformation("Registration rejected and notification sent to employee {EmployeeId} for training '{TrainingTitle}'",
                        registration.EmployeeSysID, registration.TrainingSys.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send notification for registration rejection. Registration ID: {RegistrationId}",
                        registration.TrainingRegSysID);
                    // Continue anyway, this shouldn't block the rejection process
                }

                TempData["SuccessMessage"] = "Registration rejected successfully and notification sent to the employee.";
                return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting registration {RegistrationId}", id);
                TempData["ErrorMessage"] = $"An error occurred while rejecting the registration: {ex.Message}";
                return RedirectToAction("Registrations", "Admin", new { trainingId, startDate, endDate, status });
            }
        }

    }

    public class ApprovalRequest
    {
        public int RegistrationId { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class BulkApprovalRequest
    {
        public List<int> RegistrationIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }
}
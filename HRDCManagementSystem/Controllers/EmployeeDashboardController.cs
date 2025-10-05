using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeDashboardController : Controller
    {
        private readonly HRDCContext _context;
        private readonly IEmailService _emailService;

        public EmployeeDashboardController(HRDCContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Displays the employee dashboard with training information and statistics
        /// </summary>
        /// <returns>Employee dashboard view with EmployeeDashboardViewModel</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
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

                // Get current date for comparisons
                var currentDate = DateOnly.FromDateTime(DateTime.Now);

                // Get training data for the employee
                var upcomingTrainings = await GetUpcomingTrainings(currentDate);
                var inProgressTrainings = await GetInProgressTrainings(employee.EmployeeSysID, currentDate);
                var completedTrainings = await GetCompletedTrainings(employee.EmployeeSysID, currentDate);
                var certificatesCount = await GetCertificatesCount(employee.EmployeeSysID);

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

        private async Task<List<TrainingViewModel>> GetUpcomingTrainings(DateOnly currentDate)
        {
            var trainingData = await _context.TrainingPrograms
                .Where(tp => tp.StartDate > currentDate && tp.RecStatus == "active")
                .OrderBy(tp => tp.StartDate)
                .Take(5)
                .ToListAsync();

            return trainingData.Select(tp => new TrainingViewModel
            {
                TrainingSysID = tp.TrainingSysID,
                Title = tp.Title,
                TrainerName = tp.TrainerName,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                FromTime = tp.fromTime,
                ToTime = tp.toTime,
                Venue = tp.Venue,
                Capacity = tp.Capacity,
                Status = tp.Status,
                Mode = tp.Mode,
                EligibilityType = tp.EligibilityType,
                ValidTill = tp.Validtill ?? tp.EndDate,
                MarksOutOf = tp.MarksOutOf,
                IsMarksEntry = tp.IsMarksEntry,
                ExistingPath = tp.FilePath
            }).ToList();
        }


        private async Task<List<TrainingViewModel>> GetInProgressTrainings(int employeeId, DateOnly currentDate)
        {
            var trainingData = await _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Where(tr => tr.EmployeeSysID == employeeId &&
                            tr.TrainingSys.StartDate <= currentDate &&
                            tr.TrainingSys.EndDate >= currentDate &&
                            tr.RecStatus == "active" &&
                            tr.TrainingSys.RecStatus == "active")
                .OrderBy(tr => tr.TrainingSys.StartDate)
                .Take(5)
                .ToListAsync();

            return trainingData.Select(tr => new TrainingViewModel
            {
                TrainingSysID = tr.TrainingSys.TrainingSysID,
                Title = tr.TrainingSys.Title,
                TrainerName = tr.TrainingSys.TrainerName,
                StartDate = tr.TrainingSys.StartDate,
                EndDate = tr.TrainingSys.EndDate,
                FromTime = tr.TrainingSys.fromTime,
                ToTime = tr.TrainingSys.toTime,
                Venue = tr.TrainingSys.Venue,
                Capacity = tr.TrainingSys.Capacity,
                Status = tr.TrainingSys.Status,
                Mode = tr.TrainingSys.Mode,
                EligibilityType = tr.TrainingSys.EligibilityType,
                ValidTill = tr.TrainingSys.Validtill ?? tr.TrainingSys.EndDate,
                MarksOutOf = tr.TrainingSys.MarksOutOf,
                IsMarksEntry = tr.TrainingSys.IsMarksEntry,
                ExistingPath = tr.TrainingSys.FilePath
            }).ToList();
        }


        private async Task<List<TrainingViewModel>> GetCompletedTrainings(int employeeId, DateOnly currentDate)
        {
            var trainingData = await _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Where(tr => tr.EmployeeSysID == employeeId &&
                            tr.TrainingSys.EndDate < currentDate &&
                            tr.RecStatus == "active" &&
                            tr.TrainingSys.RecStatus == "active")
                .OrderByDescending(tr => tr.TrainingSys.StartDate)
                .Take(5)
                .ToListAsync();

            return trainingData.Select(tr => new TrainingViewModel
            {
                TrainingSysID = tr.TrainingSys.TrainingSysID,
                Title = tr.TrainingSys.Title,
                TrainerName = tr.TrainingSys.TrainerName,
                StartDate = tr.TrainingSys.StartDate,
                EndDate = tr.TrainingSys.EndDate,
                FromTime = tr.TrainingSys.fromTime,
                ToTime = tr.TrainingSys.toTime,
                Venue = tr.TrainingSys.Venue,
                Capacity = tr.TrainingSys.Capacity,
                Status = tr.TrainingSys.Status,
                Mode = tr.TrainingSys.Mode,
                EligibilityType = tr.TrainingSys.EligibilityType,
                ValidTill = tr.TrainingSys.Validtill ?? tr.TrainingSys.EndDate,
                MarksOutOf = tr.TrainingSys.MarksOutOf,
                IsMarksEntry = tr.TrainingSys.IsMarksEntry,
                ExistingPath = tr.TrainingSys.FilePath
            }).ToList();
        }

        /// <summary>
        /// Gets the count of certificates for the specific employee
        /// </summary>
        private async Task<int> GetCertificatesCount(int employeeId)
        {
            return await _context.Certificates
                .CountAsync(c => c.RegSys.EmployeeSysID == employeeId &&
                               c.IsGenerated == true &&
                               c.RecStatus == "active");
        }

        /// <summary>
        /// Displays the help page with FAQs and query form
        /// </summary>
        /// <returns>Help view with FAQs and query form</returns>
        [HttpGet]
        public IActionResult Help()
        {
            try
            {
                var viewModel = new HelpViewModel();
                return View("~/Views/Employee/Help.cshtml",viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                return StatusCode(500, $"Error loading Help page: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the submission of help query form
        /// </summary>
        /// <returns>Redirect to help page with success message</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Help(HelpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Employee/Help.cshtml",model);
            }

            try
            {
                // Get the currently logged in user email
                var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUser))
                {
                    ModelState.AddModelError("", "User not authenticated.");
                    return View("~/Views/Employee/Help.cshtml", model);
                }

                // Find the employee based on user email
                var employee = await _context.Employees
                    .Include(e => e.UserSys)
                    .FirstOrDefaultAsync(e => e.UserSys.Email == currentUser && e.RecStatus == "active");

                if (employee == null)
                {
                    ModelState.AddModelError("", "Employee record not found.");
                    return View("~/Views/Employee/Help.cshtml", model);
                }

                // Map ViewModel to Entity
                var helpQuery = new HelpQuery
                {
                    EmployeeSysID = employee.EmployeeSysID,
                    Name = model.Name,
                    Email = model.Email,
                    QueryType = model.QueryType,
                    Subject = model.Subject,
                    Message = model.Message,
                    Status = "Open",
                    ViewedByAdmin = false,
                    ResolvedDate = null
                };

                try
                {
                    _context.HelpQueries.Add(helpQuery);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Your query has been submitted successfully. We'll get back to you soon!";
                    return RedirectToAction(nameof(Help));
                }
                catch (Exception ex)
                {
                    // Provide more specific database error information
                    ModelState.AddModelError("", $"Database error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                    return View("~/Views/Employee/Help.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while submitting your query: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                }
                return View("~/Views/Employee/Help.cshtml", model);
            }
        }
    }
}

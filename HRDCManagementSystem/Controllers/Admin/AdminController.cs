using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Admin;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly HRDCContext _context;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
        
        public AdminController(HRDCContext context, INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _context = context;
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Get current admin user ID and role for notifications
            var currentUserId = _currentUserService.GetCurrentUserId() ?? 0;
            var userRole = "Admin";
            
            var dashboard = new AdminDashboardViewModel
            {
                TotalEmployees = await _context.Employees.CountAsync(e => e.RecStatus == "active"),
                // Ongoing trainings determined by date and time
                ActiveTrainings = await GetOngoingTrainingsCount(),
                CertificatesIssued = await _context.Certificates.CountAsync(c => c.RecStatus == "active"),
                OverallCompletionRate = await CalculateCompletionRate(),
                UpcomingTrainings = await GetUpcomingTrainings(),
                OngoingTrainings = await GetOngoingTrainings(),
                CompletedTrainings = await GetCompletedTrainings(), // This will now show past trainings
                PendingApprovals = await GetPendingApprovals(),
                PendingFeedbackCount = await GetPendingFeedbackCount(),
                UpcomingTrainingCount = await _context.TrainingPrograms.CountAsync(tp => tp.StartDate > currentDate && tp.RecStatus == "active"),
                TotalTrainingRegistrations = await _context.TrainingRegistrations.CountAsync(tr => tr.RecStatus == "active"),
                NewHelpQueriesCount = await _context.HelpQueries.CountAsync(hq => hq.ViewedByAdmin == false && hq.RecStatus == "active"),
                UnreadNotificationCount = await _notificationService.GetUnreadNotificationCountAsync(currentUserId, userRole)
            };

            return View(dashboard);
        }

        [HttpGet]
        public IActionResult Trainings()
        {
            return RedirectToAction("TrainingIndex", "Training");
        }

        private async Task<int> GetOngoingTrainingsCount()
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var trainings = await _context.TrainingPrograms
                .Where(tp => tp.RecStatus == "active")
                .ToListAsync();

            return trainings.Count(tp =>
                tp.StartDate <= today &&
                (tp.EndDate > today || (tp.EndDate == today && tp.toTime > currentTime)));
        }

        private async Task<double> CalculateCompletionRate()
        {
            var totalTrainings = await _context.TrainingPrograms.CountAsync(tp => tp.RecStatus == "active");
            if (totalTrainings == 0) return 0;

            var completedTrainings = await _context.TrainingPrograms.CountAsync(tp => tp.Status == "Completed" && tp.RecStatus == "active");
            return Math.Round((double)completedTrainings / totalTrainings * 100, 2);
        }

        private async Task<List<TrainingProgramViewModel>> GetUpcomingTrainings()
        {
            // Fix: Split query to avoid EF translation issues
            var trainingData = await _context.TrainingPrograms
                .Where(tp => tp.StartDate > currentDate && tp.RecStatus == "active")
                .OrderBy(tp => tp.StartDate)
                .Take(5)
                .ToListAsync();

            return trainingData.Select(tp => new TrainingProgramViewModel
            {
                TrainingSysID = tp.TrainingSysID,
                Title = tp.Title,
                TrainerName = tp.TrainerName,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                FromTime = tp.fromTime,
                ToTime = tp.toTime,
                Venue = tp.Venue,
                RegisteredCount = _context.TrainingRegistrations.Count(tr => tr.TrainingSysID == tp.TrainingSysID && tr.RecStatus == "active"),
                Capacity = tp.Capacity,
                Status = tp.Status,
                Mode = tp.Mode
            }).ToList();
        }

        private async Task<List<TrainingProgramViewModel>> GetOngoingTrainings()
        {
            // Fix: Split query to avoid EF translation issues
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var trainingData = await _context.TrainingPrograms
                .Where(tp => tp.RecStatus == "active" &&
                    tp.StartDate <= today &&
                    (tp.EndDate > today || (tp.EndDate == today && tp.toTime > currentTime)))
                .OrderBy(tp => tp.StartDate)
                .Take(5)
                .ToListAsync();


            return trainingData.Select(tp => new TrainingProgramViewModel
            {
                TrainingSysID = tp.TrainingSysID,
                Title = tp.Title,
                TrainerName = tp.TrainerName,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                FromTime = tp.fromTime,
                ToTime = tp.toTime,
                Venue = tp.Venue,
                RegisteredCount = _context.TrainingRegistrations.Count(tr => tr.TrainingSysID == tp.TrainingSysID && tr.RecStatus == "active"),
                Capacity = tp.Capacity,
                Status = tp.Status,
                Mode = tp.Mode
            }).ToList();
        }

        private async Task<List<TrainingProgramViewModel>> GetCompletedTrainings()
        {
            // Show past trainings: Completed or EndDate + ToTime in the past
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var trainingData = (await _context.TrainingPrograms
                .Where(tp => tp.RecStatus == "active" &&
                    (tp.Status == "Completed" ||
                    tp.EndDate < today ||
                    (tp.EndDate == today && tp.toTime < currentTime)))
                .OrderByDescending(tp => tp.EndDate)
                .ToListAsync())
                .Take(5);

            return trainingData.Select(tp => new TrainingProgramViewModel
            {
                TrainingSysID = tp.TrainingSysID,
                Title = tp.Title,
                TrainerName = tp.TrainerName,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                FromTime = tp.fromTime,
                ToTime = tp.toTime,
                Venue = tp.Venue,
                RegisteredCount = _context.TrainingRegistrations.Count(tr => tr.TrainingSysID == tp.TrainingSysID && tr.RecStatus == "active"),
                Capacity = tp.Capacity,
                Status = tp.Status,
                Mode = tp.Mode
            }).ToList();
        }

        // Returns a list of employees who have registered for any training and are awaiting confirmation,
        // and provides a method to approve (set confirmation = true) for a registration.
        private async Task<List<PendingApprovalViewModel>> GetPendingApprovals()
        {
            // List all employees who have registered and are awaiting admin confirmation
            // Include legacy records where pending was stored as false
            // Exclude trainings that are already completed/past
            var today = DateOnly.FromDateTime(DateTime.Now);

            var pending = await _context.TrainingRegistrations
                .Where(tr => tr.Registration == true && (tr.Confirmation == null || tr.Confirmation == false) && tr.RecStatus == "active")
                .Join(_context.Employees,
                    tr => tr.EmployeeSysID,
                    e => e.EmployeeSysID,
                    (tr, e) => new { tr, e })
                .Join(_context.TrainingPrograms,
                    combined => combined.tr.TrainingSysID,
                    tp => tp.TrainingSysID,
                    (combined, tp) => new { combined.tr, combined.e, tp })
                .Where(x => x.tp.RecStatus == "active" && x.tp.EndDate >= today && x.tp.Status != "Completed")
                .Select(x => new PendingApprovalViewModel
                {
                    TrainingRegSysID = x.tr.TrainingRegSysID,
                    EmployeeSysID = x.e.EmployeeSysID,
                    EmployeeName = $"{x.e.FirstName} {x.e.LastName}",
                    TrainingTitle = x.tp.Title,
                    Department = x.e.Department,
                    RegistrationDate = x.tr.CreateDateTime ?? DateTime.Now
                })
                .OrderByDescending(x => x.RegistrationDate)
                .Take(10)
                .ToListAsync();

            return pending;
        }

        [HttpGet]
        public async Task<IActionResult> Registrations(int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Include(tr => tr.EmployeeSys)
                .Where(tr => tr.Registration == true && tr.RecStatus == "active");

            if (trainingId.HasValue)
            {
                query = query.Where(tr => tr.TrainingSysID == trainingId.Value);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) <= end);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(tr => tr.Confirmation == null);
                        break;
                    case "approved":
                        query = query.Where(tr => tr.Confirmation == true);
                        break;
                    case "rejected":
                        query = query.Where(tr => tr.Confirmation == false);
                        break;
                }
            }

            var items = await query
                .OrderByDescending(tr => tr.CreateDateTime)
                .Select(tr => new AdminRegistrationItemViewModel
                {
                    TrainingRegSysID = tr.TrainingRegSysID,
                    TrainingSysID = tr.TrainingSysID,
                    TrainingTitle = tr.TrainingSys.Title,
                    EmployeeSysID = tr.EmployeeSysID,
                    EmployeeName = tr.EmployeeSys.FirstName + " " + tr.EmployeeSys.LastName,
                    Department = tr.EmployeeSys.Department,
                    Confirmation = tr.Confirmation,
                    RegistrationDate = tr.CreateDateTime ?? DateTime.Now,
                    TrainingStatus = tr.TrainingSys.Status
                })
                .ToListAsync();

            ViewBag.TrainingId = trainingId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.TrainingOptions = await _context.TrainingPrograms
                .Where(tp => tp.RecStatus == "active")
                .OrderBy(tp => tp.Title)
                .Select(tp => new { tp.TrainingSysID, tp.Title })
                .ToListAsync();

            return View(items);
        }

        // Exports for the Registrations grid (CSV/Excel) and Print-friendly
        [HttpGet]
        public async Task<IActionResult> ExportRegistrationsCsv(int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Include(tr => tr.EmployeeSys)
                .Where(tr => tr.Registration == true && tr.RecStatus == "active");

            if (trainingId.HasValue)
            {
                query = query.Where(tr => tr.TrainingSysID == trainingId.Value);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) <= end);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(tr => tr.Confirmation == null);
                        break;
                    case "approved":
                        query = query.Where(tr => tr.Confirmation == true);
                        break;
                    case "rejected":
                        query = query.Where(tr => tr.Confirmation == false);
                        break;
                }
            }

            var rows = await query
                .OrderByDescending(tr => tr.CreateDateTime)
                .Select(tr => new
                {
                    Training = tr.TrainingSys.Title,
                    Employee = tr.EmployeeSys.FirstName + " " + tr.EmployeeSys.LastName,
                    Department = tr.EmployeeSys.Department,
                    RegisteredOn = (tr.CreateDateTime ?? DateTime.MinValue),
                    Status = tr.Confirmation == null ? "Pending" : (tr.Confirmation == true ? "Approved" : "Rejected")
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Training,Employee,Department,Registered On,Status");
            foreach (var r in rows)
            {
                var values = new[]
                {
                    EscapeCsv(r.Training ?? string.Empty),
                    EscapeCsv(r.Employee ?? string.Empty),
                    EscapeCsv(r.Department ?? string.Empty),
                    EscapeCsv(r.RegisteredOn.ToString("dd/MM/yyyy HH:mm")),
                    EscapeCsv(r.Status)
                };
                sb.AppendLine(string.Join(',', values));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"registrations-{DateTime.Now:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportRegistrationsExcel(int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Include(tr => tr.EmployeeSys)
                .Where(tr => tr.Registration == true && tr.RecStatus == "active");

            if (trainingId.HasValue)
            {
                query = query.Where(tr => tr.TrainingSysID == trainingId.Value);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) <= end);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(tr => tr.Confirmation == null);
                        break;
                    case "approved":
                        query = query.Where(tr => tr.Confirmation == true);
                        break;
                    case "rejected":
                        query = query.Where(tr => tr.Confirmation == false);
                        break;
                }
            }

            var rows = await query
                .OrderByDescending(tr => tr.CreateDateTime)
                .Select(tr => new
                {
                    Training = tr.TrainingSys.Title,
                    Employee = tr.EmployeeSys.FirstName + " " + tr.EmployeeSys.LastName,
                    Department = tr.EmployeeSys.Department,
                    RegisteredOn = (tr.CreateDateTime ?? DateTime.MinValue),
                    Status = tr.Confirmation == null ? "Pending" : (tr.Confirmation == true ? "Approved" : "Rejected")
                })
                .ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Registrations");
            // Headers
            ws.Cells[1, 1].Value = "Training";
            ws.Cells[1, 2].Value = "Employee";
            ws.Cells[1, 3].Value = "Department";
            ws.Cells[1, 4].Value = "Registered On";
            ws.Cells[1, 5].Value = "Status";

            var row = 2;
            foreach (var r in rows)
            {
                ws.Cells[row, 1].Value = r.Training;
                ws.Cells[row, 2].Value = r.Employee;
                ws.Cells[row, 3].Value = r.Department;
                ws.Cells[row, 4].Value = r.RegisteredOn.ToString("dd/MM/yyyy HH:mm");
                ws.Cells[row, 5].Value = r.Status;
                row++;
            }

            ws.Cells.AutoFitColumns();
            var bytes = package.GetAsByteArray();
            var fileName = $"registrations-{DateTime.Now:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> PrintRegistrations(int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _context.TrainingRegistrations
                .Include(tr => tr.TrainingSys)
                .Include(tr => tr.EmployeeSys)
                .Where(tr => tr.Registration == true && tr.RecStatus == "active");

            if (trainingId.HasValue)
            {
                query = query.Where(tr => tr.TrainingSysID == trainingId.Value);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(tr => (tr.CreateDateTime ?? DateTime.MinValue) <= end);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(tr => tr.Confirmation == null);
                        break;
                    case "approved":
                        query = query.Where(tr => tr.Confirmation == true);
                        break;
                    case "rejected":
                        query = query.Where(tr => tr.Confirmation == false);
                        break;
                }
            }

            var items = await query
                .OrderByDescending(tr => tr.CreateDateTime)
                .Select(tr => new AdminRegistrationItemViewModel
                {
                    TrainingRegSysID = tr.TrainingRegSysID,
                    TrainingSysID = tr.TrainingSysID,
                    TrainingTitle = tr.TrainingSys.Title,
                    EmployeeSysID = tr.EmployeeSysID,
                    EmployeeName = tr.EmployeeSys.FirstName + " " + tr.EmployeeSys.LastName,
                    Department = tr.EmployeeSys.Department,
                    Confirmation = tr.Confirmation,
                    RegistrationDate = tr.CreateDateTime ?? DateTime.Now,
                    TrainingStatus = tr.TrainingSys.Status
                })
                .ToListAsync();

            return View("~/Views/Admin/RegistrationsPrint.cshtml", items);
        }

        private static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var needsQuotes = input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
            var value = input.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{value}\"" : value;
        }

        // Duplicate approval endpoints removed. Now handled by TrainingRegistrationController.

        private async Task<int> GetPendingFeedbackCount()
        {
            return await _context.TrainingRegistrations
                .Where(tr => tr.RecStatus == "active" &&
                           !_context.Feedbacks.Any(f => f.TrainingRegSysID == tr.TrainingRegSysID && f.RecStatus == "active"))
                .CountAsync();
        }

        [HttpGet]
        public async Task<IActionResult> HelpQueries(string status = "all", bool? viewed = null)
        {
            // Build the query
            var queryable = _context.HelpQueries
                .Include(hq => hq.EmployeeSys)
                .Where(hq => hq.RecStatus == "active");

            // Filter by status if specified
            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
            {
                queryable = queryable.Where(hq => hq.Status == status);
            }

            // Filter by viewed status if specified
            if (viewed.HasValue)
            {
                queryable = queryable.Where(hq => hq.ViewedByAdmin == viewed.Value);
            }

            // Get the query results
            var helpQueries = await queryable
                .OrderByDescending(hq => hq.CreateDateTime)
                .Select(hq => new HelpQueryViewModel
                {
                    HelpQueryID = hq.HelpQueryID,
                    EmployeeSysID = hq.EmployeeSysID,
                    EmployeeName = hq.EmployeeSys.FirstName + " " + hq.EmployeeSys.LastName,
                    Name = hq.Name,
                    Email = hq.Email,
                    QueryType = hq.QueryType,
                    Subject = hq.Subject,
                    Message = hq.Message,
                    Status = hq.Status,
                    ViewedByAdmin = hq.ViewedByAdmin,
                    ResolvedDate = hq.ResolvedDate,
                    CreateDateTime = hq.CreateDateTime
                })
                .ToListAsync();

            // Update any unviewed queries to viewed
            var unviewedQueries = await _context.HelpQueries
                .Where(hq => hq.ViewedByAdmin == false && hq.RecStatus == "active")
                .ToListAsync();

            if (unviewedQueries.Any())
            {
                foreach (var helpItem in unviewedQueries)
                {
                    helpItem.ViewedByAdmin = true;
                    helpItem.ModifiedDateTime = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }

            // Set ViewBag for filters
            ViewBag.Status = status;
            ViewBag.Viewed = viewed;

            // Return the view with the queries
            return View(helpQueries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQueryStatus(int id, string status)
        {
            var helpQuery = await _context.HelpQueries.FindAsync(id);
            if (helpQuery == null)
            {
                TempData["ErrorMessage"] = "Query not found.";
                return RedirectToAction(nameof(HelpQueries));
            }

            helpQuery.Status = status;
            if (status == "Resolved")
            {
                helpQuery.ResolvedDate = DateTime.Now;
            }
            helpQuery.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Query status updated successfully.";

            return RedirectToAction(nameof(HelpQueries));
        }
    }
}
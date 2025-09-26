using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly HRDCContext _context;
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
        public AdminController(HRDCContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var dashboard = new AdminDashboardViewModel
            {
                TotalEmployees = await _context.Employees.CountAsync(e => e.RecStatus == "active"),
                ActiveTrainings = await _context.TrainingPrograms.CountAsync(tp => tp.Status == "Ongoing" && tp.RecStatus == "active"),
                CertificatesIssued = await _context.Certificates.CountAsync(c => c.RecStatus == "active"),
                OverallCompletionRate = await CalculateCompletionRate(),
                UpcomingTrainings = await GetUpcomingTrainings(),
                OngoingTrainings = await GetOngoingTrainings(),
                CompletedTrainings = await GetCompletedTrainings(),
                PendingApprovals = await GetPendingApprovals(),
                PendingFeedbackCount = await GetPendingFeedbackCount(),
                UpcomingTrainingCount = await _context.TrainingPrograms.CountAsync(tp => tp.StartDate > currentDate && tp.RecStatus == "active"),
                TotalTrainingRegistrations = await _context.TrainingRegistrations.CountAsync(tr => tr.RecStatus == "active")
            };

            return View(dashboard);
        }

        [HttpGet]
        public IActionResult Trainings()
        {
            return RedirectToAction("TrainingIndex", "Training");
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
            var trainingData = await _context.TrainingPrograms
                .Where(tp => tp.Status == "Ongoing" && tp.RecStatus == "active")
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
            // Fix: Split query to avoid EF translation issues
            var trainingData = await _context.TrainingPrograms
                .Where(tp => tp.Status == "Completed" && tp.RecStatus == "active")
                .OrderByDescending(tp => tp.EndDate)
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

        private async Task<List<PendingApprovalViewModel>> GetPendingApprovals()
        {
            return await _context.TrainingRegistrations
                .Where(tr => tr.Confirmation == null && tr.RecStatus == "active")
                .Join(_context.Employees,
                    tr => tr.EmployeeSysID,
                    e => e.EmployeeSysID,
                    (tr, e) => new { tr, e })
                .Join(_context.TrainingPrograms,
                    combined => combined.tr.TrainingSysID,
                    tp => tp.TrainingSysID,
                    (combined, tp) => new PendingApprovalViewModel
                    {
                        TrainingRegSysID = combined.tr.TrainingRegSysID,
                        EmployeeSysID = combined.e.EmployeeSysID,
                        EmployeeName = $"{combined.e.FirstName} {combined.e.LastName}",
                        TrainingTitle = tp.Title,
                        Department = combined.e.Department,
                        RegistrationDate = combined.tr.CreateDateTime ?? DateTime.Now
                    })
                .Take(5)
                .ToListAsync();
        }

        private async Task<int> GetPendingFeedbackCount()
        {
            return await _context.TrainingRegistrations
                .Where(tr => tr.RecStatus == "active" &&
                           !_context.Feedbacks.Any(f => f.TrainingRegSysID == tr.TrainingRegSysID && f.RecStatus == "active"))
                .CountAsync();
        }
    }
}
using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminReportController : Controller
    {
        private readonly HRDCContext _context;
        private readonly ILogger<AdminReportController> _logger;

        public AdminReportController(
            HRDCContext context,
            ILogger<AdminReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================
        // Utility: Calculate Attendance % (BATCH PROCESSING)
        // =========================
        private async Task<Dictionary<int, decimal>> CalculateAttendancePercentBatch(ICollection<int> trainingRegSysIds)
        {
            if (!trainingRegSysIds.Any())
                return new Dictionary<int, decimal>();

            // Fetch grouped counts from DB (server-side)
            var groupedData = await _context.Attendances
                .Where(a => trainingRegSysIds.Contains(a.TrainingRegSysID))
                .GroupBy(a => a.TrainingRegSysID)
                .Select(g => new
                {
                    TrainingRegSysId = g.Key,
                    Total = g.Count(),
                    Present = g.Count(a => a.IsPresent)
                })
                .ToListAsync();

            // Calculate percentages client-side
            return groupedData
                .ToDictionary(
                    x => x.TrainingRegSysId,
                    x => x.Total > 0 ? (decimal)x.Present * 100 / x.Total : 0m
                );
        }

        // =========================
        // PAGE LOAD: SHOW FILTERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            try
            {
                var trainings = await _context.TrainingPrograms
                    .OrderByDescending(t => t.StartDate)
                    .Select(t => new { t.TrainingSysID, t.Title })
                    .ToListAsync();

                ViewBag.Trainings = trainings;
                return View("~/Views/Admin/Reports.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports page");
                return StatusCode(500, "Internal server error while loading reports");
            }
        }

        // =========================
        // MAIN REPORT DATA (SPECIFIC TRAINING) - REPLACED WITH OVERALL STATISTICS
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetOverallStatistics(
            int? trainingId,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            try
            {
                IQueryable<TrainingRegistration> query = _context.TrainingRegistrations.AsQueryable();

                if (trainingId.HasValue && trainingId > 0)
                {
                    query = query.Where(r => r.TrainingSysID == trainingId);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(r => r.TrainingSys.StartDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(r => r.TrainingSys.EndDate <= toDate.Value);
                }

                var registrations = await query
                    .Include(r => r.TrainingSys)
                    .Include(r => r.EmployeeSys)
                    .ToListAsync();

                if (!registrations.Any())
                    return NotFound("No registrations found for the selected criteria.");

                var totalParticipants = registrations.Count;

                var registrationIds = registrations.Select(r => r.TrainingRegSysID).ToList();
                var attendancePercentages = await CalculateAttendancePercentBatch(registrationIds);

                var averageAttendance = attendancePercentages.Any() ? (decimal)attendancePercentages.Values.Average() : 0m;

                var marksList = registrations
                    .Where(r => r.MarksObtained.HasValue)
                    .Select(r => r.MarksObtained!.Value)
                    .ToList();

                var averageMarks = marksList.Any() ? (decimal)marksList.Average() : 0m;

                var passCount = registrations.Count(r =>
                    r.MarksObtained.HasValue &&
                    CalculateResultStatus(r.MarksObtained, r.TrainingSys.MarksOutOf) == "Pass");

                var passRate = totalParticipants > 0 ? (decimal)passCount * 100 / totalParticipants : 0m;

                var statistics = new
                {
                    TotalParticipants = totalParticipants,
                    AverageAttendancePercent = Math.Round(averageAttendance, 2),
                    AverageMarks = Math.Round(averageMarks, 2),
                    PassRatePercent = Math.Round(passRate, 2)
                };

                return Json(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overall statistics");
                return StatusCode(500, "Internal server error while generating overall statistics");
            }
        }



        // =========================
        // HELPER METHODS
        // =========================
        private static string CalculateResultStatus(decimal? marks, decimal? marksOutOf)
        {
            if (!marksOutOf.HasValue || !marks.HasValue)
                return "-";

            var passThreshold = Math.Round(marksOutOf.Value * 0.5m);
            return marks.Value >= passThreshold ? "Pass" : "Fail";
        }

        private static string GenerateCsvContent<T>(IEnumerable<T> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Employee Name,Department,Email,Training,Start Date,End Date,From Time,To Time,Attendance %,Marks,Result");

            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }

                var properties = item.GetType().GetProperties();
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? string.Empty;
                    return EscapeCsv(value);
                });

                sb.AppendLine(string.Join(',', values));
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var needsQuotes = input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
            var value = input.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{value}\"" : value;
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "training";

            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(fileName
                .Where(ch => !invalidChars.Contains(ch))
                .ToArray())
                .Replace(" ", "-")
                .Trim();
        }
    }
}

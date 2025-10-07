using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;
using Mapster;

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
        // MAIN REPORT DATA (SPECIFIC TRAINING)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetReports(
            int? trainingId,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            try
            {
                // Build as IQueryable first
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

                // Include navigation properties after filters
                var queryWithIncludes = query
                    .Include(r => r.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(r => r.TrainingSys);

                var baseData = await queryWithIncludes
                    .Select(r => new
                    {
                        TrainingId = r.TrainingSysID,
                        TrainingName = r.TrainingSys.Title,
                        EmployeeId = r.EmployeeSysID,
                        EmployeeName = r.EmployeeSys.FirstName + " " + r.EmployeeSys.LastName,
                        Department = r.EmployeeSys.Department,
                        Email = r.EmployeeSys.UserSys.Email,
                        StartDate = r.TrainingSys.StartDate,
                        EndDate = r.TrainingSys.EndDate,
                        FromTime = r.TrainingSys.fromTime,
                        ToTime = r.TrainingSys.toTime,
                        MarksOutOf = r.TrainingSys.MarksOutOf,
                        Marks = r.Marks,
                        TrainingRegSysId = r.TrainingRegSysID
                    })
                    .ToListAsync();

                if (!baseData.Any())
                    return NotFound("No registrations found for this training.");

                // Batch calculate attendance percentages once
                var registrationIds = baseData.Select(x => x.TrainingRegSysId).ToList();
                var attendancePercentages = await CalculateAttendancePercentBatch(registrationIds);

                // Mapster mapping per item, then enrich computed fields
                var reportList = baseData
                    .Select(b =>
                    {
                        var vm = b.Adapt<TrainingReportViewModel>();
                        vm.AttendancePercent = attendancePercentages.GetValueOrDefault(b.TrainingRegSysId, 0);
                        vm.ResultStatus = CalculateResultStatus(b.Marks, b.MarksOutOf);
                        return vm;
                    })
                    .OrderBy(x => x.EmployeeName)
                    .ToList();

                return PartialView("~/Views/Admin/Partials/_TrainingReportTable.cshtml", reportList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reports");
                return StatusCode(500, "Internal server error while generating report");
            }
        }

        // =========================
        // EXPORT TO CSV
        // =========================
        [HttpGet]
        public async Task<IActionResult> ExportToCsv(
            int? trainingId,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            try
            {
                // Build as IQueryable first, apply filters, then Includes
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

                var queryWithIncludes = query
                    .Include(r => r.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(r => r.TrainingSys);

                var baseData = await queryWithIncludes
                    .Select(r => new
                    {
                        TrainingRegSysId = r.TrainingRegSysID,
                        EmployeeName = r.EmployeeSys.FirstName + " " + r.EmployeeSys.LastName,
                        Department = r.EmployeeSys.Department,
                        Email = r.EmployeeSys.UserSys.Email,
                        TrainingName = r.TrainingSys.Title,
                        StartDate = r.TrainingSys.StartDate,
                        EndDate = r.TrainingSys.EndDate,
                        FromTime = r.TrainingSys.fromTime,
                        ToTime = r.TrainingSys.toTime,
                        MarksOutOf = r.TrainingSys.MarksOutOf,
                        Marks = r.Marks
                    })
                    .ToListAsync();

                if (!baseData.Any())
                    return NotFound("No data found to export.");

                // Attendance calculated once
                var registrationIds = baseData.Select(x => x.TrainingRegSysId).ToList();
                var attendancePercentages = await CalculateAttendancePercentBatch(registrationIds);

                var csvData = baseData
                    .Select(r => new
                    {
                        r.EmployeeName,
                        r.Department,
                        r.Email,
                        r.TrainingName,
                        StartDate = r.StartDate.ToString("dd/MM/yyyy"),
                        EndDate = r.EndDate.ToString("dd/MM/yyyy"),
                        FromTime = r.FromTime.ToString("HH:mm"),
                        ToTime = r.ToTime.ToString("HH:mm"),
                        AttendancePercent = attendancePercentages.GetValueOrDefault(r.TrainingRegSysId, 0).ToString("0.##"),
                        Marks = r.Marks?.ToString() ?? string.Empty,
                        ResultStatus = CalculateResultStatus(r.Marks, r.MarksOutOf)
                    })
                    .OrderBy(x => x.EmployeeName)
                    .ToList();

                var csvContent = GenerateCsvContent(csvData);
                var bytes = Encoding.UTF8.GetBytes(csvContent);

                var reportName = baseData.First().TrainingName;
                var fileName = $"{SanitizeFileName(reportName)}-report-{DateTime.Now:yyyyMMdd}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting CSV for training {TrainingId}", trainingId);
                return StatusCode(500, "Internal server error while exporting data");
            }
        }

        // =========================
        // EXPORT REPORT TO EXCEL (EPPlus)
        // =========================
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
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

                var queryWithIncludes = query
                    .Include(r => r.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Include(r => r.TrainingSys);

                var trainingData = await queryWithIncludes
                    .Select(r => new
                    {
                        TrainingRegSysId = r.TrainingRegSysID,
                        EmployeeName = $"{r.EmployeeSys.FirstName} {r.EmployeeSys.LastName}",
                        Department = r.EmployeeSys.Department,
                        Email = r.EmployeeSys.UserSys.Email,
                        TrainingName = r.TrainingSys.Title,
                        StartDate = r.TrainingSys.StartDate,
                        EndDate = r.TrainingSys.EndDate,
                        FromTime = r.TrainingSys.fromTime,
                        ToTime = r.TrainingSys.toTime,
                        MarksOutOf = r.TrainingSys.MarksOutOf,
                        Marks = r.Marks
                    })
                    .OrderBy(x => x.EmployeeName)
                    .ToListAsync();

                if (!trainingData.Any())
                    return NotFound("No registrations found for this training.");

                var registrationIds = trainingData.Select(x => x.TrainingRegSysId).ToList();
                var attendancePercentages = await CalculateAttendancePercentBatch(registrationIds);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Report");

                // Headers
                ws.Cells[1, 1].Value = "Employee Name";
                ws.Cells[1, 2].Value = "Department";
                ws.Cells[1, 3].Value = "Email";
                ws.Cells[1, 4].Value = "Training";
                ws.Cells[1, 5].Value = "Start Date";
                ws.Cells[1, 6].Value = "End Date";
                ws.Cells[1, 7].Value = "From Time";
                ws.Cells[1, 8].Value = "To Time";
                ws.Cells[1, 9].Value = "Attendance %";
                ws.Cells[1, 10].Value = "Marks";
                ws.Cells[1, 11].Value = "Result";

                var row = 2;
                foreach (var r in trainingData)
                {
                    var attendancePercent = attendancePercentages.GetValueOrDefault(r.TrainingRegSysId, 0);
                    var result = CalculateResultStatus(r.Marks, r.MarksOutOf);

                    ws.Cells[row, 1].Value = r.EmployeeName;
                    ws.Cells[row, 2].Value = r.Department;
                    ws.Cells[row, 3].Value = r.Email;
                    ws.Cells[row, 4].Value = r.TrainingName;
                    ws.Cells[row, 5].Value = r.StartDate.ToString("dd/MM/yyyy");
                    ws.Cells[row, 6].Value = r.EndDate.ToString("dd/MM/yyyy");
                    ws.Cells[row, 7].Value = r.FromTime.ToString("HH:mm");
                    ws.Cells[row, 8].Value = r.ToTime.ToString("HH:mm");
                    ws.Cells[row, 9].Value = attendancePercent;
                    ws.Cells[row, 10].Value = r.Marks?.ToString() ?? string.Empty;
                    ws.Cells[row, 11].Value = result;
                    row++;
                }

                ws.Cells.AutoFitColumns();
                var bytes = package.GetAsByteArray();
                var reportName = trainingData.First().TrainingName;
                var fileName = $"{SanitizeFileName(reportName)}-report-{DateTime.Now:yyyyMMdd}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel for training {TrainingId}", trainingId);
                return StatusCode(500, "Internal server error while exporting Excel");
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

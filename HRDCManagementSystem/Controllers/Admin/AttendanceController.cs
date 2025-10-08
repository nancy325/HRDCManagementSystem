using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HRDCManagementSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
public class AttendanceController : Controller
{
    private readonly HRDCContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(HRDCContext context, ILogger<AttendanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Admin/Attendance (list trainings eligible for attendance marking)
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var now = DateTime.Now;

            // Pull minimal data then evaluate completion using EndDate + toTime on the client side for correct semantics
            var trainings = (await _context.TrainingPrograms
                .Where(t => t.RecStatus == "active")
                .OrderByDescending(t => t.EndDate)
                .Select(t => new
                {
                    t.TrainingSysID,
                    t.Title,
                    t.StartDate,
                    t.EndDate,
                    t.fromTime,
                    t.toTime,
                    t.Status
                })
                .ToListAsync())
                .Where(t => t.Status == "Completed" || now >= t.EndDate.ToDateTime(t.toTime))
                .Select(t => new
                {
                    t.TrainingSysID,
                    t.Title,
                    t.StartDate,
                    t.EndDate,
                    t.Status
                })
                .ToList();

            return View(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance index");
            TempData["ErrorMessage"] = "Unable to load trainings for attendance right now.";
            return View(Enumerable.Empty<object>());
        }
    }

    // GET: Admin/Attendance/Mark/{trainingId}
    [HttpGet("Mark/{trainingId}")]
    public async Task<IActionResult> Mark(int trainingId)
    {
        try
        {
            var training = await _context.TrainingPrograms.FirstOrDefaultAsync(t => t.TrainingSysID == trainingId && t.RecStatus == "active");
            if (training == null)
            {
                TempData["ErrorMessage"] = "Training not found.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;
            var endsAt = training.EndDate.ToDateTime(training.toTime);
            if (now < endsAt && !string.Equals(training.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Attendance can be marked only after the training is over.";
                return RedirectToAction(nameof(Index));
            }

            var registrations = await _context.TrainingRegistrations
                .Include(r => r.EmployeeSys)
                .Where(r => r.TrainingSysID == trainingId && r.RecStatus == "active" && r.Confirmation == true)
                .ToListAsync();

            var defaultAttendanceDate = training.EndDate;
            var regIds = registrations.Select(r => r.TrainingRegSysID).ToList();
            var existingForDate = await _context.Attendances
                .Where(a => regIds.Contains(a.TrainingRegSysID) && a.AttendanceDate == defaultAttendanceDate)
                .ToListAsync();

            var vm = new AttendanceMarkViewModel
            {
                TrainingSysID = training.TrainingSysID,
                AttendanceDate = defaultAttendanceDate,
                TrainingTitle = training.Title,
                Items = registrations
                    .Select(r => new AttendanceMarkItem
                    {
                        TrainingRegSysID = r.TrainingRegSysID,
                        EmployeeSysID = r.EmployeeSysID,
                        EmployeeName = $"{r.EmployeeSys.FirstName} {r.EmployeeSys.LastName}",
                        IsPresent = existingForDate.Any(a => a.TrainingRegSysID == r.TrainingRegSysID && a.IsPresent)
                    })
                    .OrderBy(i => i.EmployeeName)
                    .ToList(),
                IsAlreadyTaken = existingForDate.Any(),
                AbsentParticipants = existingForDate.Any()
                    ? registrations
                        .Where(r => !existingForDate.Any(a => a.TrainingRegSysID == r.TrainingRegSysID && a.IsPresent))
                        .Select(r => $"{r.EmployeeSys.FirstName} {r.EmployeeSys.LastName}")
                        .OrderBy(n => n)
                        .ToList()
                    : new List<string>()
            };

            if (vm.IsAlreadyTaken)
            {
                TempData["ErrorMessage"] = "Attendance is already taken for this training.";
            }

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance mark page for training {TrainingId}", trainingId);
            TempData["ErrorMessage"] = "Unable to load attendance screen. Please try again later.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Admin/Attendance/Mark/{trainingId}
    [HttpPost("Mark/{trainingId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(int trainingId, AttendanceMarkViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the highlighted errors.";
                return View(model);
            }

            var training = await _context.TrainingPrograms.FirstOrDefaultAsync(t => t.TrainingSysID == model.TrainingSysID && t.RecStatus == "active");
            if (training == null)
            {
                TempData["ErrorMessage"] = "Training not found.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;
            var endsAt = training.EndDate.ToDateTime(training.toTime);
            if (now < endsAt && !string.Equals(training.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Attendance can be marked only after the training is over.";
                return RedirectToAction(nameof(Index));
            }

            var dateToMark = model.AttendanceDate;

            var regIds = model.Items.Select(i => i.TrainingRegSysID).ToList();
            var existing = await _context.Attendances
                .Where(a => regIds.Contains(a.TrainingRegSysID) && a.AttendanceDate == dateToMark)
                .ToListAsync();

            // Prevent re-submission if already taken
            if (existing.Any())
            {
                TempData["ErrorMessage"] = "Attendance has already been taken for this training on the selected date.";
                return RedirectToAction(nameof(Mark), new { trainingId = model.TrainingSysID });
            }

            // Upsert attendance rows for the provided date
            foreach (var item in model.Items)
            {
                var row = existing.FirstOrDefault(a => a.TrainingRegSysID == item.TrainingRegSysID);
                if (row == null)
                {
                    _context.Attendances.Add(new Attendance
                    {
                        TrainingRegSysID = item.TrainingRegSysID,
                        AttendanceDate = dateToMark,
                        IsPresent = item.IsPresent
                    });
                }
                else
                {
                    row.IsPresent = item.IsPresent;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Attendance saved.";
            return RedirectToAction(nameof(Mark), new { trainingId = model.TrainingSysID });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save attendance for training {TrainingId}", model.TrainingSysID);
            TempData["ErrorMessage"] = "An unexpected error occurred while saving attendance.";
            return View(model);
        }
    }

    // GET: Admin/Attendance/ExportCsv?trainingId=1&date=2025-01-01
    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(int trainingId, DateOnly? date)
    {
        try
        {
            var training = await _context.TrainingPrograms.FirstOrDefaultAsync(t => t.TrainingSysID == trainingId);
            if (training == null)
            {
                return NotFound("Training not found");
            }

            var exportDate = date ?? training.EndDate;

            var attendees = await _context.TrainingRegistrations
                .Include(r => r.EmployeeSys)
                .Where(r => r.TrainingSysID == trainingId && r.RecStatus == "active" && r.Confirmation == true)
                .Select(r => new
                {
                    r.TrainingRegSysID,
                    Name = r.EmployeeSys.FirstName + " " + r.EmployeeSys.LastName
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            var regIds = attendees.Select(a => a.TrainingRegSysID).ToList();
            var attendanceRows = await _context.Attendances
                .Where(a => regIds.Contains(a.TrainingRegSysID) && a.AttendanceDate == exportDate)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Employee Name,Present");
            foreach (var a in attendees)
            {
                var present = attendanceRows.Any(x => x.TrainingRegSysID == a.TrainingRegSysID && x.IsPresent);
                var presentText = present ? "Yes" : "No";
                // basic CSV escaping for commas
                var name = a.Name.Contains(',') ? $"\"{a.Name.Replace("\"", "\"\"")}\"" : a.Name;
                sb.AppendLine($"{name},{presentText}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"attendance-{training.Title.Replace(' ', '-')}-{exportDate:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export attendance CSV for training {TrainingId}", trainingId);
            return StatusCode(500, "Internal server error while exporting CSV");
        }
    }
}



using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class RegistrationControlController : Controller
    {
        private readonly HRDCContext _context;

        public RegistrationControlController(HRDCContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
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
                        // Include legacy false as pending
                        query = query.Where(tr => tr.Confirmation == null || tr.Confirmation == false);
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
                    RegistrationDate = tr.CreateDateTime ?? DateTime.Now
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var reg = await _context.TrainingRegistrations.FirstOrDefaultAsync(r => r.TrainingRegSysID == id && r.RecStatus == "active");
            if (reg == null)
            {
                TempData["ErrorMessage"] = "Registration not found.";
            }
            else
            {
                reg.Confirmation = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registration approved.";
            }

            return RedirectToAction("Index", new
            {
                trainingId,
                startDate = startDate?.ToString("yyyy-MM-dd"),
                endDate = endDate?.ToString("yyyy-MM-dd"),
                status
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, int? trainingId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var reg = await _context.TrainingRegistrations.FirstOrDefaultAsync(r => r.TrainingRegSysID == id && r.RecStatus == "active");
            if (reg == null)
            {
                TempData["ErrorMessage"] = "Registration not found.";
            }
            else
            {
                reg.Confirmation = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registration rejected.";
            }

            return RedirectToAction("Index", new
            {
                trainingId,
                startDate = startDate?.ToString("yyyy-MM-dd"),
                endDate = endDate?.ToString("yyyy-MM-dd"),
                status
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportApprovedCsv(int trainingId)
        {
            var approved = await _context.TrainingRegistrations
                .Include(tr => tr.EmployeeSys)
                .Include(tr => tr.TrainingSys)
                .Where(tr => tr.TrainingSysID == trainingId && tr.Registration == true && tr.Confirmation == true && tr.RecStatus == "active")
                .OrderBy(tr => tr.EmployeeSys.FirstName)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Training,Employee Name,Department,Email,Registered On,Status");
            foreach (var r in approved)
            {
                var training = r.TrainingSys?.Title ?? string.Empty;
                var name = (r.EmployeeSys?.FirstName + " " + r.EmployeeSys?.LastName)?.Trim() ?? string.Empty;
                var dept = r.EmployeeSys?.Department ?? string.Empty;
                var email = r.EmployeeSys?.UserSys?.Email ?? string.Empty;
                var regOn = (r.CreateDateTime ?? DateTime.MinValue).ToString("yyyy-MM-dd HH:mm");
                var status = "Approved";
                sb.AppendLine($"\"{training}\",\"{name}\",\"{dept}\",\"{email}\",\"{regOn}\",\"{status}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"approved_participants_training_{trainingId}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}



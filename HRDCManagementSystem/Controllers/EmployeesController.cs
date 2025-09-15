//using HRDCManagementSystem.Data;
//using HRDCManagementSystem.Models.Participant;
//using HRDCManagementSystem.Models.Entities;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace HRDCManagementSystem.Controllers
//{
//    [Authorize(Roles = "Employee")]
//    public class EmployeesController : Controller
//    {
//        private readonly HRDCContext _context;

//        public EmployeesController(HRDCContext context)
//        {
//            _context = context;
//        }

//        [HttpGet]
//        public async Task<IActionResult> Dashboard()
//        {
//            var userSysId = HttpContext.Session.GetInt32("UserSysID");
//            if (userSysId == null)
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            var user = await _context.UserMasters
//                .Include(u => u.Employees)
//                .FirstOrDefaultAsync(u => u.UserSysID == userSysId && u.RecStatus == "active");

//            if (user == null || user.Employees == null || !user.Employees.Any())
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            var employee = user.Employees.First();
            
//            var dashboard = new DashboardViewModel
//            {
//                EmployeeName = $"{employee.FirstName} {employee.LastName}",
//                Department = employee.Department,
//                Designation = employee.Designation,
//                TotalTrainings = await _context.TrainingRegistrations
//                    .CountAsync(tr => tr.EmployeeSysID == employee.EmployeeSysID && tr.RecStatus == "active"),
//                CompletedTrainings = await _context.TrainingRegistrations
//                    .CountAsync(tr => tr.EmployeeSysID == employee.EmployeeSysID && 
//                                    tr.RecStatus == "active" && 
//                                    tr.TrainingProgram.Status == "Completed"),
//                UpcomingTrainings = await GetUpcomingTrainings(employee.EmployeeSysID),
//                RecentTrainings = await GetRecentTrainings(employee.EmployeeSysID)
//            };

//            return View(dashboard);
//        }

//        private async Task<List<TrainingViewModel>> GetUpcomingTrainings(int employeeSysId)
//        {
//            return await _context.TrainingRegistrations
//                .Where(tr => tr.EmployeeSysID == employeeSysId && 
//                           tr.RecStatus == "active" &&
//                           tr.TrainingProgram.StartDate > DateOnly.FromDateTime(DateTime.Now))
//                .Include(tr => tr.TrainingProgram)
//                .OrderBy(tr => tr.TrainingProgram.StartDate)
//                .Select(tr => new TrainingViewModel
//                {
//                    TrainingSysID = tr.TrainingProgram.TrainingSysID,
//                    Title = tr.TrainingProgram.Title,
//                    TrainerName = tr.TrainingProgram.TrainerName,
//                    StartDate = tr.TrainingProgram.StartDate,
//                    EndDate = tr.TrainingProgram.EndDate,
//                    FromTime = tr.TrainingProgram.fromTime,
//                    ToTime = tr.TrainingProgram.toTime,
//                    Venue = tr.TrainingProgram.Venue,
//                    Status = tr.TrainingProgram.Status,
//                    Mode = tr.TrainingProgram.Mode,
//                    Confirmation = tr.Confirmation
//                })
//                .Take(5)
//                .ToListAsync();
//        }

//        private async Task<List<TrainingViewModel>> GetRecentTrainings(int employeeSysId)
//        {
//            return await _context.TrainingRegistrations
//                .Where(tr => tr.EmployeeSysID == employeeSysId && 
//                           tr.RecStatus == "active")
//                .Include(tr => tr.TrainingProgram)
//                .OrderByDescending(tr => tr.CreateDateTime)
//                .Select(tr => new TrainingViewModel
//                {
//                    TrainingSysID = tr.TrainingProgram.TrainingSysID,
//                    Title = tr.TrainingProgram.Title,
//                    TrainerName = tr.TrainingProgram.TrainerName,
//                    StartDate = tr.TrainingProgram.StartDate,
//                    EndDate = tr.TrainingProgram.EndDate,
//                    FromTime = tr.TrainingProgram.fromTime,
//                    ToTime = tr.TrainingProgram.toTime,
//                    Venue = tr.TrainingProgram.Venue,
//                    Status = tr.TrainingProgram.Status,
//                    Mode = tr.TrainingProgram.Mode,
//                    Confirmation = tr.Confirmation
//                })
//                .Take(5)
//                .ToListAsync();
//        }
//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRDCManagementSystem.Models.Admin;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminDashboardController : Controller
    {
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalParticipants = 1247,
                TrainingsConducted = 58,
                PendingApprovals = 12,
                FeedbackScore = 4.6,
                UpcomingTrainings = GetUpcomingTrainings()
            };

            return View(model);
        }

        private List<TrainingSummary> GetUpcomingTrainings() => new()
        {
            new TrainingSummary { Title = "AI in HR", Date = DateTime.Today.AddDays(3), Registered = 40 },
            new TrainingSummary { Title = "Advanced Excel", Date = DateTime.Today.AddDays(7), Registered = 25 }
        };
    }
}

// Controllers/ParticipantsController.cs
using HRDCManagementSystem.Models;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;


namespace HRDCManagementSystem.Controllers
{
    [Authorize(Roles = "User")] // remove if you don't have auth enabled
    public class ParticipantsController : Controller
    {
        private readonly IParticipantService _service;

        public ParticipantsController(IParticipantService service)
        {
            _service = service;
        }

        // Dashboard
        [HttpGet]
        public IActionResult Dashboard(string q)
        {
            var all = _service.GetAll();
            if (!string.IsNullOrWhiteSpace(q))
            {
                all = all.Where(p => (p.Name + " " + p.Email + " " + p.Course).ToLower().Contains(q.ToLower()));
            }

            var upcoming = _service.GetUpcomingTrainings().Take(5);
            var notifications = _service.GetNotifications();
            var stats = _service.GetStats();

            var vm = new DashboardViewModel
            {
                Participants = all,
                UpcomingTrainings = upcoming,
                Notifications = notifications,
                TotalTrainings = stats.total,
                Completed = stats.completed,
                InProgress = stats.inProgress,
                Certificates = stats.certificates,
                WelcomeName = User?.Identity?.Name ?? "Participant"
            };

            return View(vm);
        }

        // You can add actions for Details/Create/Edit/Delete later
    }
}

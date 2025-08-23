using HRDCManagementSystem.Models;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

public class ParticipantsController : Controller
{
    private readonly IParticipantService _service;
    private readonly ITrainingService _trainingService;

    public ParticipantsController(IParticipantService service, ITrainingService trainingService)
    {
        _service = service;
        _trainingService = trainingService;
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

    // Training Registration
    public IActionResult TrainingRegistration(string searchTerm = "", string filterCategory = "all")
    {
        var viewModel = new TrainingViewModel
        {
            SearchTerm = searchTerm,
            FilterCategory = filterCategory,
            Trainings = _trainingService.GetTrainings(searchTerm, filterCategory)
        };
        return View(viewModel);
    }

    [HttpPost]
    public IActionResult Register(int trainingId)
    {
        TempData["Message"] = _trainingService.RegisterTraining(trainingId);
        return RedirectToAction("TrainingRegistration");
    }


    /// my  training 
    // Show trainings
    public IActionResult MyTraining()
    {
        var trainings = _trainingService.GetRegisteredTrainings();  // uses your existing method
        return View(trainings);
    }
}

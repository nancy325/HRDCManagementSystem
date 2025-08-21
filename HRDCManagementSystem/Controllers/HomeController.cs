using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRDCManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        [Authorize] // only logged-in users can access
        public IActionResult Welcome()
        {
            return Content($"Welcome, {User.Identity?.Name}!");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}

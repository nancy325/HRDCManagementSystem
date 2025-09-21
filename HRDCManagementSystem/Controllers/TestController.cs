using Microsoft.AspNetCore.Mvc;

namespace HRDCManagementSystem.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return Content("🚀 TestController is working!");
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(vm);

            if ((vm.Username == "admin" && vm.Password == "admin123") ||
                (vm.Username == "user" && vm.Password == "user123"))
            {
                var role = vm.Username == "admin" ? "Admin" : "User";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, vm.Username),
                    new Claim(ClaimTypes.Role, role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = vm.RecoverPassword
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (role == "Admin")
                {
                    // Redirect to /Admin/Dashboard
                    return RedirectToAction("Dashboard", "AdminDashboard", new { area = "" });
                }
                else
                {
                    return RedirectToAction("Dashboard", "Participants");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}

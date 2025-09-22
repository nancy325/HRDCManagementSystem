using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models;
using HRDCManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly HRDCContext _context;

        public AccountController(HRDCContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registration(RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _context.UserMasters
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.RecStatus == "active");

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            // Hash the password
            var passwordHasher = new PasswordHasher<UserMaster>();
            var newUser = new UserMaster
            {
                Email = model.Email,
                Role = model.Role
            };
            newUser.Password = passwordHasher.HashPassword(newUser, model.Password);

            _context.UserMasters.Add(newUser);
            await _context.SaveChangesAsync();

            await LoginUserAsync(newUser, RecoverPassword: false);
            
            return RedirectToAction("Dashboard", newUser.Role == "Admin" ? "Admin" : "Employee");
        }


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

            var user = await _context.UserMasters
                .Include(u => u.Employees)
                .FirstOrDefaultAsync(u => u.Email == vm.Email && u.RecStatus == "active");

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(vm);
            }

            var passwordHasher = new PasswordHasher<UserMaster>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, vm.Password);

            if (result != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(vm);
            }

            await LoginUserAsync(user, vm.RecoverPassword);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Dashboard", user.Role == "Admin" ? "Admin" : "Employee");
        }

        private async Task LoginUserAsync(UserMaster user, bool RecoverPassword)
        {
            // Store UserSysID in session
            HttpContext.Session.SetInt32("UserSysID", user.UserSysID);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role);

            // Create claims - Fix: Set NameIdentifier to email instead of UserSysID
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Email), // Fixed: Use email instead of UserSysID
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserSysID", user.UserSysID.ToString())
            };

            // Add employee information if available
            if (user.Employees?.Any() == true)
            {
                var employee = user.Employees.First();
                claims.Add(new Claim("EmployeeSysID", employee.EmployeeSysID.ToString()));

                if (!string.IsNullOrEmpty(employee.FirstName))
                    claims.Add(new Claim(ClaimTypes.GivenName, employee.FirstName));

                if (!string.IsNullOrEmpty(employee.LastName))
                    claims.Add(new Claim(ClaimTypes.Surname, employee.LastName));

                if (!string.IsNullOrEmpty(employee.Department))
                    claims.Add(new Claim("Department", employee.Department));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RecoverPassword,
                ExpiresUtc = RecoverPassword ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear session data
            HttpContext.Session.Remove("UserSysID");
            HttpContext.Session.Remove("Email");
            HttpContext.Session.Remove("Role");
            HttpContext.Session.Remove("EmployeeSysID");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }



        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userSysId = HttpContext.Session.GetInt32("UserSysID");
            if (userSysId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.UserMasters
                .Include(u => u.Employees)
                .FirstOrDefaultAsync(u => u.UserSysID == userSysId && u.RecStatus == "active");

            if (user == null)
            {
                await Logout();
                return RedirectToAction(nameof(Login));
            }

            var profile = new ProfileViewModel
            {
                Email = user.Email,
                Role = user.Role
            };

            if (user.Employees?.Any() == true)
            {
                var employee = user.Employees.First();
                profile.FirstName = employee.FirstName;
                profile.LastName = employee.LastName;
                profile.Department = employee.Department;
                profile.Designation = employee.Designation;
            }

            return View(profile);
        }
    }
}
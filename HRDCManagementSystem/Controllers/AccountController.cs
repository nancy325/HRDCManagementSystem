using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly HRDCContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        // Dictionary to store OTPs with email and expiry time
        // Static so it persists across requests
        private static Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();

        public AccountController(HRDCContext context, IEmailService emailService, ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        //[HttpGet]
        //public IActionResult Registration()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> Registration(RegistrationViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    var existingUser = await _context.UserMasters
        //        .FirstOrDefaultAsync(u => u.Email == model.Email && u.RecStatus == "active");

        //    if (existingUser != null)
        //    {
        //        ModelState.AddModelError("Email", "Email already exists");
        //        return View(model);
        //    }

        //    // Hash the password
        //    var passwordHasher = new PasswordHasher<UserMaster>();
        //    var newUser = new UserMaster
        //    {
        //        Email = model.Email,
        //        Role = model.Role
        //    };
        //    newUser.Password = passwordHasher.HashPassword(newUser, model.Password);

        //    _context.UserMasters.Add(newUser);
        //    await _context.SaveChangesAsync();

        //    await LoginUserAsync(newUser, RecoverPassword: false);

        //    return RedirectToAction("Dashboard", newUser.Role == "Admin" ? "Admin" : "Employee");
        //}


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
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordEmailViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.UserMasters
                .Include(u => u.Employees)
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.RecStatus == "active");

            if (user == null)
            {
                // Don't reveal that the user does not exist
                TempData["SuccessMessage"] = "If your email is registered, you will receive an OTP shortly.";
                return RedirectToAction(nameof(VerifyOTP));
            }

            // Generate random 6-digit OTP
            var random = new Random();
            string otp = random.Next(100000, 999999).ToString();

            // Store OTP in the dictionary with 5-minute expiry
            _otpStore[model.Email] = (otp, DateTime.UtcNow.AddMinutes(5));

            // Get user name if available
            string userName = user.Email;
            if (user.Employees?.Any() == true)
            {
                userName = $"{user.Employees.First().FirstName} {user.Employees.First().LastName}";
            }

            // Send email with OTP
            string subject = "HRDC Password Recovery OTP";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                        <h2 style='color: #003366;'>HRDC Password Recovery</h2>
                        <p>Dear {userName},</p>
                        <p>Your OTP for password recovery is: <strong style='font-size: 18px; background: #f0f0f0; padding: 5px 10px; border-radius: 3px;'>{otp}</strong></p>
                        <p>It will expire in 5 minutes.</p>
                        <p>If you did not request a password reset, please ignore this email or contact support.</p>
                        <p>Regards,<br>Human Resource Development Centre (HRDC)<br>CHARUSAT</p>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(model.Email, subject, body);

            // Store email in TempData to pre-fill the next form
            TempData["RecoveryEmail"] = model.Email;
            TempData["SuccessMessage"] = "OTP has been sent to your email address. Please check your inbox.";

            return RedirectToAction(nameof(VerifyOTP));
        }

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            var model = new VerifyOTPViewModel();

            // Pre-fill email if available in TempData
            if (TempData["RecoveryEmail"] != null)
            {
                model.Email = TempData["RecoveryEmail"].ToString();
                // Keep the value for the POST action
                TempData.Keep("RecoveryEmail");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOTP(VerifyOTPViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if OTP exists for this email
            if (!_otpStore.TryGetValue(model.Email, out var otpInfo))
            {
                ModelState.AddModelError("", "Invalid or expired OTP. Please request a new one.");
                return View(model);
            }

            // Check if OTP is correct and not expired
            if (otpInfo.Otp != model.OTP)
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                return View(model);
            }

            if (DateTime.UtcNow > otpInfo.Expiry)
            {
                // Remove expired OTP
                _otpStore.Remove(model.Email);
                ModelState.AddModelError("", "OTP has expired. Please request a new one.");
                return View(model);
            }

            // OTP is valid, redirect to reset password
            TempData["ResetPasswordEmail"] = model.Email;

            return RedirectToAction(nameof(ResetPassword));
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var model = new ResetPasswordViewModel();

            // Pre-fill email if available in TempData
            if (TempData["ResetPasswordEmail"] != null)
            {
                model.Email = TempData["ResetPasswordEmail"].ToString();

                // Verify this email has a valid OTP entry
                if (!_otpStore.ContainsKey(model.Email))
                {
                    TempData["ErrorMessage"] = "Your session has expired. Please start the password recovery process again.";
                    return RedirectToAction(nameof(ForgotPassword));
                }
            }
            else
            {
                // No email in TempData, redirect back to forgot password
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if the email exists and has a valid OTP entry
            if (!_otpStore.ContainsKey(model.Email))
            {
                TempData["ErrorMessage"] = "Your session has expired. Please restart the password recovery process.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _context.UserMasters
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.RecStatus == "active");

            if (user == null)
            {
                // Don't reveal that the user does not exist
                _otpStore.Remove(model.Email); // Clean up
                TempData["SuccessMessage"] = "Password has been reset successfully.";
                return RedirectToAction(nameof(Login));
            }

            // Reset the password
            var passwordHasher = new PasswordHasher<UserMaster>();
            user.Password = passwordHasher.HashPassword(user, model.NewPassword);

            _context.UserMasters.Update(user);
            await _context.SaveChangesAsync();

            // Remove the OTP from the store as it's been used
            _otpStore.Remove(model.Email);

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendOTP(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return RedirectToAction(nameof(VerifyOTP));
            }

            // Redirect to ForgotPassword with the email pre-filled
            TempData["RecoveryEmail"] = email;
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userSysId = HttpContext.Session.GetInt32("UserSysID");
            if (userSysId == null)
            {
                return RedirectToAction(nameof(Login));
            }
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            var userSysId = HttpContext.Session.GetInt32("UserSysID");
            if (userSysId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = await _context.UserMasters
                .FirstOrDefaultAsync(u => u.UserSysID == userSysId && u.RecStatus == "active");

            if (user == null)
            {
                await Logout();
                return RedirectToAction(nameof(Login));
            }

            var passwordHasher = new PasswordHasher<UserMaster>();
            user.Password = passwordHasher.HashPassword(user, vm.NewPassword);
            _context.UserMasters.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Profile");
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

            var profile = new EmployeeViewModel();

            if (user.Employees?.Any() == true)
            {
                var employee = user.Employees.First();
                profile.EmployeeSysID = employee.EmployeeSysID;
                profile.FirstName = employee.FirstName;
                profile.MiddleName = employee.MiddleName;
                profile.LastName = employee.LastName;
                profile.Department = employee.Department;
                profile.Designation = employee.Designation;
                profile.Institute = employee.Institute;
                profile.PhoneNumber = employee.PhoneNumber;
                profile.AlternatePhone = employee.AlternatePhone;
                profile.Type = employee.Type;
                profile.ProfilePhotoPath = employee.ProfilePhotoPath;
                profile.JoinDate = employee.JoinDate;
                profile.Leftdate = employee.LeftDate;
            }

            return View(profile);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userSysId = HttpContext.Session.GetInt32("UserSysID");
            if (userSysId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.UserMasters
                .FirstOrDefaultAsync(u => u.UserSysID == userSysId && u.RecStatus == "active");

            if (user == null)
            {
                await Logout();
                return RedirectToAction(nameof(Login));
            }

            var viewModel = new UserSettingsViewModel
            {
                NotificationSettings = new NotificationSettingsViewModel
                {
                    IsWebNotificationEnabled = user.IsWebNotificationEnabled
                },
                ChangePassword = new ChangePasswordViewModel()
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWebNotification(bool enabled)
        {
            var userSysId = HttpContext.Session.GetInt32("UserSysID");
            if (userSysId == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                var user = await _context.UserMasters
                    .FirstOrDefaultAsync(u => u.UserSysID == userSysId && u.RecStatus == "active");

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsWebNotificationEnabled = enabled;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Web notifications have been {(enabled ? "enabled" : "disabled")}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating web notification setting");
                return Json(new { success = false, message = "An error occurred while updating notification settings" });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(ChangePasswordViewModel model)
        {
            var userSysId = HttpContext.Session.GetInt32("UserSysID");
            if (userSysId == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid password format" });
            }

            try
            {
                var user = await _context.UserMasters
                    .FirstOrDefaultAsync(u => u.UserSysID == userSysId && u.RecStatus == "active");

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var passwordHasher = new PasswordHasher<UserMaster>();
                user.Password = passwordHasher.HashPassword(user, model.NewPassword);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password");
                return Json(new { success = false, message = "An error occurred while changing password" });
            }
        }
    }
}
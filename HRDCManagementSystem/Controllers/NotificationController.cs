using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRDCManagementSystem.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService, 
            ICurrentUserService currentUserService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Displays all notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userSysId = _currentUserService.GetCurrentUserId() ?? 0;
            var userType = User.FindFirstValue(ClaimTypes.Role);

            if (userSysId == 0 || string.IsNullOrEmpty(userType))
            {
                _logger.LogWarning("User accessed notifications without valid ID or role");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var notifications = await _notificationService.GetNotificationsAsync(userSysId, userType);
                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userSysId);
                TempData["ErrorMessage"] = "Unable to load notifications. Please try again later.";
                return View(new List<Models.ViewModels.NotificationViewModel>());
            }
        }

        /// <summary>
        /// Marks a specific notification as read
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(notificationId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                TempData["ErrorMessage"] = "Unable to mark notification as read. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Marks all notifications for the current user as read
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userSysId = _currentUserService.GetCurrentUserId() ?? 0;
            var userType = User.FindFirstValue(ClaimTypes.Role);

            if (userSysId == 0 || string.IsNullOrEmpty(userType))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await _notificationService.MarkAllAsReadAsync(userSysId, userType);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userSysId);
                TempData["ErrorMessage"] = "Unable to mark all notifications as read. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Gets the count of unread notifications for the current user (for AJAX calls)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userSysId = _currentUserService.GetCurrentUserId() ?? 0;
            var userType = User.FindFirstValue(ClaimTypes.Role);

            if (userSysId == 0 || string.IsNullOrEmpty(userType))
            {
                return Json(new { count = 0 });
            }

            try
            {
                var count = await _notificationService.GetUnreadNotificationCountAsync(userSysId, userType);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count for user {UserId}", userSysId);
                return Json(new { count = 0 });
            }
        }
    }
}
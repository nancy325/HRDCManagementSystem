using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HRDCManagementSystem.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notifications in the application
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirstValue("UserSysID");
                var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);

                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User {UserId} connected to notification hub", userId);
                }

                // Add user to a group based on their role for role-based notifications
                if (!string.IsNullOrEmpty(userRole))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, userRole);
                    _logger.LogInformation("User {UserId} added to {Role} group", userId, userRole);
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationHub OnConnectedAsync");
                await base.OnConnectedAsync();
            }
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = Context.User?.FindFirstValue("UserSysID");
                var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);

                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User {UserId} disconnected from notification hub", userId);
                }

                // Remove user from their role group
                if (!string.IsNullOrEmpty(userRole))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userRole);
                    _logger.LogInformation("User {UserId} removed from {Role} group", userId, userRole);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationHub OnDisconnectedAsync");
                await base.OnDisconnectedAsync(exception);
            }
        }
    }
}
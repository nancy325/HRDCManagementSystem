using HRDCManagementSystem.Data;
using HRDCManagementSystem.Hubs;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRDCManagementSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HRDCContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            HRDCContext context, 
            ICurrentUserService currentUserService,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<int> CreateNotificationAsync(int? userSysId, string? userType, string title, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Attempted to create notification with empty title or message");
                    throw new ArgumentException("Title and message are required");
                }

                var notification = new Notification
                {
                    UserSysID = userSysId,
                    UserType = userType,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedDateTime = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Notify clients via SignalR
                await NotifyClientsAsync(notification);

                return notification.NotificationID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification: {Title}", title);
                throw;
            }
        }

        public async Task<List<NotificationViewModel>> GetNotificationsAsync(int userSysId, string userType)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => (n.UserSysID == userSysId || 
                              (n.UserSysID == null && n.UserType == userType)) && 
                              n.RecStatus == "active")
                    .OrderByDescending(n => n.CreatedDateTime)
                    .Select(n => new NotificationViewModel
                    {
                        NotificationID = n.NotificationID,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        CreatedDateTime = n.CreatedDateTime,
                        UserType = n.UserType
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId} with role {UserType}", userSysId, userType);
                return new List<NotificationViewModel>();
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(int userSysId, string userType)
        {
            try
            {
                return await _context.Notifications
                    .CountAsync(n => (n.UserSysID == userSysId || 
                                   (n.UserSysID == null && n.UserType == userType)) && 
                                   n.IsRead == false && 
                                   n.RecStatus == "active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notification count for user {UserId} with role {UserType}", userSysId, userType);
                return 0;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                    
                    // If user-specific notification
                    if (notification.UserSysID.HasValue)
                    {
                        // Fix for the warning: Use ToString() only if UserSysID has a value
                        string userIdString = notification.UserSysID.Value.ToString();
                        await _hubContext.Clients.User(userIdString)
                            .SendAsync("NotificationRead", notificationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            }
        }

        public async Task MarkAllAsReadAsync(int userSysId, string userType)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => (n.UserSysID == userSysId || 
                              (n.UserSysID == null && n.UserType == userType)) && 
                              n.IsRead == false && 
                              n.RecStatus == "active")
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
                
                // Notify the client that all notifications are read
                // Fix for warning: We know userSysId is not null here as it's an int (not nullable)
                string userIdString = userSysId.ToString();
                await _hubContext.Clients.User(userIdString).SendAsync("AllNotificationsRead");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId} with role {UserType}", userSysId, userType);
            }
        }

        private async Task NotifyClientsAsync(Notification notification)
        {
            try
            {
                var notificationData = new
                {
                    id = notification.NotificationID,
                    title = notification.Title,
                    message = notification.Message
                };

                if (notification.UserSysID.HasValue)
                {
                    // User-specific notification - fix for the warning
                    string userIdString = notification.UserSysID.Value.ToString();
                    await _hubContext.Clients.User(userIdString)
                        .SendAsync("ReceiveNotification", notificationData);
                }
                else if (!string.IsNullOrEmpty(notification.UserType))
                {
                    // Role/Group-based notification
                    await _hubContext.Clients.Group(notification.UserType)
                        .SendAsync("ReceiveNotification", notificationData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification to clients");
            }
        }
    }
}
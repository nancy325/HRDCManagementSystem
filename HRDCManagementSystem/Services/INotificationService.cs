using HRDCManagementSystem.Models.ViewModels;

namespace HRDCManagementSystem.Services
{
    /// <summary>
    /// Interface for notification operations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Creates a new notification
        /// </summary>
        /// <param name="userSysId">Optional user ID if the notification is user-specific</param>
        /// <param name="userType">Optional user type if the notification is role-based</param>
        /// <param name="title">Title of the notification</param>
        /// <param name="message">Body text of the notification</param>
        /// <returns>ID of the created notification</returns>
        Task<int> CreateNotificationAsync(int? userSysId, string? userType, string title, string message);

        /// <summary>
        /// Gets notifications for a specific user or role
        /// </summary>
        /// <param name="userSysId">The user ID</param>
        /// <param name="userType">The user's role</param>
        /// <returns>List of notifications</returns>
        Task<List<NotificationViewModel>> GetNotificationsAsync(int userSysId, string userType);

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        /// <param name="userSysId">The user ID</param>
        /// <param name="userType">The user's role</param>
        /// <returns>Number of unread notifications</returns>
        Task<int> GetUnreadNotificationCountAsync(int userSysId, string userType);

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="notificationId">ID of the notification</param>
        Task MarkAsReadAsync(int notificationId);

        /// <summary>
        /// Marks all notifications for a user as read
        /// </summary>
        /// <param name="userSysId">The user ID</param>
        /// <param name="userType">The user's role</param>
        Task MarkAllAsReadAsync(int userSysId, string userType);
    }
}
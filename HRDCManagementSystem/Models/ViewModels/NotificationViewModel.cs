namespace HRDCManagementSystem.Models.ViewModels
{
    /// <summary>
    /// View model for displaying notification information
    /// </summary>
    public class NotificationViewModel
    {
        /// <summary>
        /// Unique identifier for the notification
        /// </summary>
        public int NotificationID { get; set; }

        /// <summary>
        /// Title/subject of the notification
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Body text of the notification
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Whether the notification has been read by the user
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// When the notification was created
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Optional role/user type this notification is targeted to (null for user-specific)
        /// </summary>
        public string? UserType { get; set; }

        /// <summary>
        /// Returns a concise representation of when the notification was created
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedDateTime;

                if (timeSpan.TotalMinutes < 2)
                    return "just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";

                return CreatedDateTime.ToString("MMM dd, yyyy");
            }
        }
    }
}
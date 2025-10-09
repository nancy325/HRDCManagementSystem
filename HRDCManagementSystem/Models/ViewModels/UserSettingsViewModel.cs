namespace HRDCManagementSystem.Models.ViewModels
{
    public class UserSettingsViewModel
    {
        public NotificationSettingsViewModel NotificationSettings { get; set; } = new();
        public ChangePasswordViewModel ChangePassword { get; set; } = new();
    }

    public class NotificationSettingsViewModel
    {
        public bool IsWebNotificationEnabled { get; set; } = true;
    }
}
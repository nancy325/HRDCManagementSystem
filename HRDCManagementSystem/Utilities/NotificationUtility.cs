using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Utilities
{
    /// <summary>
    /// Utility class for creating notifications for different system events
    /// </summary>
    public static class NotificationUtility
    {
        /// <summary>
        /// Create notification for when a new training is created
        /// </summary>
        public static async Task NotifyNewTraining(INotificationService notificationService, TrainingProgram training)
        {
            if (training == null || string.IsNullOrEmpty(training.Title))
            {
                return;
            }

            // Notify all admins (system-wide for Admin type)
            await notificationService.CreateNotificationAsync(
                null,
                "Admin",
                "New Training Created",
                $"A new training program '{training.Title}' has been added to the system.");

            // If the training has an eligibility type, notify relevant employees
            if (!string.IsNullOrEmpty(training.EligibilityType))
            {
                await notificationService.CreateNotificationAsync(
                    null,
                    "Employee",
                    "New Training Available",
                    $"A new training program '{training.Title}' that you might be eligible for has been added.");
            }
        }

        /// <summary>
        /// Create notification for training registration status change
        /// </summary>
        public static async Task NotifyRegistrationStatusChange(
            INotificationService notificationService,
            TrainingRegistration registration,
            string status)
        {
            if (registration?.EmployeeSys?.UserSysID == null ||
                registration.TrainingSys == null ||
                string.IsNullOrEmpty(status))
            {
                return;
            }

            // We only send to the specific employee
            await notificationService.CreateNotificationAsync(
                registration.EmployeeSys.UserSysID,
                "Employee",
                $"Training Registration {status}",
                $"Your registration for '{registration.TrainingSys.Title}' has been {status.ToLower()}.");
        }

        /// <summary>
        /// Create notification for certificate generation
        /// </summary>
        public static async Task NotifyCertificateGenerated(
            INotificationService notificationService,
            Certificate certificate)
        {
            if (certificate?.RegSys?.EmployeeSys?.UserSysID == null ||
                certificate.RegSys?.TrainingSys == null)
            {
                return;
            }

            await notificationService.CreateNotificationAsync(
                certificate.RegSys.EmployeeSys.UserSysID,
                "Employee",
                "Certificate Generated",
                $"Your certificate for '{certificate.RegSys.TrainingSys.Title}' has been generated and is ready to download.");
        }

        /// <summary>
        /// Create notification for upcoming training sessions
        /// </summary>
        public static async Task NotifyUpcomingTraining(
            INotificationService notificationService,
            HRDCContext context,
            int trainingId,
            int daysNotice)
        {
            var training = await context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == trainingId);

            if (training == null) return;

            var registrations = await context.TrainingRegistrations
                .Include(r => r.EmployeeSys)
                    .ThenInclude(e => e.UserSys)
                .Where(r => r.TrainingSysID == trainingId && r.Confirmation == true && r.RecStatus == "active")
                .ToListAsync();

            foreach (var reg in registrations)
            {
                if (reg.EmployeeSys?.UserSysID != null)
                {
                    await notificationService.CreateNotificationAsync(
                        reg.EmployeeSys.UserSysID,
                        "Employee",
                        "Upcoming Training Reminder",
                        $"Reminder: Your training '{training.Title}' is scheduled to begin in {daysNotice} day{(daysNotice != 1 ? "s" : "")}.");
                }
            }
        }

        /// <summary>
        /// Create notification for feedback requests
        /// </summary>
        public static async Task NotifyFeedbackRequest(
            INotificationService notificationService,
            TrainingRegistration registration)
        {
            if (registration?.EmployeeSys?.UserSysID == null ||
                registration.TrainingSys == null)
            {
                return;
            }

            await notificationService.CreateNotificationAsync(
                registration.EmployeeSys.UserSysID,
                "Employee",
                "Feedback Request",
                $"Please provide your feedback for the training '{registration.TrainingSys.Title}' that you attended.");
        }
    }
}
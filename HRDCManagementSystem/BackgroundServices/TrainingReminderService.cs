using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Services;
using HRDCManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.BackgroundServices
{
    public class TrainingReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrainingReminderService> _logger;

        public TrainingReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<TrainingReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Training Reminder Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckUpcomingTrainings();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking upcoming trainings");
                }

                // Run once per day
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Training Reminder Service is stopping.");
        }

        private async Task CheckUpcomingTrainings()
        {
            _logger.LogInformation("Checking for upcoming trainings...");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HRDCContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var currentDate = DateOnly.FromDateTime(DateTime.Today);

            // Check for trainings starting tomorrow (1 day reminder only)
            var tomorrow = currentDate.AddDays(1);

            var tomorrowTrainings = await context.TrainingPrograms
                .Where(t => t.StartDate == tomorrow &&
                       t.RecStatus == "active" &&
                       t.Status != "Cancelled")
                .ToListAsync();

            _logger.LogInformation($"Found {tomorrowTrainings.Count} trainings starting tomorrow");

            foreach (var training in tomorrowTrainings)
            {
                // Check if we have already sent a reminder for this training
                var hasReminderBeenSent = await CheckIfReminderAlreadySent(context, training.TrainingSysID);

                if (!hasReminderBeenSent)
                {
                    _logger.LogInformation("Sending reminder for training '{TrainingTitle}' (ID: {TrainingId})", 
                        training.Title, training.TrainingSysID);

                    await SendTrainingReminderNotifications(
                        notificationService,
                        context,
                        training);
                }
                else
                {
                    _logger.LogInformation("Reminder already sent for training '{TrainingTitle}' (ID: {TrainingId}), skipping", 
                        training.Title, training.TrainingSysID);
                }
            }
        }

        /// <summary>
        /// Check if a reminder notification has already been sent for a specific training
        /// </summary>
        private async Task<bool> CheckIfReminderAlreadySent(HRDCContext context, int trainingId)
        {
            try
            {
                // Get the training to construct the reminder message pattern
                var training = await context.TrainingPrograms
                    .FirstOrDefaultAsync(t => t.TrainingSysID == trainingId);

                if (training == null) return true; // If training doesn't exist, consider reminder as sent

                // Check if any notification with the reminder pattern exists for this training
                var reminderMessage = $"Reminder: Your training '{training.Title}' is scheduled to begin in 1 day";

                var existingReminder = await context.Notifications
                    .AnyAsync(n => n.Title == "Upcoming Training Reminder" &&
                                  n.Message.StartsWith(reminderMessage) &&
                                  n.RecStatus == "active");

                return existingReminder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if reminder was already sent for training {TrainingId}", trainingId);
                return false; // In case of error, allow reminder to be sent
            }
        }

        /// <summary>
        /// Send training reminder notifications to registered employees
        /// </summary>
        private async Task SendTrainingReminderNotifications(
            INotificationService notificationService,
            HRDCContext context,
            TrainingProgram training)
        {
            try
            {
                // Get all confirmed registrations for this training
                var registrations = await context.TrainingRegistrations
                    .Include(r => r.EmployeeSys)
                        .ThenInclude(e => e.UserSys)
                    .Where(r => r.TrainingSysID == training.TrainingSysID && 
                               r.Confirmation == true && 
                               r.RecStatus == "active")
                    .ToListAsync();

                _logger.LogInformation("Found {Count} confirmed registrations for training '{TrainingTitle}'", 
                    registrations.Count, training.Title);

                if (!registrations.Any())
                {
                    _logger.LogInformation("No confirmed registrations found for training '{TrainingTitle}', skipping reminder", 
                        training.Title);
                    return;
                }

                // Send individual reminder notifications to each registered employee
                var notificationTasks = registrations
                    .Where(r => r.EmployeeSys?.UserSysID != null)
                    .Select(async registration =>
                    {
                        try
                        {
                            await notificationService.CreateNotificationAsync(
                                registration.EmployeeSys.UserSysID,
                                "Employee",
                                "Upcoming Training Reminder",
                                $"Reminder: Your training '{training.Title}' is scheduled to begin in 1 day. " +
                                $"Date: {training.StartDate:dd/MM/yyyy}, Time: {training.fromTime:HH:mm} - {training.toTime:HH:mm}, " +
                                $"Venue: {training.Venue ?? "Online/TBD"}");

                            _logger.LogDebug("Reminder notification sent to employee {EmployeeId} for training '{TrainingTitle}'",
                                registration.EmployeeSys.EmployeeSysID, training.Title);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send reminder notification to employee {EmployeeId} for training '{TrainingTitle}'",
                                registration.EmployeeSys?.EmployeeSysID, training.Title);
                        }
                    });

                await Task.WhenAll(notificationTasks);

                _logger.LogInformation("Completed sending reminder notifications for training '{TrainingTitle}' to {Count} employees",
                    training.Title, registrations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder notifications for training '{TrainingTitle}'", training.Title);
            }
        }
    }
}
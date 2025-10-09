using HRDCManagementSystem.Data;
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
            
            // Check for trainings starting in 3 days
            var threeDaysLater = currentDate.AddDays(3);
            
            var upcomingTrainings = await context.TrainingPrograms
                .Where(t => t.StartDate == threeDaysLater &&
                       t.RecStatus == "active" &&
                       t.Status != "Cancelled")
                .Select(t => t.TrainingSysID)
                .ToListAsync();

            _logger.LogInformation($"Found {upcomingTrainings.Count} trainings starting in 3 days");
            
            foreach (var trainingId in upcomingTrainings)
            {
                await NotificationUtility.NotifyUpcomingTraining(
                    notificationService,
                    context,
                    trainingId,
                    3
                );
            }
            
            // Check for trainings starting tomorrow
            var tomorrow = currentDate.AddDays(1);
            
            var tomorrowTrainings = await context.TrainingPrograms
                .Where(t => t.StartDate == tomorrow &&
                       t.RecStatus == "active" &&
                       t.Status != "Cancelled")
                .Select(t => t.TrainingSysID)
                .ToListAsync();

            _logger.LogInformation($"Found {tomorrowTrainings.Count} trainings starting tomorrow");
            
            foreach (var trainingId in tomorrowTrainings)
            {
                await NotificationUtility.NotifyUpcomingTraining(
                    notificationService,
                    context,
                    trainingId,
                    1
                );
            }
        }
    }
}
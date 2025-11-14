using HRDCManagementSystem.Data;
using HRDCManagementSystem.Helpers;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace HRDCManagementSystem.BackgroundServices
{
    /// <summary>
    /// Background service for handling training notification emails asynchronously
    /// </summary>
    public class TrainingNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TrainingNotificationService> _logger;
        private readonly ConcurrentQueue<TrainingNotificationRequest> _emailQueue;

        public TrainingNotificationService(
            IServiceProvider serviceProvider,
            ILogger<TrainingNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _emailQueue = new ConcurrentQueue<TrainingNotificationRequest>();
        }

        /// <summary>
        /// Queue a training notification for processing
        /// </summary>
        public void QueueTrainingNotification(int trainingId, string triggerType = "created")
        {
            _emailQueue.Enqueue(new TrainingNotificationRequest
            {
                TrainingId = trainingId,
                TriggerType = triggerType,
                QueuedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Queued training notification for training ID {TrainingId} with trigger '{TriggerType}'",
                trainingId, triggerType);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Training Notification Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_emailQueue.TryDequeue(out var request))
                    {
                        await ProcessTrainingNotification(request, stoppingToken);
                    }
                    else
                    {
                        // Wait for 5 seconds before checking the queue again
                        await Task.Delay(5000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Training Notification Service");
                    // Continue processing other notifications
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("Training Notification Service stopped");
        }

        private async Task ProcessTrainingNotification(TrainingNotificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HRDCContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Get the training details
                var training = await context.TrainingPrograms
                    .FirstOrDefaultAsync(t => t.TrainingSysID == request.TrainingId, cancellationToken);

                if (training == null)
                {
                    _logger.LogWarning("Training with ID {TrainingId} not found for notification", request.TrainingId);
                    return;
                }

                // Get eligible employees based on training eligibility type
                var eligibleEmployees = await GetEligibleEmployees(context, training);

                if (!eligibleEmployees.Any())
                {
                    _logger.LogInformation("No eligible employees found for training '{TrainingTitle}' with eligibility '{EligibilityType}'",
                        training.Title, training.EligibilityType ?? "All");
                    return;
                }

                _logger.LogInformation("Processing training notifications for {Count} eligible employees for training '{TrainingTitle}'",
                    eligibleEmployees.Count, training.Title);

                // Send emails in batches to avoid overwhelming the email service
                const int batchSize = 10;
                var batches = eligibleEmployees.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var emailTasks = batch.Select(async employee =>
                    {
                        try
                        {
                            await SendTrainingNotificationEmail(emailService, employee, training, request.TriggerType);
                            _logger.LogDebug("Sent training notification email to {Email} for training '{TrainingTitle}'",
                                employee.UserSys.Email, training.Title);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send training notification email to {Email} for training '{TrainingTitle}'",
                                employee.UserSys?.Email ?? "unknown", training.Title);
                        }
                    });

                    await Task.WhenAll(emailTasks);

                    // Small delay between batches to be gentle on the email service
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(2000, cancellationToken);
                    }
                }

                _logger.LogInformation("Completed processing training notifications for training '{TrainingTitle}'", training.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing training notification for training ID {TrainingId}", request.TrainingId);
            }
        }

        private async Task<List<Employee>> GetEligibleEmployees(HRDCContext context, TrainingProgram training)
        {
            var query = context.Employees
                .Include(e => e.UserSys)
                .Where(e => e.RecStatus == "active" && e.UserSys.RecStatus == "active");

            // Apply eligibility filter based on training eligibility type
            if (!string.IsNullOrEmpty(training.EligibilityType))
            {
                var eligibilityType = training.EligibilityType.Trim().ToLower();

                switch (eligibilityType)
                {
                    case "technical":
                        query = query.Where(e =>
                            e.Type.ToLower().Contains("technical") ||
                            e.Department.ToLower().Contains("it") ||
                            e.Department.ToLower().Contains("computer") ||
                            e.Department.ToLower().Contains("software") ||
                            e.Department.ToLower().Contains("technology") ||
                            e.Department.ToLower().Contains("engineering") ||
                            e.Designation.ToLower().Contains("developer") ||
                            e.Designation.ToLower().Contains("engineer") ||
                            e.Designation.ToLower().Contains("technical") ||
                            e.Designation.ToLower().Contains("analyst") ||
                            e.Designation.ToLower().Contains("architect"));
                        break;

                    case "non-technical":
                        query = query.Where(e =>
                            e.Type.ToLower().Contains("non-technical") ||
                            e.Type.ToLower().Contains("administrative") ||
                            e.Department.ToLower().Contains("hr") ||
                            e.Department.ToLower().Contains("human resource") ||
                            e.Department.ToLower().Contains("finance") ||
                            e.Department.ToLower().Contains("accounting") ||
                            e.Department.ToLower().Contains("admin") ||
                            e.Department.ToLower().Contains("management") ||
                            e.Department.ToLower().Contains("marketing") ||
                            e.Department.ToLower().Contains("sales") ||
                            e.Designation.ToLower().Contains("manager") ||
                            e.Designation.ToLower().Contains("executive") ||
                            e.Designation.ToLower().Contains("officer") ||
                            e.Designation.ToLower().Contains("assistant") ||
                            e.Designation.ToLower().Contains("coordinator") ||
                            e.Designation.ToLower().Contains("admin"));
                        break;

                    case "all":
                    case "general":
                        // No additional filter for general trainings
                        break;

                    default:
                        // Custom eligibility type matching
                        query = query.Where(e =>
                            e.Type.ToLower().Contains(eligibilityType) ||
                            e.Department.ToLower().Contains(eligibilityType) ||
                            e.Designation.ToLower().Contains(eligibilityType));
                        break;
                }
            }

            return await query.ToListAsync();
        }

        private async Task SendTrainingNotificationEmail(
            IEmailService emailService,
            Employee employee,
            TrainingProgram training,
            string triggerType)
        {
            var subject = triggerType switch
            {
                "updated" => $"Training Updated: {training.Title}",
                "reminder" => $"Training Reminder: {training.Title}",
                _ => $"New Training Available: {training.Title}"
            };

            var emailBody = CreateTrainingNotificationEmailBody(employee, training, triggerType);

            await emailService.SendEmailAsync(employee.UserSys.Email, subject, emailBody, true);
        }

        private string CreateTrainingNotificationEmailBody(Employee employee, TrainingProgram training, string triggerType)
        {
            return EmailTemplates.GetTrainingNotificationEmailTemplate(
                firstName: employee.FirstName,
                lastName: employee.LastName,
                trainingTitle: training.Title,
                startDate: training.StartDate.ToDateTime(training.fromTime),
                endDate: training.EndDate.ToDateTime(training.toTime),
                trainerName: training.TrainerName,
                venue: training.Venue ?? "Online/TBD",
                eligibilityType: training.EligibilityType ?? "General",
                capacity: training.Capacity,
                mode: training.Mode,
                department: employee.Department,
                designation: employee.Designation,
                fromTime: training.fromTime,
                toTime: training.toTime,
                triggerType: triggerType
            );
        }
    }

    /// <summary>
    /// Request model for training notification processing
    /// </summary>
    public class TrainingNotificationRequest
    {
        public int TrainingId { get; set; }
        public string TriggerType { get; set; } = "created";
        public DateTime QueuedAt { get; set; }
    }
}
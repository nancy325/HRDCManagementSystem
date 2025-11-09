using HRDCManagementSystem.Data;
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
            var actionText = triggerType switch
            {
                "updated" => "has been updated",
                "reminder" => "is coming up soon",
                _ => "has been added"
            };

            var headerColor = triggerType switch
            {
                "updated" => "#ffc107",
                "reminder" => "#fd7e14",
                _ => "#667eea"
            };

            var iconEmoji = triggerType switch
            {
                "updated" => "??",
                "reminder" => "?",
                _ => "??"
            };

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, {headerColor} 0%, {headerColor}99 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;'>
                            <h1 style='margin: 0; font-size: 28px;'>{iconEmoji} Training {triggerType.ToUpper()}!</h1>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; border: 1px solid #e9ecef;'>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Dear <strong>{employee.FirstName} {employee.LastName}</strong>,</p>
                            
                            <p style='font-size: 16px; margin-bottom: 25px;'>A training program that matches your profile {actionText}:</p>
                            
                            <div style='background: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid {headerColor}; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                <h2 style='color: {headerColor}; margin-top: 0; font-size: 22px;'>{training.Title}</h2>
                                
                                <div style='margin: 15px 0;'>
                                    <p style='margin: 8px 0;'><strong>?? Duration:</strong> {training.StartDate:dd MMMM yyyy} - {training.EndDate:dd MMMM yyyy}</p>
                                    <p style='margin: 8px 0;'><strong>? Time:</strong> {training.fromTime:HH:mm} - {training.toTime:HH:mm}</p>
                                    <p style='margin: 8px 0;'><strong>????? Trainer:</strong> {training.TrainerName}</p>
                                    {(string.IsNullOrEmpty(training.Venue) ? "" : $"<p style='margin: 8px 0;'><strong>?? Venue:</strong> {training.Venue}</p>")}
                                    <p style='margin: 8px 0;'><strong>?? Eligibility:</strong> {training.EligibilityType ?? "General"}</p>
                                    <p style='margin: 8px 0;'><strong>?? Capacity:</strong> {training.Capacity} participants</p>
                                    <p style='margin: 8px 0;'><strong>?? Mode:</strong> {training.Mode}</p>
                                </div>
                            </div>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <p style='font-size: 16px; color: #28a745; font-weight: bold;'>?? {(triggerType == "created" ? "Registration is now open!" : triggerType == "reminder" ? "Don't forget to attend!" : "Check the updated details!")}</p>
                                <p style='font-size: 14px; color: #6c757d;'>Log in to the HRDC portal for more information.</p>
                            </div>
                            
                            <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #b3d7ff;'>
                                <p style='margin: 0; font-size: 14px; color: #0056b3;'>
                                    <strong>?? Note:</strong> This training has been recommended for you based on your role as <strong>{employee.Designation}</strong> in the <strong>{employee.Department}</strong> department.
                                </p>
                            </div>
                            
                            <p style='font-size: 16px; margin: 25px 0 10px 0;'>Don't miss this opportunity to enhance your skills!</p>
                            
                            <p style='font-size: 16px; margin-bottom: 25px;'>
                                Best regards,<br>
                                <strong>Human Resource Development Centre (HRDC)</strong><br>
                                <span style='color: #6c757d;'>CHARUSAT University</span>
                            </p>
                            
                            <hr style='border: none; border-top: 1px solid #e9ecef; margin: 30px 0;'>
                            
                            <p style='font-size: 12px; color: #6c757d; text-align: center; margin: 0;'>
                                This is an automated notification. Please do not reply to this email.<br>
                                For any queries, please contact the HRDC administration.
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
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
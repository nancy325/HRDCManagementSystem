using HRDCManagementSystem.Data;
using HRDCManagementSystem.Helpers;
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
        /// Create notification and send email for when a new training is created - sends only to applicable employees
        /// </summary>
        public static async Task NotifyNewTraining(
            INotificationService notificationService,
            TrainingProgram training,
            HRDCContext context = null,
            IEmailService emailService = null,
            ILogger logger = null)
        {
            if (training == null || string.IsNullOrEmpty(training.Title))
            {
                return;
            }

            try
            {
                // Always notify admins about new training creation
                await notificationService.CreateNotificationAsync(
                    null,
                    "Admin",
                    "New Training Created",
                    $"A new training program '{training.Title}' has been added to the system.");

                // If we have context and email service, send targeted notifications and emails
                if (context != null && emailService != null)
                {
                    var eligibleEmployees = await GetEligibleEmployees(context, training, logger);

                    if (eligibleEmployees.Any())
                    {
                        logger?.LogInformation("Found {Count} eligible employees for training '{Title}' with eligibility type '{EligibilityType}'",
                            eligibleEmployees.Count, training.Title, training.EligibilityType ?? "All");

                        // Send individual notifications to each eligible employee
                        var notificationTasks = eligibleEmployees.Select(async employee =>
                        {
                            try
                            {
                                await notificationService.CreateNotificationAsync(
                                    employee.UserSysID,
                                    "Employee",
                                    "New Training Available",
                                    $"A new training program '{training.Title}' that matches your profile has been added. Registration is now open.");
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError(ex, "Failed to create notification for employee {EmployeeId}", employee.EmployeeSysID);
                            }
                        });

                        await Task.WhenAll(notificationTasks);

                        // Send targeted emails to eligible employees
                        await SendTrainingNotificationEmails(emailService, eligibleEmployees, training, logger);
                    }
                    else
                    {
                        logger?.LogWarning("No eligible employees found for training '{Title}' with eligibility type '{EligibilityType}'",
                            training.Title, training.EligibilityType ?? "All");
                    }
                }
                else
                {
                    // Fallback: create general notification for all employees if context is not available
                    await notificationService.CreateNotificationAsync(
                        null,
                        "Employee",
                        "New Training Available",
                        $"A new training program '{training.Title}' has been added.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in NotifyNewTraining for training '{Title}'", training.Title);
                throw;
            }
        }

        /// <summary>
        /// Get employees eligible for a training based on eligibility type
        /// </summary>
        private static async Task<List<Employee>> GetEligibleEmployees(
            HRDCContext context,
            TrainingProgram training,
            ILogger logger = null)
        {
            try
            {
                var query = context.Employees
                    .Include(e => e.UserSys)
                    .Where(e => e.RecStatus == "active" && e.UserSys.RecStatus == "active");

                // Apply eligibility filter based on training eligibility type
                if (!string.IsNullOrEmpty(training.EligibilityType))
                {
                    var eligibilityType = training.EligibilityType.Trim();

                    switch (eligibilityType.ToLower())
                    {
                        case "technical":
                            // For technical trainings, target employees in technical departments/designations
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
                            // For non-technical trainings, target employees in non-technical departments/designations
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
                            // For general trainings, include all employees (no additional filter)
                            break;

                        default:
                            // For custom eligibility types, try to match by Type, Department, or Designation
                            query = query.Where(e =>
                                e.Type.ToLower().Contains(eligibilityType.ToLower()) ||
                                e.Department.ToLower().Contains(eligibilityType.ToLower()) ||
                                e.Designation.ToLower().Contains(eligibilityType.ToLower()));
                            break;
                    }
                }

                var result = await query.ToListAsync();

                logger?.LogInformation("Eligibility filter applied: '{EligibilityType}' - Found {Count} eligible employees",
                    training.EligibilityType ?? "All", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting eligible employees for training '{Title}'", training.Title);
                return new List<Employee>();
            }
        }

        /// <summary>
        /// Send email notifications to eligible employees about new training
        /// </summary>
        private static async Task SendTrainingNotificationEmails(
            IEmailService emailService,
            List<Employee> eligibleEmployees,
            TrainingProgram training,
            ILogger logger = null)
        {
            try
            {
                // Prepare email content
                string subject = $"New Training Available: {training.Title}";

                // Create email tasks for all eligible employees
                var emailTasks = eligibleEmployees
                    .Where(e => e.UserSys != null && !string.IsNullOrEmpty(e.UserSys.Email))
                    .Select(async employee =>
                    {
                        try
                        {
                            string emailBody = CreateTrainingNotificationEmailBody(employee, training);
                            await emailService.SendEmailAsync(employee.UserSys.Email, subject, emailBody, true);

                            logger?.LogInformation("Training notification email sent to {Email} for training '{Title}'",
                                employee.UserSys.Email, training.Title);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "Failed to send training notification email to {Email}",
                                employee.UserSys?.Email ?? "unknown");
                        }
                    });

                // Send all emails concurrently
                await Task.WhenAll(emailTasks);

                logger?.LogInformation("Completed sending training notification emails to {Count} employees for training '{Title}'",
                    eligibleEmployees.Count, training.Title);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error sending training notification emails for training '{Title}'", training.Title);
            }
        }

        /// <summary>
        /// Create HTML email body for training notification
        /// </summary>
        private static string CreateTrainingNotificationEmailBody(Employee employee, TrainingProgram training)
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
                triggerType: "created"
            );
        }

        /// <summary>
        /// Create notification and send email for training registration status change
        /// </summary>
        public static async Task NotifyRegistrationStatusChange(
            INotificationService notificationService,
            TrainingRegistration registration,
            string status,
            IEmailService emailService = null,
            ILogger logger = null)
        {
            if (registration?.EmployeeSys?.UserSysID == null ||
                registration.TrainingSys == null ||
                string.IsNullOrEmpty(status))
            {
                return;
            }

            try
            {
                // Create notification
                await notificationService.CreateNotificationAsync(
                    registration.EmployeeSys.UserSysID,
                    "Employee",
                    $"Training Registration {status}",
                    $"Your registration for '{registration.TrainingSys.Title}' has been {status.ToLower()}.");

                // Send email if email service is available and user has email
                if (emailService != null && 
                    registration.EmployeeSys.UserSys?.Email != null &&
                    !string.IsNullOrEmpty(registration.EmployeeSys.UserSys.Email))
                {
                    bool isApproved = status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                    
                    string subject = $"Training Registration {status} - {registration.TrainingSys.Title}";
                    
                    string emailBody = EmailTemplates.GetTrainingApprovalEmailTemplate(
                        firstName: registration.EmployeeSys.FirstName,
                        lastName: registration.EmployeeSys.LastName,
                        trainingTitle: registration.TrainingSys.Title,
                        startDate: registration.TrainingSys.StartDate.ToDateTime(registration.TrainingSys.fromTime),
                        endDate: registration.TrainingSys.EndDate.ToDateTime(registration.TrainingSys.toTime),
                        trainerName: registration.TrainingSys.TrainerName,
                        venue: registration.TrainingSys.Venue ?? "Online/TBD",
                        mode: registration.TrainingSys.Mode,
                        isApproved: isApproved
                    );

                    await emailService.SendEmailAsync(
                        registration.EmployeeSys.UserSys.Email,
                        subject,
                        emailBody,
                        true // isHtml
                    );

                    logger?.LogInformation(
                        "Training registration {Status} email sent to {Email} for training '{Title}'",
                        status,
                        registration.EmployeeSys.UserSys.Email,
                        registration.TrainingSys.Title
                    );
                }
                else
                {
                    logger?.LogWarning(
                        "Email service not available or employee email not found for registration {RegistrationId}",
                        registration.TrainingRegSysID
                    );
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, 
                    "Error sending registration status change notification/email for registration {RegistrationId}, Status: {Status}",
                    registration.TrainingRegSysID, status);
                throw;
            }
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

        /// <summary>
        /// Create notification and send email for when a training is updated - sends only to applicable employees
        /// </summary>
        public static async Task NotifyTrainingUpdated(
            INotificationService notificationService,
            TrainingProgram training,
            HRDCContext context = null,
            IEmailService emailService = null,
            ILogger logger = null)
        {
            if (training == null || string.IsNullOrEmpty(training.Title))
            {
                return;
            }

            try
            {
                // Always notify admins about training updates
                await notificationService.CreateNotificationAsync(
                    null,
                    "Admin",
                    "Training Updated",
                    $"The training program '{training.Title}' has been updated.");

                // If we have context and email service, send targeted notifications and emails
                if (context != null && emailService != null)
                {
                    var eligibleEmployees = await GetEligibleEmployees(context, training, logger);

                    if (eligibleEmployees.Any())
                    {
                        logger?.LogInformation("Found {Count} eligible employees for updated training '{Title}' with eligibility type '{EligibilityType}'",
                            eligibleEmployees.Count, training.Title, training.EligibilityType ?? "All");

                        // Send individual notifications to each eligible employee
                        var notificationTasks = eligibleEmployees.Select(async employee =>
                        {
                            try
                            {
                                await notificationService.CreateNotificationAsync(
                                    employee.UserSysID,
                                    "Employee",
                                    "Training Updated",
                                    $"The training program '{training.Title}' that matches your profile has been updated. Please check the latest details.");
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError(ex, "Failed to create notification for employee {EmployeeId}", employee.EmployeeSysID);
                            }
                        });

                        await Task.WhenAll(notificationTasks);

                        // Send targeted update emails to eligible employees
                        await SendTrainingUpdateNotificationEmails(emailService, eligibleEmployees, training, logger);
                    }
                    else
                    {
                        logger?.LogWarning("No eligible employees found for updated training '{Title}' with eligibility type '{EligibilityType}'",
                            training.Title, training.EligibilityType ?? "All");
                    }
                }
                else
                {
                    // Fallback: create general notification for all employees if context is not available
                    await notificationService.CreateNotificationAsync(
                        null,
                        "Employee",
                        "Training Updated",
                        $"The training program '{training.Title}' has been updated.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in NotifyTrainingUpdated for training '{Title}'", training.Title);
                throw;
            }
        }

        /// <summary>
        /// Send email notifications to eligible employees about training updates
        /// </summary>
        private static async Task SendTrainingUpdateNotificationEmails(
            IEmailService emailService,
            List<Employee> eligibleEmployees,
            TrainingProgram training,
            ILogger logger = null)
        {
            try
            {
                // Prepare email content
                string subject = $"Training Updated: {training.Title}";

                // Create email tasks for all eligible employees
                var emailTasks = eligibleEmployees
                    .Where(e => e.UserSys != null && !string.IsNullOrEmpty(e.UserSys.Email))
                    .Select(async employee =>
                    {
                        try
                        {
                            string emailBody = CreateTrainingUpdateNotificationEmailBody(employee, training);
                            await emailService.SendEmailAsync(employee.UserSys.Email, subject, emailBody, true);

                            logger?.LogInformation("Training update notification email sent to {Email} for training '{Title}'",
                                employee.UserSys.Email, training.Title);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "Failed to send training update notification email to {Email}",
                                employee.UserSys?.Email ?? "unknown");
                        }
                    });

                // Send all emails concurrently
                await Task.WhenAll(emailTasks);

                logger?.LogInformation("Completed sending training update notification emails to {Count} employees for training '{Title}'",
                    eligibleEmployees.Count, training.Title);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error sending training update notification emails for training '{Title}'", training.Title);
            }
        }

        /// <summary>
        /// Create HTML email body for training update notification
        /// </summary>
        private static string CreateTrainingUpdateNotificationEmailBody(Employee employee, TrainingProgram training)
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
                triggerType: "updated"
            );
        }
    }
}
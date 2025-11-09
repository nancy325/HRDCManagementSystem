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
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;'>
                            <h1 style='margin: 0; font-size: 28px;'>?? New Training Available!</h1>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; border: 1px solid #e9ecef;'>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Dear <strong>{employee.FirstName} {employee.LastName}</strong>,</p>
                            
                            <p style='font-size: 16px; margin-bottom: 25px;'>We're excited to announce a new training program that matches your profile:</p>
                            
                            <div style='background: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                <h2 style='color: #667eea; margin-top: 0; font-size: 22px;'>{training.Title}</h2>
                                
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
                                <p style='font-size: 16px; color: #28a745; font-weight: bold;'>?? Registration is now open!</p>
                                <p style='font-size: 14px; color: #6c757d;'>Log in to the HRDC portal to register for this training.</p>
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
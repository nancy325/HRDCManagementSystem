namespace HRDCManagementSystem.Helpers
{
    public static class EmailTemplates
    {
        public static string GetWelcomeEmailTemplate(string firstName, string email, string password)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #1976d2; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .credentials {{ background-color: #e3f2fd; padding: 15px; margin: 15px 0; border-radius: 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Welcome to HRDC Management System</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {firstName},</p>
                            <p>Your account has been created in the HRDC Management System. Please use the following credentials to log in:</p>
                            
                            <div class='credentials'>
                                <p><strong>Username/Email:</strong> {email}</p>
                                <p><strong>Password:</strong> {password}</p>
                            </div>
                            
                            <p>For security reasons, we recommend changing your password after your first login.</p>
                            <p>If you have any questions, please contact your administrator.</p>
                            
                            <p>Best regards,<br>HRDC Management Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        public static string GetPasswordResetEmailTemplate(string firstName, string email, string newPassword)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #1976d2; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .credentials {{ background-color: #fff3cd; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #ffc107; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>HRDC Management System - Password Reset</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {firstName},</p>
                            <p>Your password has been reset for the HRDC Management System. Please use the following credentials to log in:</p>
                            
                            <div class='credentials'>
                                <p><strong>Username/Email:</strong> {email}</p>
                                <p><strong>New Password:</strong> {newPassword}</p>
                            </div>
                            
                            <p><strong>Important:</strong> For security reasons, please change your password immediately after logging in.</p>
                            <p>If you did not request this password reset, please contact your administrator immediately.</p>
                            
                            <p>Best regards,<br>HRDC Management Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        public static string GetTrainingRegistrationEmailTemplate(string firstName, string trainingTitle, DateTime startDate, DateTime endDate, string venue)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .training-info {{ background-color: #d4edda; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #28a745; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Training Registration Confirmation</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {firstName},</p>
                            <p>You have been successfully registered for the following training:</p>
                            
                            <div class='training-info'>
                                <p><strong>Training Title:</strong> {trainingTitle}</p>
                                <p><strong>Start Date:</strong> {startDate:dd/MM/yyyy}</p>
                                <p><strong>End Date:</strong> {endDate:dd/MM/yyyy}</p>
                                <p><strong>Venue:</strong> {venue}</p>
                            </div>
                            
                            <p>Please make sure to attend the training sessions as scheduled. Further details will be provided by the training coordinator.</p>
                            
                            <p>Best regards,<br>HRDC Management Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        public static string GetTrainingApprovalEmailTemplate(string firstName, string lastName, string trainingTitle,
            DateTime startDate, DateTime endDate, string trainerName, string venue, string mode, bool isApproved)
        {
            var statusText = isApproved ? "APPROVED" : "REJECTED";
            var headerColor = isApproved ? "#28a745" : "#dc3545";
            var statusIcon = isApproved ? "&#9989;" : "&#10060;"; // ? : ?
            var statusMessage = isApproved ? "Great news! Your training registration has been approved." : "We regret to inform you that your training registration has been rejected.";
            var actionText = isApproved ? "You can now prepare to attend the training session." : "You may contact HR for more information about alternative training opportunities.";

            var nextStepsHtml = isApproved ?
                @"<div style='background-color: #d4edda; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #28a745;'>
                    <h3 style='color: #155724; margin-top: 0;'>Next Steps:</h3>
                    <ul style='color: #155724; margin: 0; padding-left: 20px;'>
                        <li>Mark your calendar for the training dates</li>
                        <li>Prepare any required materials</li>
                        <li>Arrive on time at the specified venue</li>
                        <li>Bring a notebook and writing materials</li>
                    </ul>
                </div>" :
                @"<div style='background-color: #f8d7da; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #dc3545;'>
                    <p style='color: #721c24; margin: 0;'>
                        <strong>Note:</strong> This decision may be due to capacity constraints or eligibility requirements. 
                        Please check with your HR department for future training opportunities.
                    </p>
                </div>";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: {headerColor}; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .training-info {{ background-color: white; padding: 20px; margin: 15px 0; border-radius: 5px; border-left: 4px solid {headerColor}; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                        .status-message {{ color: {headerColor}; font-weight: bold; font-size: 18px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>{statusIcon} Training Registration {statusText}</h2>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{firstName} {lastName}</strong>,</p>
                            
                            <p class='status-message'>{statusMessage}</p>
                            
                            <div class='training-info'>
                                <h3 style='color: {headerColor}; margin-top: 0;'>{trainingTitle}</h3>
                                <p><strong>&#128197; Duration:</strong> {startDate:dd MMMM yyyy} - {endDate:dd MMMM yyyy}</p>
                                <p><strong>&#128100; Trainer:</strong> {trainerName}</p>
                                {(string.IsNullOrEmpty(venue) ? "" : $"<p><strong>&#127968; Venue:</strong> {venue}</p>")}
                                <p><strong>&#128187; Mode:</strong> {mode}</p>
                                <p><strong>&#128218; Status:</strong> <span style='color: {headerColor}; font-weight: bold;'>{statusText}</span></p>
                            </div>
                            
                            {nextStepsHtml}
                            
                            <p>{actionText}</p>
                            
                            {(isApproved ?
                                "<p style='color: #28a745; font-weight: bold;'>We look forward to your participation in this training program!</p>" :
                                "<p style='color: #6c757d;'>Thank you for your interest in our training programs.</p>")}
                            
                            <p>Best regards,<br><strong>Human Resource Development Centre (HRDC)</strong><br>CHARUSAT University</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.<br>For any queries, please contact the HRDC administration.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        public static string GetTrainingNotificationEmailTemplate(
            string firstName, 
            string lastName, 
            string trainingTitle, 
            DateTime startDate, 
            DateTime endDate, 
            string trainerName, 
            string venue, 
            string eligibilityType, 
            int capacity, 
            string mode,
            string department,
            string designation,
            TimeOnly fromTime,
            TimeOnly toTime,
            string triggerType = "created")
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

            var iconText = triggerType switch
            {
                "updated" => "&#128204;", // ??
                "reminder" => "&#128276;", // ??
                _ => "&#127891;"  // ??
            };

            var statusMessage = triggerType switch
            {
                "updated" => "Training Updated!",
                "reminder" => "Training Reminder!",
                _ => "New Training Available!"
            };

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, {headerColor} 0%, {headerColor}99 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; border: 1px solid #e9ecef; }}
                        .training-info {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid {headerColor}; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .note-box {{ background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #b3d7ff; }}
                        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1 style='margin: 0; font-size: 28px;'>{iconText} {statusMessage}</h1>
                        </div>
                        
                        <div class='content'>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Dear <strong>{firstName} {lastName}</strong>,</p>
                            
                            <p style='font-size: 16px; margin-bottom: 25px;'>A training program that matches your profile {actionText}:</p>
                            
                            <div class='training-info'>
                                <h2 style='color: {headerColor}; margin-top: 0; font-size: 22px;'>{trainingTitle}</h2>
                                
                                <div style='margin: 15px 0;'>
                                    <p style='margin: 8px 0;'><strong>&#128197; Duration:</strong> {startDate:dd MMMM yyyy} - {endDate:dd MMMM yyyy}</p>
                                    <p style='margin: 8px 0;'><strong>&#128336; Time:</strong> {fromTime:HH:mm} - {toTime:HH:mm}</p>
                                    <p style='margin: 8px 0;'><strong>&#128100; Trainer:</strong> {trainerName}</p>
                                    {(string.IsNullOrEmpty(venue) ? "" : $"<p style='margin: 8px 0;'><strong>&#127968; Venue:</strong> {venue}</p>")}
                                    <p style='margin: 8px 0;'><strong>&#128100; Eligibility:</strong> {eligibilityType ?? "General"}</p>
                                    <p style='margin: 8px 0;'><strong>&#128101; Capacity:</strong> {capacity} participants</p>
                                    <p style='margin: 8px 0;'><strong>&#128187; Mode:</strong> {mode}</p>
                                </div>
                            </div>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <p style='font-size: 16px; color: #28a745; font-weight: bold;'>&#9989; {(triggerType == "created" ? "Registration is now open!" : triggerType == "reminder" ? "Don't forget to attend!" : "Check the updated details!")}</p>
                                <p style='font-size: 14px; color: #6c757d;'>Log in to the HRDC portal for more information.</p>
                            </div>
                            
                            <div class='note-box'>
                                <p style='margin: 0; font-size: 14px; color: #0056b3;'>
                                    <strong>&#128161; Note:</strong> This training has been recommended for you based on your role as <strong>{designation}</strong> in the <strong>{department}</strong> department.
                                </p>
                            </div>
                            
                            <p style='font-size: 16px; margin: 25px 0 10px 0;'>Don't miss this opportunity to enhance your skills!</p>
                            
                            <p style='font-size: 16px; margin-bottom: 25px;'>
                                Best regards,<br>
                                <strong>Human Resource Development Centre (HRDC)</strong><br>
                                <span style='color: #6c757d;'>CHARUSAT University</span>
                            </p>
                            
                            <hr style='border: none; border-top: 1px solid #e9ecef; margin: 30px 0;'>
                            
                            <div class='footer'>
                                <p style='margin: 0;'>
                                    This is an automated notification. Please do not reply to this email.<br>
                                    For any queries, please contact the HRDC administration.
                                </p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}
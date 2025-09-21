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
    }
}
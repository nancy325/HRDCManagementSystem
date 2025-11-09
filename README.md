# HRDC Management System

A comprehensive Human Resource Development Center (HRDC) Management System built with ASP.NET Core MVC for managing employees, training programs, registrations, certificates, and feedback.

## 📋 Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Project Structure](#project-structure)
- [Technologies Used](#technologies-used)
- [Key Functionalities](#key-functionalities)
- [Database](#database)
- [Logging](#logging)

## ✨ Features

### Employee Management
- ✅ Add, view, and manage employee profiles
- ✅ Automatic joining date tracking
- ✅ Employee left date tracking (on deletion)
- ✅ Profile photo upload (optional)
- ✅ Employee self-profile editing
- ✅ Employee type classification (Tech/Non-Tech)
- ✅ Automatic password generation and email notification

### Training Management
- ✅ Create and manage training programs
- ✅ Training registration system
- ✅ Admin approval/rejection workflow
- ✅ Training status tracking (Upcoming, Ongoing, Completed)
- ✅ Google Form test integration
- ✅ Training capacity management
- ✅ Training reminders and notifications

### Certificate Management
- ✅ Automatic certificate generation
- ✅ Certificate template support
- ✅ PDF certificate download
- ✅ Certificate tracking and management

### Feedback System
- ✅ Training feedback collection
- ✅ Feedback question management
- ✅ Feedback analytics

### Notification System
- ✅ Real-time notifications via SignalR
- ✅ Email notifications
- ✅ Web notifications
- ✅ Notification preferences

### Admin Features
- ✅ Comprehensive admin dashboard
- ✅ Training registration approval/rejection
- ✅ Employee management
- ✅ Certificate generation
- ✅ Report generation (CSV, PDF)
- ✅ Help query management

## 🔧 Prerequisites

Before running this application, ensure you have the following installed:

- **.NET 8.0 SDK** or later
- **SQL Server** (Azure SQL Database or Local SQL Server)
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **Git** (for version control)

## 📦 Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd HRDCManagementSystem
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Configure application settings**
   - Open `appsettings.json`
   - Update the `ConnectionStrings:DefaultConnection` with your database connection string
   - Update the `EmailSettings` section with your email configuration
   - **Note**: Never commit sensitive information like passwords or connection strings to version control

4. **Run database migrations** (if applicable)
   ```bash
   dotnet ef database update
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

## ⚙️ Configuration

### appsettings.json

The application requires the following configuration. **All values shown are placeholders and must be replaced with actual values.**

### Using User Secrets (Recommended for Development)

For local development, use .NET User Secrets to store sensitive configuration:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "EmailSettings:Password" "YOUR_EMAIL_PASSWORD"
```

This keeps sensitive data out of your `appsettings.json` file.

#### Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER_ID;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

**⚠️ Important**: Replace all placeholder values with your actual database credentials. Never commit actual connection strings to version control.

#### Email Settings
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderEmail": "YOUR_EMAIL@gmail.com",
    "SenderName": "HRDC Management System",
    "Username": "YOUR_EMAIL@gmail.com",
    "Password": "YOUR_APP_PASSWORD",
    "UseSsl": true
  }
}
```

**⚠️ Important**: 
- Replace all placeholder values with your actual email credentials
- For Gmail, use an App Password instead of your regular password
- Never commit actual email credentials to version control
- Consider using User Secrets or Environment Variables for sensitive configuration

#### Serilog Configuration
The application uses Serilog for logging. Logs are written to the `Logs` folder with daily rolling intervals.

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

## 🚀 Running the Application

1. **Run the application**
   ```bash
   dotnet run
   ```

2. **Open your browser**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - The exact URL will be displayed in the console

3. **Default Login**
   - Admin credentials should be created in the database
   - Contact your system administrator for initial login credentials

## 📁 Project Structure

```
HRDCManagementSystem/
├── Controllers/          # MVC Controllers
│   ├── Admin/           # Admin-specific controllers
│   ├── Employee/        # Employee management
│   ├── Training/        # Training management
│   ├── Certificate/     # Certificate management
│   └── ...
├── Data/                # Database context
│   └── HRDCContext.cs
├── Models/              # Data models
│   ├── Entities/       # Database entities
│   └── ViewModels/     # View models
├── Services/            # Business logic services
│   ├── CertificateService.cs
│   ├── EmailService.cs
│   └── NotificationService.cs
├── Views/              # Razor views
│   ├── Admin/          # Admin views
│   ├── Employee/       # Employee views
│   └── ...
├── wwwroot/            # Static files (CSS, JS, images)
├── Migrations/         # Entity Framework migrations
├── Helpers/            # Utility classes
├── Hubs/               # SignalR hubs
├── BackgroundServices/ # Background tasks
├── Program.cs          # Application entry point
└── appsettings.json    # Configuration file
```

## 🛠️ Technologies Used

### Backend
- **ASP.NET Core 8.0** - Web framework
- **Entity Framework Core 9.0.8** - ORM
- **SQL Server** - Database
- **SignalR** - Real-time communication
- **Serilog** - Logging framework

### Frontend
- **Bootstrap 5** - CSS framework
- **jQuery** - JavaScript library
- **Bootstrap Icons** - Icon library

### Libraries & Packages
- **Mapster** - Object mapping
- **iText7** - PDF generation
- **EPPlus** - Excel file handling
- **MailKit** - Email functionality
- **SkiaSharp** - Image processing
- **Serilog.Sinks.File** - File logging

## 🎯 Key Functionalities

### Employee Management
- **Add Employee**: Admin can add new employees with automatic password generation
- **Employee Profile**: Employees can view and edit their own profiles
- **Join Date**: Automatically set when employee is created
- **Left Date**: Automatically set when employee is deleted
- **Type Classification**: Tech or Non-Tech employee types

### Training Registration
- **Registration**: Employees can register for training programs
- **Approval Workflow**: Admin approves/rejects registrations
- **Status Tracking**: Pending, Approved, Rejected statuses
- **Completed Training Protection**: Cannot approve/reject registrations for completed trainings

### Certificate Generation
- **Automatic Generation**: Certificates generated after training completion
- **Template Support**: Custom certificate templates
- **PDF Export**: Download certificates as PDF
- **Bulk Generation**: Generate certificates for multiple employees

### Notification System
- **Real-time Notifications**: SignalR-based web notifications
- **Email Notifications**: Email alerts for important events
- **Notification Preferences**: Users can manage notification settings

### Logging
- **File Logging**: All exceptions and important events logged to files
- **Daily Rolling**: Log files rotated daily
- **30-Day Retention**: Logs retained for 30 days
- **Structured Logging**: Serilog for structured logging

## 🗄️ Database

The application uses **Entity Framework Core** with **SQL Server** database. The main entities include:

- **Employee** - Employee information
- **UserMaster** - User accounts and authentication
- **TrainingProgram** - Training programs
- **TrainingRegistration** - Training registrations
- **Certificate** - Generated certificates
- **Feedback** - Training feedback
- **Notification** - System notifications
- **HelpQuery** - Help and support queries

### Database Context
The `HRDCContext` class manages database operations with automatic audit field tracking (CreateDateTime, ModifiedDateTime, RecStatus).

## 📝 Logging

The application uses **Serilog** for comprehensive logging:

- **Log Location**: `Logs/log-YYYYMMDD.txt`
- **Log Levels**: Information, Warning, Error, Fatal
- **Exception Logging**: All exceptions are logged with full stack traces
- **Application Events**: Startup, shutdown, and key operations are logged

## 🔐 Security Features

- **Cookie-based Authentication**
- **Role-based Authorization** (Admin, Employee)
- **Password Hashing** using ASP.NET Core Identity Password Hasher
- **CSRF Protection** with Anti-Forgery Tokens
- **SQL Injection Protection** via Entity Framework Core

### Security Best Practices

- ✅ **Never commit sensitive data** to version control
- ✅ Use **User Secrets** for local development
- ✅ Use **Environment Variables** or **Azure Key Vault** for production
- ✅ Keep `appsettings.json` with placeholder values only
- ✅ Regularly rotate passwords and connection strings
- ✅ Review `.gitignore` to ensure sensitive files are excluded

## 📧 Email Configuration

The application sends emails for:
- Welcome emails with login credentials
- Training registration notifications
- Approval/rejection notifications
- Password reset emails

**Security Notes**: 
- For Gmail, use an App Password instead of your regular password
- Store sensitive configuration in User Secrets for development
- Use Environment Variables or Azure Key Vault for production
- Never commit actual credentials to version control

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is proprietary software. All rights reserved.

## 👥 Support

For support and queries, please contact the system administrator or create a help query through the application.

## 🔄 Version History

- **v1.0.0** - Initial release with core functionalities
  - Employee management
  - Training management
  - Certificate generation
  - Notification system
  - Feedback system

---

**Built with ❤️ for HRDC Management**


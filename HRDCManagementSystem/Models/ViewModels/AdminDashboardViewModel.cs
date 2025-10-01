namespace HRDCManagementSystem.Models.Admin
{
    public class AdminDashboardViewModel
    {
        // Statistics
        public int TotalEmployees { get; set; }
        public int ActiveTrainings { get; set; }
        public int CertificatesIssued { get; set; }
        public double OverallCompletionRate { get; set; }

        // Recent Data
        public List<TrainingProgramViewModel> UpcomingTrainings { get; set; } = new();
        public List<TrainingProgramViewModel> OngoingTrainings { get; set; } = new();
        public List<TrainingProgramViewModel> CompletedTrainings { get; set; } = new();
        public List<PendingApprovalViewModel> PendingApprovals { get; set; } = new();

        // Additional analytics
        public int TotalTrainingRegistrations { get; set; }
        public int PendingFeedbackCount { get; set; }
        public int UpcomingTrainingCount { get; set; }
        
        // Help Queries
        public int NewHelpQueriesCount { get; set; }
    }

    public class TrainingProgramViewModel
    {
        public int TrainingSysID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TrainerName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly FromTime { get; set; }
        public TimeOnly ToTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public int RegisteredCount { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
    }

    public class PendingApprovalViewModel
    {
        public int TrainingRegSysID { get; set; }
        public int EmployeeSysID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string TrainingTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    public class AdminRegistrationItemViewModel
    {
        public int TrainingRegSysID { get; set; }
        public int TrainingSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public int EmployeeSysID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool? Confirmation { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
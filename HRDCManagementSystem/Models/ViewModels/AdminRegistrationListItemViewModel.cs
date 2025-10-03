namespace HRDCManagementSystem.Models.ViewModels
{
    public class AdminRegistrationListItemViewModel
    {
        public int TrainingRegSysID { get; set; }
        public int TrainingSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public int EmployeeSysID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool? Confirmation { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string TrainingStatus { get; set; }
    }
}



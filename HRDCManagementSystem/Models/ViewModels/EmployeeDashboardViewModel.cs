namespace HRDCManagementSystem.Models.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public EmployeeViewModel Employee { get; set; }
        public List<TrainingViewModel> UpcomingTrainings { get; set; }
    }
}

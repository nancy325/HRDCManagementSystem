namespace HRDCManagementSystem.Models.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public Models.Entities.Employee Employee { get; set; }
     
        public List<TrainingItemViewModel> UpcomingTrainings { get; set; } = new List<TrainingItemViewModel>();
        public List<TrainingItemViewModel> CompletedTrainings { get; set; } = new List<TrainingItemViewModel>();
        public List<TrainingItemViewModel> InProgressTrainings { get; set; } = new List<TrainingItemViewModel>();
        public int CertificatesCount { get; set; } = 0;
    }
}

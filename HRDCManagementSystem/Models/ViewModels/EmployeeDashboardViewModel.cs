namespace HRDCManagementSystem.Models.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public Models.Entities.Employee Employee { get; set; }

        public List<TrainingViewModel> UpcomingTrainings { get; set; } = new List<TrainingViewModel>();
        public List<TrainingViewModel> CompletedTrainings { get; set; } = new List<TrainingViewModel>();
        public List<TrainingViewModel> InProgressTrainings { get; set; } = new List<TrainingViewModel>();
        public int CertificatesCount { get; set; } = 0;
    }
}

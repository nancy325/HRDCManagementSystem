using HRDCManagementSystem.Models;
using HRDCManagementSystem.Models.Request;

namespace HRDCManagementSystem.Models
{
    public class DashboardViewModel
    {
        //public IEnumerable<Participants> Participants { get; set; }
        //public IEnumerable<Participants> UpcomingTrainings { get; set; }
        public IEnumerable<string> Notifications { get; set; }
        public int TotalTrainings { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Certificates { get; set; }
        public string WelcomeName { get; set; }
        
        // Additional properties for the employee dashboard
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int CompletedTrainings { get; set; }
        public List<TrainingRequest> UpcomingTrainings { get; set; } = new();
        public List<TrainingRequest> RecentTrainings { get; set; } = new();
    }
}

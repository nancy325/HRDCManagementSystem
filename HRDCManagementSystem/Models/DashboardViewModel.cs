namespace HRDCManagementSystem.Models
{
    public class DashboardViewModel
    {
        public IEnumerable<Participants> Participants { get; set; }
        public IEnumerable<Participants> UpcomingTrainings { get; set; }
        public IEnumerable<string> Notifications { get; set; }
        public int TotalTrainings { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Certificates { get; set; }
        public string WelcomeName { get; set; }
    }
}

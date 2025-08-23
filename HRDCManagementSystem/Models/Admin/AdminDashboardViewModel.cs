namespace HRDCManagementSystem.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalParticipants { get; set; }
        public int TrainingsConducted { get; set; }
        public int PendingApprovals { get; set; }
        public double FeedbackScore { get; set; }

        public List<TrainingSummary> UpcomingTrainings { get; set; }
    }

    public class TrainingSummary
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public int Registered { get; set; }
    }
}

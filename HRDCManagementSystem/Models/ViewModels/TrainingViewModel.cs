using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Models.Participant
{
    public class TrainingViewModel
    {
        public int TrainingSysID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TrainerName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly FromTime { get; set; }
        public TimeOnly ToTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public bool? Confirmation { get; set; }
    }
}

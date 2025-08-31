using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Models.Participant
{
    public class TrainingViewModel
    {
        public string SearchTerm { get; set; }
        public string FilterCategory { get; set; }
        public List<Training> Trainings { get; set; }
    }
}

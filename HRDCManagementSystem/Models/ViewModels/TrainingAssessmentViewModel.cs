namespace HRDCManagementSystem.Models.ViewModels
{
    public class TrainingAssessmentViewModel
    {
        public int TrainingSysID { get; set; }
        public string Title { get; set; }
        public string TrainerName { get; set; }
        public string? GoogleFormTestLink { get; set; }
        public DateTime? TestAvailableFrom { get; set; }
        public DateTime? TestAvailableUntil { get; set; }
        public bool? IsMarksEntry { get; set; }
        public int? MarksOutOf { get; set; }
    }
}

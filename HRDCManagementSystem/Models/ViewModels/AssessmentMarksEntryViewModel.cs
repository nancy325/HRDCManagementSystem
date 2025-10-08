namespace HRDCManagementSystem.Models.ViewModels
{
    public class AssessmentMarksEntryViewModel
    {
        public int TrainingId { get; set; }
        public string TrainingTitle { get; set; }
        public int MarksOutOf { get; set; }
        public List<EmployeeMarksViewModel> EmployeeMarks { get; set; } = new();
    }

    public class EmployeeMarksViewModel
    {
        public int RegistrationId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue, ErrorMessage = "Marks obtained must be a non-negative number.")]
        public int? MarksObtained { get; set; }
    }
}

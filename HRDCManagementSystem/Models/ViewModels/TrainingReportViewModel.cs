using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models.ViewModels
{
    public class TrainingReportViewModel
    {
        public int TrainingId { get; set; }
        [Required]
        public string TrainingName { get; set; }
        public int EmployeeId { get; set; }
        [Required]
        public string EmployeeName { get; set; }
        [Required]
        public string Department { get; set; }
        [Required]
        public string Email { get; set; }
        [DisplayFormat(DataFormatString = "{0:0.##}%")]
        public decimal AttendancePercent { get; set; }
        public decimal? Marks { get; set; }
        public string ResultStatus { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly FromTime { get; set; }
        public TimeOnly ToTime { get; set; }
    }
}



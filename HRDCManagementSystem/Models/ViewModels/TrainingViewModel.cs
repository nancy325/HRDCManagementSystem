using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.ViewModels
{
    public class TrainingViewModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TrainingSysID { get; set; }
        public string Title { get; set; } = null!;

        public string TrainerName { get; set; } = null!;
        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly StartDate { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly EndDate { get; set; }

        public TimeOnly FromTime { get; set; }

        public TimeOnly ToTime { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly ValidTill { get; set; }

        public string? Venue { get; set; }

        public string? EligibilityType { get; set; }

        public int Capacity { get; set; }
       
        [Display(Name = "Upload PDF")]
        public IFormFile? FilePath { get; set; } // for new uploads

        public string? ExistingPath {  get; set; } // for showing already uploaded file path/name
        public string Mode { get; set; } = null!;

        public string Status { get; set; } = null!;

        public int? MarksOutOf { get; set; }

        public bool IsMarksEntry { get; set; }
    }
}

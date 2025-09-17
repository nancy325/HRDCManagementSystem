using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models.Entities;

public partial class TrainingProgram: BaseEntity
{
    [Key]
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

    public TimeOnly fromTime { get; set; }

    public TimeOnly toTime { get; set; }
    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly? Validtill { get; set; }


    public string? Venue { get; set; }

    public string? EligibilityType { get; set; }

    public int Capacity { get; set; }

    public string? FilePath { get; set; }

    public string Mode { get; set; } = null!;

    public string Status { get; set; } = null!;
    
    public int? MarksOutOf { get; set; }

    public bool IsMarksEntry { get; set; }

    
    public virtual ICollection<TrainingRegistration> TrainingRegistrations { get; set; } = new List<TrainingRegistration>();
}

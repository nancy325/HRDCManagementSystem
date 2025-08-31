using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class TrainingProgram
{
    public int TrainingSysID { get; set; }

    public string Title { get; set; } = null!;

    public string TrainerName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public TimeOnly fromTime { get; set; }

    public TimeOnly toTime { get; set; }

    public TimeOnly Validtill { get; set; }

    public string? Venue { get; set; }

    public string? EligibilityType { get; set; }

    public int Capacity { get; set; }

    public string? FilePath { get; set; }

    public string Mode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? MarksOutOf { get; set; }

    public bool? IsMarksEntry { get; set; }

    public int? CreateUserId { get; set; }

    public DateTime? CreateDateTime { get; set; }

    public int? ModifiedUserId { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RecStatus { get; set; } = null!;

    public virtual ICollection<TrainingRegistration> TrainingRegistrations { get; set; } = new List<TrainingRegistration>();
}

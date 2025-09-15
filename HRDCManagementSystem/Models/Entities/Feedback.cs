using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class Feedback: BaseEntity
{
    public int FeedbackID { get; set; }

    public int? RegSysID { get; set; }

    public int? QuestionID { get; set; }

    public int? TrainingRating { get; set; }

    public int? TrainerRating { get; set; }

    public string? Comment { get; set; }
    public virtual FeedbackQuestion? Question { get; set; }

    public virtual TrainingRegistration? RegSys { get; set; }
}

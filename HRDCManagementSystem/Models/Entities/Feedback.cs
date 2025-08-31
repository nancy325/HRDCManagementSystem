using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class Feedback
{
    public int FeedbackID { get; set; }

    public int? RegSysID { get; set; }

    public int? QuestionID { get; set; }

    public int? TrainingRating { get; set; }

    public int? TrainerRating { get; set; }

    public string? Comment { get; set; }

    public int? CreateUserId { get; set; }

    public DateTime? CreateDateTime { get; set; }

    public int? ModifiedUserId { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RecStatus { get; set; } = null!;

    public virtual FeedbackQuestion? Question { get; set; }

    public virtual TrainingRegistration? RegSys { get; set; }
}

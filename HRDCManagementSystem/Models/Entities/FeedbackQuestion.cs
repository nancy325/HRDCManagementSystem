using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class FeedbackQuestion: BaseEntity
{
    public int QuestionID { get; set; }

    public string QuestionText { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

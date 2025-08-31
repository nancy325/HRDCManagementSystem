using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class FeedbackQuestion
{
    public int QuestionID { get; set; }

    public string QuestionText { get; set; } = null!;

    public bool IsActive { get; set; }

    public int? CreateUserId { get; set; }

    public DateTime? CreateDateTime { get; set; }

    public int? ModifiedUserId { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RecStatus { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

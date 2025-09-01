using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class TrainingRegistration
{
    public int TrainingRegSysID { get; set; }

    public int? EmployeeSysID { get; set; }

    public int? TrainingSysID { get; set; }

    public string? Remarks { get; set; }

    public bool? Registration { get; set; }

    public bool? Confirmation { get; set; }

    public int? Marks { get; set; }

    public int? CreateUserId { get; set; }

    public DateTime? CreateDateTime { get; set; }

    public int? ModifiedUserId { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RecStatus { get; set; } = null!;

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual Employee? EmployeeSys { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual TrainingProgram? TrainingSys { get; set; }
}

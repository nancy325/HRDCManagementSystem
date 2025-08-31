using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class Attendance
{
    public int AttendanceID { get; set; }

    public int? RegSysID { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public bool IsPresent { get; set; }

    public int? CreateUserId { get; set; }

    public DateTime? CreateDateTime { get; set; }

    public int? ModifiedUserId { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RecStatus { get; set; } = null!;

    public virtual TrainingRegistration? RegSys { get; set; }
}

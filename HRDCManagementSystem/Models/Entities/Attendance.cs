using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class Attendance : BaseEntity
{
    public int AttendanceID { get; set; }

    public int? RegSysID { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public bool IsPresent { get; set; }

    public virtual TrainingRegistration? RegSys { get; set; }
}

using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class Certificate : BaseEntity
{
    public int CertificateSysID { get; set; }

    public int? RegSysID { get; set; }

    public DateOnly? IssueDate { get; set; }

    public bool? IsGenerated { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public TimeOnly? CreatedTime { get; set; }

    public string? CertificatePath { get; set; }

    public virtual TrainingRegistration? RegSys { get; set; }
}

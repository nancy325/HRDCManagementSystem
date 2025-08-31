using System;
using System.Collections.Generic;

namespace HRDCManagementSystem.Models.Entities;

public partial class Employee
{
    public int EmployeeSysID { get; set; }

    public int? UserSysID { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Department { get; set; }

    public string? Designation { get; set; }

    public string? Institute { get; set; }

    public string? ProfilePhotoPath { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AlternatePhone { get; set; }

    public string? Type { get; set; }

    public int? CreateUserId { get; set; }

    public DateTime? CreateDateTime { get; set; }

    public int? ModifiedUserId { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RecStatus { get; set; } = null!;

    public virtual ICollection<TrainingRegistration> TrainingRegistrations { get; set; } = new List<TrainingRegistration>();

    public virtual UserMaster? UserSys { get; set; }
}

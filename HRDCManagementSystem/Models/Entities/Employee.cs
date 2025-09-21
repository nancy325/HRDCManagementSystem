using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public partial class Employee: BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EmployeeSysID { get; set; }
    [ForeignKey("UserSys")]
    public int UserSysID { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Department { get; set; }

    public string? Designation { get; set; }

    public string? Institute { get; set; }

    public string? ProfilePhotoPath { get; set; }

    public string PhoneNumber { get; set; }

    public string? AlternatePhone { get; set; }

    public string? Type { get; set; }
    public virtual ICollection<TrainingRegistration> TrainingRegistrations { get; set; } = new List<TrainingRegistration>();

    public virtual UserMaster UserSys { get; set; }
}

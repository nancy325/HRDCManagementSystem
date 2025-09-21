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
    public int UserSysID { get; set; } // removed [Required] so validation won't fail before controller assigns value

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string MiddleName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string Department { get; set; }

    [Required]
    public string Designation { get; set; }

    [Required]
    public string Institute { get; set; }

    public string? ProfilePhotoPath { get; set; }

    [Required]
    public string Type { get; set; }

    [Required]
    public string PhoneNumber { get; set; }
    public string? AlternatePhone { get; set; }

    [Required]
    public DateTime? JoinDate { get; set; }
    public DateTime? LeftDate { get; set; }
    public virtual ICollection<TrainingRegistration> TrainingRegistrations { get; set; } = new List<TrainingRegistration>();

    public virtual UserMaster UserSys { get; set; }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public partial class TrainingRegistration: BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
    public int TrainingRegSysID { get; set; }
    [ForeignKey("EmployeeSysID")]
    public int EmployeeSysID { get; set; }

    public int TrainingSysID { get; set; }

    public string? Remarks { get; set; }

    public bool Registration { get; set; }

    public bool? Confirmation { get; set; }

    public int? Marks { get; set; }

    
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual Employee? EmployeeSys { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual TrainingProgram? TrainingSys{ get; set; }
}

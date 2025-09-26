using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public partial class Attendance : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AttendanceID { get; set; }

    [ForeignKey("TrainingRegSysID")]
    public int TrainingRegSysID { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public bool IsPresent { get; set; }

    public virtual TrainingRegistration? TrainingRegSys { get; set; }
}

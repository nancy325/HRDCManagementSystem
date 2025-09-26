using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public partial class Certificate : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CertificateSysID { get; set; }
    [ForeignKey("TrainingRegSys")]
    public int TrainingRegSysID { get; set; }

    public DateOnly? IssueDate { get; set; }

    public bool? IsGenerated { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public TimeOnly? CreatedTime { get; set; }

    public string? CertificatePath { get; set; }

    public virtual TrainingRegistration? RegSys { get; set; }
}

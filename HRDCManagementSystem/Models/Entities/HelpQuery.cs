using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public class HelpQuery : BaseEntity
{
    [Key]
    public int HelpQueryID { get; set; }

    [ForeignKey("EmployeeSys")]
    public int EmployeeSysID { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string QueryType { get; set; } = "General";

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Open";

    public bool ViewedByAdmin { get; set; } = false;

    public DateTime? ResolvedDate { get; set; }

    public virtual Employee EmployeeSys { get; set; }
}
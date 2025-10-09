using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public partial class Feedback : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FeedbackID { get; set; }

    [ForeignKey("TrainingRegSysID")]
    public int TrainingRegSysID { get; set; }

    [ForeignKey("QuestionID")]
    public int QuestionID { get; set; }

    public decimal? RatingValue { get; set; }

    public string? ResponseText { get; set; }

    public virtual FeedbackQuestion? Question { get; set; }

    public virtual TrainingRegistration? RegSys { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities;

public partial class FeedbackQuestion : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int QuestionID { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    
    public int? TrainingSysID { get; set; }
    
    public string QuestionType { get; set; } = "Rating";
    
    public bool IsCommon { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

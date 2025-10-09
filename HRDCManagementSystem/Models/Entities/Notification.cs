using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRDCManagementSystem.Models.Entities
{
    /// <summary>
    /// Represents a notification for users in the system
    /// </summary>
    public class Notification : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationID { get; set; }

        [ForeignKey(nameof(UserMaster))]
        public int? UserSysID { get; set; }

        [MaxLength(20)]
        public string? UserType { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual UserMaster? UserMaster { get; set; }
    }
}
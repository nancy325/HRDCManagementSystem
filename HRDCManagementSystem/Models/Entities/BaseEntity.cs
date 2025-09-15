namespace HRDCManagementSystem.Models.Entities
{
    public abstract class BaseEntity
    {
        public int? CreateUserId { get; set; }
        public DateTime? CreateDateTime { get; set; }
        public int? ModifiedUserId { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
        public string RecStatus { get; set; } = "active";
    }
}
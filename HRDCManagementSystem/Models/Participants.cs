namespace HRDCManagementSystem.Models
{
    public class Participants
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }   // e.g. EMP001
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Course { get; set; }       // training title
        public DateTime TrainingDate { get; set; }
        public string Venue { get; set; }
        public string Trainer { get; set; }
        public string Mode { get; set; } //online offline
        public string Status { get; set; } // Approved, Pending, Completed
    }
}

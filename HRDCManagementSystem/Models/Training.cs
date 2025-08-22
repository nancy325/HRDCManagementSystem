namespace HRDCManagementSystem.Models
{
    public class Training
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Duration { get; set; }
        public string Venue { get; set; }
        public string Trainer { get; set; }
        public string Category { get; set; }
        public int Capacity { get; set; }
        public int Enrolled { get; set; }
        public string Status { get; set; }
        public string Prerequisites { get; set; }
        public bool IsRegistered { get; set; }
    }
}

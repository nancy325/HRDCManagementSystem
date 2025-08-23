namespace HRDCManagementSystem.Models
{
    public class Certificate
    {
        public int Id { get; set; }
        public string TrainingTitle { get; set; }
        public string IssueDate { get; set; }
        public string CompletionDate { get; set; }
        public string Trainer { get; set; }
        public string Duration { get; set; }
        public string Grade { get; set; }
        public string CertificateNumber { get; set; }
        public string Category { get; set; }
        public List<string> Skills { get; set; }
        public string ValidUntil { get; set; }
    }
}

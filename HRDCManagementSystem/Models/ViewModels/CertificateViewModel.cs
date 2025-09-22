namespace HRDCManagementSystem.Models.ViewModels
{
    public class CertificateViewModel
    {
        public int CertificateSysID { get; set; }
        public string TrainingTitle { get; set; }
        public DateOnly? IssueDate { get; set; }
        public string CertificatePath { get; set; }
    }
}
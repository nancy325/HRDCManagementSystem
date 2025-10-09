namespace HRDCManagementSystem.Models.ViewModels
{
    public class CertificateViewModel
    {
        public int CertificateSysID { get; set; }
        public int TrainingRegSysID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string TrainingTitle { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public string? CertificatePath { get; set; }
        public bool IsGenerated { get; set; }
    }

    public class MyCertificateViewModel
    {
        public int CertificateSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateOnly IssueDate { get; set; }
        public string? CertificatePath { get; set; }
    }

    public class RegistrationCertificateViewModel
    {
        public int TrainingRegSysID { get; set; }
        public int EmployeeSysID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool HasCertificate { get; set; }
        public string? CertificatePath { get; set; }
        public DateOnly? IssueDate { get; set; }
    }

    public class TrainingCertificatesViewModel
    {
        public int TrainingSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public List<RegistrationCertificateViewModel> Registrations { get; set; } = new List<RegistrationCertificateViewModel>();
    }
}
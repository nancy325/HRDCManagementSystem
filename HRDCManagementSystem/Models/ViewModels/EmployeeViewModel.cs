namespace HRDCManagementSystem.Models.ViewModels
{
    public class EmployeeViewModel
    {
        public int EmployeeSysID { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Institute { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AlternatePhone { get; set; }
        public string? Type { get; set; }
        public string? ProfilePhotoPath { get; set; }

        public DateTime? JoinDate { get; set; }
        public DateTime? Leftdate { get; set; }

        // Convenience property for displaying full name
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();
    }
}

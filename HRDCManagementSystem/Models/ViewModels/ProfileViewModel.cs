namespace HRDCManagementSystem.Models
{
    public class ProfileViewModel
    {
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
    }
}
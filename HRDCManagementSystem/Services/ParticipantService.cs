using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly List<Participants> _data;

        public ParticipantService()
        {
            _data = new List<Participants>
            {
                new Participants { Id=1, EmployeeId="EMP001", Name="John Participant", Email="john@org.com", Phone="9876543210", Course="Digital Marketing Fundamentals", TrainingDate=new DateTime(2025,1,5,9,0,0), Venue="Training Hall A", Trainer="Dr. Sarah Johnson",Mode="Offline", Status="Approved" },
                new Participants { Id=2, EmployeeId="EMP002", Name="Alice Smith", Email="alice@org.com", Phone="9876500011", Course="Project Management Excellence", TrainingDate = new DateTime(2025, 2, 10, 10, 0, 0), Venue = "Conference Room B", Trainer = "Prof. Michael Chen",Mode="Offline", Status = "Pending" },
                new Participants { Id=3, EmployeeId="EMP003", Name="Raj Patel", Email="raj@org.com", Phone="9876500123", Course="Leadership Development Workshop", TrainingDate = new DateTime(2025, 3, 12, 9, 0, 0), Venue = "Auditorium", Trainer = "Ms. Anita Desai",Mode="Offline", Status = "Approved" },
                // add more demo rows as needed
            };
        }

        public IEnumerable<Participants> GetAll() => _data;

        public IEnumerable<Participants> GetUpcomingTrainings()
        {
            var today = DateTime.Now.Date;
            return _data.Where(p => p.TrainingDate.Date >= today).OrderBy(p => p.TrainingDate);
        }

        public IEnumerable<string> GetNotifications()
        {
            return new List<string>
            {
                "Your registration for 'Digital Marketing Fundamentals' has been approved",
                "Training 'Digital Marketing Fundamentals' starts tomorrow at 9:00 AM",
                "Please provide feedback for completed training 'Data Analytics Basics'"
            };
        }

        public (int total, int completed, int inProgress, int certificates) GetStats()
        {
            int total = _data.Count;
            int completed = _data.Count(p => p.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
            int inProgress = _data.Count(p => p.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase));
            int certificates = _data.Count(p => p.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)); // demo
            return (total, completed, inProgress, certificates);
        }
    }
}

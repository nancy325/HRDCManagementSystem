using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Services
{
    public interface ITrainingService
    {
        List<Training> GetTrainings(string searchTerm, string filterCategory);
        List<Training> GetRegisteredTrainings();
        string RegisterTraining(int trainingId);
    }

    public class TrainingService : ITrainingService
    {
        // Static dummy trainings for testing
        private static List<Training> trainings = new List<Training>
        {
            new Training {
                Id = 1,
                Title = "Advanced Data Analytics",
                Description = "Learn advanced statistical methods...",
                Date = "Jan 15, 2025",
                Time = "9:00 AM - 5:00 PM",
                Duration = "8 hours",
                Venue = "Computer Lab A",
                Trainer = "Dr. Amanda Wilson",
                Category = "Technical",
                Capacity = 20,
                Enrolled = 15,
                Status = "open",
                Prerequisites = "Basic statistics knowledge",
                IsRegistered = false,
                Attendance = "present",
                Progress = 100,
                Materials = new List<string> { "Course_Handbook.pdf", "Analytics_Slides.pptx" },
                RegistrationDate = "Dec 10, 2024",
                CompletionDate = "Dec 28, 2024"
            },
            new Training {
                Id = 2,
                Title = "Leadership in Digital Age",
                Description = "Develop leadership skills...",
                Date = "Jan 20, 2025",
                Time = "10:00 AM - 4:00 PM",
                Duration = "6 hours",
                Venue = "Conference Hall B",
                Trainer = "Prof. Robert Kim",
                Category = "Management",
                Capacity = 25,
                Enrolled = 18,
                Status = "open",
                Prerequisites = "2+ years management experience",
                IsRegistered = false,
                Attendance = "pending",
                Progress = 40,
                Materials = new List<string> { "Leadership_Guide.pdf" },
                RegistrationDate = "Jan 5, 2025",
                CompletionDate = "Jan 8, 2025"
            },
            new Training {
                Id = 3,
                Title = "Machine Learning Fundamentals",
                Description = "Introduction to ML algorithms...",
                Date = "Jan 25, 2025",
                Time = "9:00 AM - 6:00 PM",
                Duration = "9 hours",
                Venue = "Tech Center",
                Trainer = "Dr. Lisa Chen",
                Category = "Technical",
                Capacity = 15,
                Enrolled = 15,
                Status = "full",
                Prerequisites = "Programming experience in Python",
                IsRegistered = false,
                Attendance = "pending",
                Progress = 0,
                RegistrationDate = "Dec 10, 2024",
                CompletionDate = "Dec 28, 2024"
            },
            new Training {
                Id = 4,
                Title = "Effective Communication Skills",
                Description = "Enhance communication abilities",
                Date = "Feb 2, 2025",
                Time = "9:00 AM - 1:00 PM",
                Duration = "4 hours",
                Venue = "Training Room C",
                Trainer = "Ms. Jennifer Lopez",
                Category = "Soft Skills",
                Capacity = 30,
                Enrolled = 12,
                Status = "open",
                Prerequisites = "None",
                IsRegistered = false,
                Attendance = "pending",
                Progress = 0,
                RegistrationDate = "Dec 10, 2024",
                CompletionDate = "Dec 28, 2024"
            },
            new Training {
                Id = 5,
                Title = "Financial Planning Workshop",
                Description = "Learn budgeting & investment",
                Date = "Feb 8, 2025",
                Time = "2:00 PM - 6:00 PM",
                Duration = "4 hours",
                Venue = "Seminar Hall",
                Trainer = "Mr. David Brown",
                Category = "Finance",
                Capacity = 20,
                Enrolled = 8,
                Status = "open",
                Prerequisites = "Basic math skills",
                IsRegistered = true,
                Attendance = "pending",
                RegistrationStatus="approved",
                Progress = 0,
                RegistrationDate = "Dec 10, 2024",
                CompletionDate = "Dec 28, 2024"
            }
        };

        // ✅ Get all trainings with optional search/filter
        public List<Training> GetTrainings(string searchTerm, string filterCategory)
        {
            return trainings.Where(t =>
                (string.IsNullOrEmpty(searchTerm) ||
                 t.Title.ToLower().Contains(searchTerm.ToLower()) ||
                 t.Description.ToLower().Contains(searchTerm.ToLower()) ||
                 t.Trainer.ToLower().Contains(searchTerm.ToLower()))
                && (filterCategory == "all" || t.Category == filterCategory)
            ).ToList();
        }

        // ✅ Get only trainings where user is registered
        public List<Training> GetRegisteredTrainings()
        {
            return trainings.Where(t => t.IsRegistered).ToList();
        }

        // ✅ Register a training
        public string RegisterTraining(int trainingId)
        {
            var training = trainings.FirstOrDefault(t => t.Id == trainingId);
            if (training == null) return "Training not found.";
            if (training.IsRegistered) return "Already registered!";
            if (training.Status == "full") return "Training is full!";

            training.IsRegistered = true;
            training.Enrolled++;
            training.RegistrationDate = DateTime.Now.ToString("MMM dd, yyyy");
            training.Attendance = "pending";
            training.Progress = 0;
            // ✅ mark as pending until admin takes action
            training.RegistrationStatus = "pending";

            return $"Successfully registered for {training.Title}. Pending approval.";
        }
    }
}

using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Services
{
    
        public interface ITrainingService
        {
            List<Training> GetTrainings(string searchTerm, string filterCategory);
            string RegisterTraining(int trainingId);
        }

        public class TrainingService : ITrainingService
        {
            private static List<Training> trainings = new List<Training>
        {
            new Training { Id=1, Title="Advanced Data Analytics", Description="Learn advanced statistical methods...", Date="Jan 15, 2025", Time="9:00 AM - 5:00 PM", Duration="8 hours", Venue="Computer Lab A", Trainer="Dr. Amanda Wilson", Category="Technical", Capacity=20, Enrolled=15, Status="open", Prerequisites="Basic statistics knowledge", IsRegistered=false },
            new Training { Id=2, Title="Leadership in Digital Age", Description="Develop leadership skills...", Date="Jan 20, 2025", Time="10:00 AM - 4:00 PM", Duration="6 hours", Venue="Conference Hall B", Trainer="Prof. Robert Kim", Category="Management", Capacity=25, Enrolled=18, Status="open", Prerequisites="2+ years management experience", IsRegistered=true },
            new Training { Id=3, Title="Machine Learning Fundamentals", Description="Introduction to ML algorithms...", Date="Jan 25, 2025", Time="9:00 AM - 6:00 PM", Duration="9 hours", Venue="Tech Center", Trainer="Dr. Lisa Chen", Category="Technical", Capacity=15, Enrolled=15, Status="full", Prerequisites="Programming experience in Python", IsRegistered=false },
            new Training { Id=4, Title="Effective Communication Skills", Description="Enhance communication abilities", Date="Feb 2, 2025", Time="9:00 AM - 1:00 PM", Duration="4 hours", Venue="Training Room C", Trainer="Ms. Jennifer Lopez", Category="Soft Skills", Capacity=30, Enrolled=12, Status="open", Prerequisites="None", IsRegistered=false },
            new Training { Id=5, Title="Financial Planning Workshop", Description="Learn budgeting & investment", Date="Feb 8, 2025", Time="2:00 PM - 6:00 PM", Duration="4 hours", Venue="Seminar Hall", Trainer="Mr. David Brown", Category="Finance", Capacity=20, Enrolled=8, Status="open", Prerequisites="Basic math skills", IsRegistered=false }
        };

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

            public string RegisterTraining(int trainingId)
            {
                var training = trainings.FirstOrDefault(t => t.Id == trainingId);
                if (training == null) return "Training not found.";
                if (training.IsRegistered) return "Already registered!";
                if (training.Status == "full") return "Training is full!";

                training.IsRegistered = true;
                training.Enrolled++;
                return $"Successfully registered for {training.Title}. Pending approval.";
            }
        }
    
}

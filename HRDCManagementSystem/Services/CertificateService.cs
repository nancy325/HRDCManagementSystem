using System.Collections.Generic;
using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Services
{
    public class CertificateService : ICertificateService
    {
        private static readonly List<Certificate> certificates = new List<Certificate>
        {
            new Certificate
            {
                Id = 1,
                TrainingTitle = "Digital Marketing Fundamentals",
                IssueDate = "Dec 28, 2024",
                CompletionDate = "Dec 28, 2024",
                Trainer = "Dr. Sarah Johnson",
                Duration = "8 hours",
                Grade = "A",
                CertificateNumber = "HRDC-2024-DM-001",
                Category = "Technical",
                Skills = new List<string> { "Digital Marketing", "SEO", "Social Media", "Analytics" },
                ValidUntil = "Dec 28, 2027"
            },
            new Certificate
            {
                Id = 2,
                TrainingTitle = "Data Analytics Basics",
                IssueDate = "Dec 15, 2024",
                CompletionDate = "Dec 15, 2024",
                Trainer = "Dr. Lisa Wang",
                Duration = "8 hours",
                Grade = "A+",
                CertificateNumber = "HRDC-2024-DA-002",
                Category = "Technical",
                Skills = new List<string> { "Data Analysis", "Excel", "Visualization", "Statistics" },
                ValidUntil = "Dec 15, 2027"
            },
            new Certificate
            {
                Id = 3,
                TrainingTitle = "Leadership in Digital Age",
                IssueDate = "Nov 30, 2024",
                CompletionDate = "Nov 30, 2024",
                Trainer = "Prof. Robert Kim",
                Duration = "6 hours",
                Grade = "B+",
                CertificateNumber = "HRDC-2024-LD-003",
                Category = "Management",
                Skills = new List<string> { "Leadership", "Decision Making", "Team Management" },
                ValidUntil = "Nov 30, 2027"
            },
            new Certificate
            {
                Id = 4,
                TrainingTitle = "Effective Communication Skills",
                IssueDate = "Nov 15, 2024",
                CompletionDate = "Nov 15, 2024",
                Trainer = "Ms. Jennifer Lopez",
                Duration = "4 hours",
                Grade = "A",
                CertificateNumber = "HRDC-2024-EC-004",
                Category = "Soft Skills",
                Skills = new List<string> { "Public Speaking", "Listening", "Negotiation" },
                ValidUntil = "Nov 15, 2027"
            }
        };

        public List<Certificate> GetCertificates()
        {
            return certificates;
        }

        public Certificate? GetCertificateById(int id)
        {
            return certificates.FirstOrDefault(c => c.Id == id);
        }
    }
}


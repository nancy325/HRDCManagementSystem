using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Services
{
    public interface IParticipantService
    {
            IEnumerable<Participants> GetAll();
            IEnumerable<Participants> GetUpcomingTrainings();
            IEnumerable<string> GetNotifications();
            (int total, int completed, int inProgress, int certificates) GetStats();
        }
    }


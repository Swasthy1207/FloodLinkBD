using FloodLink.Models;

namespace FloodLink.Models
{
    public class VolunteerActivityViewModel
    {
        public VolunteerProfile Volunteer { get; set; } = null!;

        public List<HelpRequest> AssignedRequests { get; set; }
            = new();

        public Dictionary<int, List<HelpRequestStatusLog>> StatusLogs
        { get; set; }
            = new();
    }
}
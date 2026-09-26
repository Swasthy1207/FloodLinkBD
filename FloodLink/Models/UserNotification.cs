using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.15 — User Notification System
    /// Alerts users for important activities: Request Accepted, Volunteer Assigned,
    /// Request Status Updated, Emergency Announcement, New Task Assignment.
    /// </summary>
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1500)]
        public string Message { get; set; } = string.Empty;

        // RequestAccepted | VolunteerAssigned | StatusUpdated | EmergencyAnnouncement | TaskAssigned | General
        [MaxLength(60)]
        public string NotificationType { get; set; } = "General";

        [MaxLength(500)]
        public string? TargetUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

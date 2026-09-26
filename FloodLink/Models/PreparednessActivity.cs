using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.11 — Preparedness Module
    /// Pre-flood season preparedness activities: Volunteer Training, Emergency Drills,
    /// Awareness Campaigns, Meeting Schedules, Resource Preparation, Shelter Updates.
    /// </summary>
    public class PreparednessActivity
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Activity title is required.")]
        [MaxLength(200)]
        [Display(Name = "Activity Title")]
        public string Title { get; set; } = string.Empty;

        // Volunteer Training | Emergency Drill | Awareness Campaign | Meeting Schedule | Resource Preparation | Shelter Update
        [Required]
        [MaxLength(80)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Volunteer Training";

        [Required(ErrorMessage = "Description / Objectives are required.")]
        [MaxLength(3000)]
        [Display(Name = "Description & Objectives")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime ScheduledDate { get; set; } = DateTime.UtcNow.AddDays(3);

        [MaxLength(250)]
        [Display(Name = "Location / Venue")]
        public string? Location { get; set; }

        [Range(0, 50000)]
        [Display(Name = "Target or Attended Participants")]
        public int ParticipantCount { get; set; } = 0;

        // Planned | In Progress | Completed | Postponed | Cancelled
        [Required]
        [MaxLength(40)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Planned";

        [MaxLength(500)]
        [Display(Name = "Outcome & Notes")]
        public string? OutcomeNotes { get; set; }

        public int? OrganizationId { get; set; }
        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization? Organization { get; set; }

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

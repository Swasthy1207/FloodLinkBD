using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.10 — Emergency Announcement Module
    /// Organizations can publish important emergency notices for all users:
    /// Flood Warning, Shelter Opening, Shelter Closing, Volunteer Recruitment,
    /// Relief Distribution Schedule, Safety Instructions.
    /// </summary>
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        [Display(Name = "Notice Title")]
        public string Title { get; set; } = string.Empty;

        // Flood Warning | Shelter Opening | Shelter Closing | Volunteer Recruitment | Relief Distribution Schedule | Safety Instructions | General Alert
        [Required]
        [MaxLength(80)]
        [Display(Name = "Announcement Category")]
        public string Category { get; set; } = "Flood Warning";

        [Required(ErrorMessage = "Announcement details are required.")]
        [MaxLength(4000)]
        [Display(Name = "Announcement Details")]
        public string Content { get; set; } = string.Empty;

        // Low | Normal | High | Critical
        [Required]
        [MaxLength(30)]
        [Display(Name = "Urgency / Priority")]
        public string UrgencyLevel { get; set; } = "Normal";

        [MaxLength(200)]
        [Display(Name = "Target Region / Area (Optional)")]
        public string? TargetArea { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Expires At (Optional)")]
        public DateTime? ExpiresAt { get; set; }

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

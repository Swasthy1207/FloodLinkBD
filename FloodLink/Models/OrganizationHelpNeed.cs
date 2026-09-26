using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    public class OrganizationHelpNeed
    {
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization? Organization { get; set; }

        [Required]
        [StringLength(450)]
        public string RequestedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(RequestedByUserId))]
        public virtual ApplicationUser? RequestedByUser { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        // Volunteers, Medical, Rescue, Boat, Transport,
        // Food, Water, Shelter, Equipment, Other
        [Required]
        [StringLength(50)]
        public string HelpType { get; set; } = "Other";

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required]
        [StringLength(300)]
        public string Location { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int PeopleAffected { get; set; }

        [StringLength(200)]
        public string? ContactInformation { get; set; }

        public DateTime? NeededBy { get; set; }

        // Pending | Under Review | Accepted |
        // In Progress | Resolved | Rejected
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(1500)]
        public string? ResponseNotes { get; set; }

        [StringLength(450)]
        public string? ReviewedByUserId { get; set; }

        [ForeignKey(nameof(ReviewedByUserId))]
        public virtual ApplicationUser? ReviewedByUser { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}
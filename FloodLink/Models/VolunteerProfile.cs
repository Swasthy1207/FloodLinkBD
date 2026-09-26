using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    public class VolunteerProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int? OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        [MaxLength(255)]
        public string? Skills { get; set; } // e.g., "Medical, Rescue, Boat Driver"

        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available"; // "Available", "Busy", "Inactive"

        [MaxLength(200)]
        public string? CurrentLocation { get; set; }

        public bool IsApprovedByOrg { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

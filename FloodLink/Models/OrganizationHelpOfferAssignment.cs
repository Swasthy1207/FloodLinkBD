using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    public class OrganizationHelpOfferAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HelpOfferId { get; set; }

        [ForeignKey(nameof(HelpOfferId))]
        public OrganizationHelpOffer? HelpOffer { get; set; }

        [Required]
        public int VolunteerProfileId { get; set; }

        [ForeignKey(nameof(VolunteerProfileId))]
        public VolunteerProfile? VolunteerProfile { get; set; }

        [Required]
        [StringLength(450)]
        public string AssignedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(AssignedByUserId))]
        public ApplicationUser? AssignedByUser { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Assigned";

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
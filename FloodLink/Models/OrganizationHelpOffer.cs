using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    public class OrganizationHelpOffer
    {
        [Key]
        public int HelpOfferId { get; set; }

        // The help request/need being answered
        [Required]
        public int HelpNeedId { get; set; }

        [ForeignKey(nameof(HelpNeedId))]
        public OrganizationHelpNeed? HelpNeed { get; set; }

        // Organization that is offering help
        [Required]
        public int OfferingOrganizationId { get; set; }

        [ForeignKey(nameof(OfferingOrganizationId))]
        public Organization? OfferingOrganization { get; set; }

        // Example: Food, Water, Rescue Team, Medicine
        [Required]
        [StringLength(100)]
        public string OfferedHelpType { get; set; } = string.Empty;

        // Optional quantity/details
        [StringLength(200)]
        public string? OfferedQuantity { get; set; }

        // Message from offering organization
        [StringLength(1000)]
        public string? Message { get; set; }

        // Pending / Accepted / Rejected / Withdrawn / Completed
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }
    }
}
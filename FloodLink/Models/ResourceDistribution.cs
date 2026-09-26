using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.9 — Records of resource distribution to affected people, shelters, or areas.
    /// </summary>
    public class ResourceDistribution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ResourceId { get; set; }
        [ForeignKey(nameof(ResourceId))]
        public virtual Resource? Resource { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity distributed must be at least 1.")]
        [Display(Name = "Quantity Distributed")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Distributed to / Target destination is required.")]
        [MaxLength(200)]
        [Display(Name = "Distributed To (Area, Shelter, or Family)")]
        public string DistributedTo { get; set; } = string.Empty;

        public int? OrganizationId { get; set; }
        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization? Organization { get; set; }

        public int? DisasterOperationId { get; set; }
        [ForeignKey(nameof(DisasterOperationId))]
        public virtual DisasterOperation? DisasterOperation { get; set; }

        [MaxLength(450)]
        public string? DistributedByUserId { get; set; }
        [ForeignKey(nameof(DistributedByUserId))]
        public virtual ApplicationUser? DistributedBy { get; set; }

        [Display(Name = "Distribution Date")]
        public DateTime DistributionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        [Display(Name = "Remarks / Notes")]
        public string? Remarks { get; set; }
    }
}

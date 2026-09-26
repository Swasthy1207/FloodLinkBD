using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.12 — High-Risk Area Management
    /// Identifies villages, unions, or regions difficult to reach during floods.
    /// Stores: Area Name, Risk Level, Accessibility, Population Estimate.
    /// Helps coordinators prepare relief operations effectively.
    /// </summary>
    public class HighRiskArea
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Area / Village name is required.")]
        [MaxLength(150)]
        [Display(Name = "Area / Village Name")]
        public string AreaName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "District / Upazila")]
        public string? DistrictOrUpazila { get; set; }

        [MaxLength(100)]
        [Display(Name = "Union / Ward")]
        public string? UnionOrWard { get; set; }

        [Display(Name = "Latitude")]
        public double? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public double? Longitude { get; set; }

        // High | Medium | Low
        [Required]
        [MaxLength(30)]
        [Display(Name = "Risk Level")]
        public string RiskLevel { get; set; } = "High";

        // Easily Accessible | Difficult | Very Difficult | Boat Required | Road Inaccessible
        [Required]
        [MaxLength(60)]
        [Display(Name = "Accessibility Condition")]
        public string Accessibility { get; set; } = "Boat Required";

        [Required]
        [Range(0, 10000000, ErrorMessage = "Population estimate must be a positive number.")]
        [Display(Name = "Population Estimate")]
        public int PopulationEstimate { get; set; } = 0;

        [MaxLength(1000)]
        [Display(Name = "Hazards & Terrain Notes")]
        public string? TerrainAndHazardNotes { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

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

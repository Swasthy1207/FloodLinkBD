using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.9 — Resource and Equipment Management
    /// Tracks relief supplies (Food Packets, Drinking Water, Medicine, Blankets, etc.)
    /// and equipment (Rescue Boats, Life Jackets, Ambulances, Emergency Vehicles).
    /// </summary>
    public class Resource
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Resource/Equipment name is required.")]
        [MaxLength(150)]
        [Display(Name = "Item Name")]
        public string ResourceName { get; set; } = string.Empty;

        // "Resource" (Supplies) or "Equipment"
        [Required]
        [MaxLength(30)]
        [Display(Name = "Item Classification")]
        public string ItemClassification { get; set; } = "Resource"; 

        // Food Packets | Drinking Water | Medicine | Blankets | Baby Food | Hygiene Kits | Rescue Boats | Life Jackets | Ambulances | Emergency Vehicles | Other
        [Required]
        [MaxLength(80)]
        [Display(Name = "Category")]
        public string Category { get; set; } = "Food Packets";

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater.")]
        [Display(Name = "Quantity in Stock")]
        public int Quantity { get; set; } = 0;

        [MaxLength(30)]
        [Display(Name = "Unit of Measure")]
        public string Unit { get; set; } = "Packets"; // Packets, Liters, Units, Boxes, Kits

        // Available | In Use / Deployed | Low Stock | Depleted | Maintenance
        [MaxLength(40)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Available";

        [MaxLength(300)]
        [Display(Name = "Storage / Station Location")]
        public string? Location { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Description & Condition Notes")]
        public string? Notes { get; set; }

        public int? OrganizationId { get; set; }
        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization? Organization { get; set; }

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ResourceDistribution> Distributions { get; set; } = new List<ResourceDistribution>();
    }
}

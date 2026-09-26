using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.8 — represents an emergency shelter/evacuation center.
    /// </summary>
    public class Shelter
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Shelter name is required.")]
        [MaxLength(150)]
        [Display(Name = "Shelter Name")]
        public string ShelterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location/address is required.")]
        [MaxLength(300)]
        [Display(Name = "Location / Address")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Latitude")]
        public double? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Total capacity is required.")]
        [Range(1, 100000)]
        [Display(Name = "Total Capacity (people)")]
        public int Capacity { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Current Occupancy")]
        public int CurrentOccupancy { get; set; } = 0;

        /// <summary>Open | Full | Closed | Under Maintenance</summary>
        [MaxLength(30)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Open";

        [MaxLength(200)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        [Display(Name = "Facilities Available")]
        public string? Facilities { get; set; } // e.g., "Clean water, Medical aid, Generator"

        /// <summary>Organization managing this shelter (optional).</summary>
        public int? OrganizationId { get; set; }

        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization? Organization { get; set; }

        /// <summary>User who created this shelter record.</summary>
        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

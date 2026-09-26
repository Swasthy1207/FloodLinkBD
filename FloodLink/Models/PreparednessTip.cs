using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>Section 4.11 — Flood Preparedness Tip/Guideline.</summary>
    public class PreparednessTip
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required.")]
        [MaxLength(4000)]
        [Display(Name = "Content / Description")]
        public string Content { get; set; } = string.Empty;

        // Before Flood | During Flood | After Flood | Emergency Contacts | Evacuation
        [Required]
        [MaxLength(60)]
        [Display(Name = "Category")]
        public string Category { get; set; } = "Before Flood";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

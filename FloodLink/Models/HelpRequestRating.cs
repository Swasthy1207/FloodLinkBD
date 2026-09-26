using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Represents a 1 to 5 star rating submitted for a completed/resolved Help Request.
    /// A user can submit at most one rating per Help Request (with update support).
    /// </summary>
    public class HelpRequestRating
    {
        [Key]
        public int Id { get; set; }

        public int HelpRequestId { get; set; }

        [ForeignKey(nameof(HelpRequestId))]
        public virtual HelpRequest? HelpRequest { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int RatingValue { get; set; }

        [MaxLength(1000)]
        public string? Review { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

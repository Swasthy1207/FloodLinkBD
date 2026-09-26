using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Represents a user comment posted on a Help Request.
    /// </summary>
    public class HelpRequestComment
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

        [Required(ErrorMessage = "Comment text is required.")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 2000 characters.")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

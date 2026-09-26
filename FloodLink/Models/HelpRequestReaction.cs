using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Represents a user's reaction (Like or Dislike) on a Help Request.
    /// A user can have at most one reaction (Like or Dislike) per Help Request.
    /// </summary>
    public class HelpRequestReaction
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

        /// <summary>
        /// True = Like, False = Dislike
        /// </summary>
        public bool IsLike { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

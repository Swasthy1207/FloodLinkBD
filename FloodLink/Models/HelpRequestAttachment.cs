using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Represents an attached file or photo for a Help Request (Section 4.4 enhancement).
    /// </summary>
    public class HelpRequestAttachment
    {
        [Key]
        public int Id { get; set; }

        public int HelpRequestId { get; set; }

        [ForeignKey(nameof(HelpRequestId))]
        public virtual HelpRequest? HelpRequest { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}

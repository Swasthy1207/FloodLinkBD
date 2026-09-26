using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Section 4.7 — records every status/priority change for a HelpRequest (audit trail).
    /// </summary>
    public class HelpRequestStatusLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HelpRequestId { get; set; }

        [ForeignKey(nameof(HelpRequestId))]
        public virtual HelpRequest? HelpRequest { get; set; }

        /// <summary>Status or priority value before the change.</summary>
        [MaxLength(50)]
        public string? OldValue { get; set; }

        /// <summary>Status or priority value after the change.</summary>
        [MaxLength(50)]
        public string? NewValue { get; set; }

        /// <summary>Type of change: "Status" or "Priority"</summary>
        [MaxLength(20)]
        public string ChangeType { get; set; } = "Status";

        /// <summary>Optional note added by the coordinator when changing status.</summary>
        [MaxLength(500)]
        public string? Note { get; set; }

        /// <summary>FK to the user who made the change.</summary>
        [MaxLength(450)]
        public string? ChangedByUserId { get; set; }

        [ForeignKey(nameof(ChangedByUserId))]
        public virtual ApplicationUser? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}

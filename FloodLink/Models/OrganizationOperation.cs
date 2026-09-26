using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    public class OrganizationOperation
    {
        [Key]
        public int OrganizationOperationId { get; set; }

        // Foreign keys
        [ForeignKey("DisasterOperation")]
        public int DisasterOperationId { get; set; }
        public virtual DisasterOperation DisasterOperation { get; set; } = null!;

        [ForeignKey("Organization")]
        public int OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string ParticipationStatus { get; set; } = "Pending"; // Pending, Approved, Rejected, Active, Completed

        [StringLength(300)]
        public string? AssignedArea { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}

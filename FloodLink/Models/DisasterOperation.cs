using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    public class DisasterOperation
    {
        [Key]
        public int DisasterOperationId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 3)]
        public string OperationName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? DisasterType { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Active, Completed, Cancelled

        // Lead organization (the one that creates the operation)
        [ForeignKey("LeadOrganization")]
        public int CreatedByOrganizationId { get; set; }
        public virtual Organization LeadOrganization { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

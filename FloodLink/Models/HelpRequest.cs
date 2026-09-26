using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodLink.Models
{
    /// <summary>
    /// Represents a citizen emergency help request (Section 4.4).
    /// </summary>
    public class HelpRequest
    {
        public int Id { get; set; }

        // ── Who submitted ─────────────────────────────────────────────────
        /// <summary>FK to the ApplicationUser who submitted the request.</summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? Submitter { get; set; }

        /// <summary>Kept for backward-compatibility with existing volunteer-assignment logic.</summary>
        [StringLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        // ── Reporter fields (Section 4.5) ─────────────────────────────────
        /// <summary>The name of the affected person (if submitted by a Community Reporter on their behalf).</summary>
        [StringLength(150)]
        [Display(Name = "Affected Person's Name")]
        public string? AffectedPersonName { get; set; }

        public bool IsVerified { get; set; } = false;

        [StringLength(450)]
        public string? VerifiedById { get; set; }

        [ForeignKey(nameof(VerifiedById))]
        public virtual ApplicationUser? VerifiedBy { get; set; }

        public DateTime? VerifiedAt { get; set; }

        [StringLength(1000)]
        [Display(Name = "Verification Notes")]
        public string? VerificationNotes { get; set; }

        [StringLength(2000)]
        [Display(Name = "Emergency Info Updates")]
        public string? EmergencyNotes { get; set; }

        // ── Request details ───────────────────────────────────────────────
        [Required(ErrorMessage = "Request title is required.")]
        [StringLength(150, MinimumLength = 5,
            ErrorMessage = "Title must be between 5 and 150 characters.")]
        [Display(Name = "Request Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, MinimumLength = 20,
            ErrorMessage = "Description must be between 20 and 2000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Help category is required.")]
        [StringLength(80)]
        [Display(Name = "Help Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of affected people is required.")]
        [Range(1, 100000, ErrorMessage = "Number of affected people must be at least 1.")]
        [Display(Name = "Number of Affected People")]
        public int NumberOfPeople { get; set; } = 1;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(300, MinimumLength = 3,
            ErrorMessage = "Location must be between 3 and 300 characters.")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact information is required.")]
        [StringLength(200, MinimumLength = 5,
            ErrorMessage = "Contact information must be between 5 and 200 characters.")]
        [Display(Name = "Contact Information")]
        public string ContactInformation { get; set; } = string.Empty;

        // ── Image ─────────────────────────────────────────────────────────
        /// <summary>Relative path to the uploaded image, e.g. /uploads/helprequests/abc.jpg</summary>
        [StringLength(500)]
        [Display(Name = "Photo")]
        public string? ImagePath { get; set; }

        // ── Status / Priority ─────────────────────────────────────────────
        /// <summary>Submitted | In Progress | Resolved | Closed</summary>
        [StringLength(30)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        /// <summary>Low | Medium | High — AI urgency detection in 4.13 will update this.</summary>
        [StringLength(20)]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        // ── Volunteer assignment (preserved from existing implementation) ──
        [StringLength(256)]
        public string? VolunteerEmail { get; set; }

        [StringLength(450)]
        public string? AssignedVolunteerId { get; set; }

        [ForeignKey(nameof(AssignedVolunteerId))]
        public virtual ApplicationUser? AssignedVolunteer { get; set; }

        // ── Volunteer progress / completion (accountability) ──────────────
        /// <summary>Latest progress note added by the assigned volunteer.</summary>
        [StringLength(500)]
        [Display(Name = "Progress Update")]
        public string? VolunteerProgressNote { get; set; }

        /// <summary>UTC timestamp when the volunteer marked the request as Completed.</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>FK to the volunteer who completed this request.</summary>
        [StringLength(450)]
        public string? CompletedByVolunteerId { get; set; }

        [ForeignKey(nameof(CompletedByVolunteerId))]
        public virtual ApplicationUser? CompletedByVolunteer { get; set; }

        // ── Coordination link (extension point for 4.3 / future) ─────────
        /// <summary>Optional FK to DisasterOperation — populated when request is assigned to an operation.</summary>
        public int? DisasterOperationId { get; set; }

        [ForeignKey(nameof(DisasterOperationId))]
        public virtual DisasterOperation? DisasterOperation { get; set; }

        // ── Section 4.13 AI-Based Urgency Detection ──────────────────────
        [StringLength(20)]
        [Display(Name = "AI Suggested Urgency")]
        public string? AiSuggestedUrgency { get; set; }

        [StringLength(500)]
        [Display(Name = "AI Urgency Rationale")]
        public string? AiUrgencyRationale { get; set; }

        // ── Section 4.14 AI Duplicate Request Detection ──────────────────
        [Display(Name = "Potential Duplicate")]
        public bool IsPotentialDuplicate { get; set; } = false;

        public int? PotentialDuplicateRequestId { get; set; }

        [StringLength(500)]
        [Display(Name = "Duplicate Detection Reason")]
        public string? DuplicateDetectionReason { get; set; }

        public bool DuplicateDismissed { get; set; } = false;

        // ── Location Coordinates (Section 4.4 & Map Integration) ─────────
        [Display(Name = "Latitude")]
        public double? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public double? Longitude { get; set; }

        // ── Navigation Collections (Attachments, Reactions, Ratings, Comments) ──
        public virtual ICollection<HelpRequestAttachment> Attachments { get; set; } = new List<HelpRequestAttachment>();
        public virtual ICollection<HelpRequestReaction> Reactions { get; set; } = new List<HelpRequestReaction>();
        public virtual ICollection<HelpRequestRating> Ratings { get; set; } = new List<HelpRequestRating>();
        public virtual ICollection<HelpRequestComment> Comments { get; set; } = new List<HelpRequestComment>();
        public virtual ICollection<HelpRequestStatusLog> StatusLogs { get; set; } = new List<HelpRequestStatusLog>();

        // ── Timestamps ────────────────────────────────────────────────────
        [Display(Name = "Submitted")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
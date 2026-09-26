using System.ComponentModel.DataAnnotations;

namespace FloodLink.Models
{
    /// <summary>
    /// View-model used by the Create and Edit HelpRequest forms.
    /// Separates the IFormFile upload from the entity model.
    /// Updated to support multiple image uploads, lat/lng and attachment deletion.
    /// </summary>
    public class HelpRequestViewModel
    {
        public int Id { get; set; }

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

        [StringLength(150)]
        [Display(Name = "Affected Person's Name (if reporting for someone else)")]
        public string? AffectedPersonName { get; set; }

        // ── Map coordinates ──────────────────────────────────────────────
        [Display(Name = "Latitude")]
        public double? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public double? Longitude { get; set; }

        // ── Multiple image uploads ────────────────────────────────────────
        /// <summary>Multiple image files — optional, validated server-side.</summary>
        [Display(Name = "Photos (optional, up to 5)")]
        public List<IFormFile>? ImageFiles { get; set; }

        // ── Legacy single image (kept for backward compatibility display) ─
        /// <summary>Existing single image path shown during Edit (backward compat).</summary>
        public string? ExistingImagePath { get; set; }

        /// <summary>Existing attachments loaded during Edit page.</summary>
        public List<HelpRequestAttachment> ExistingAttachments { get; set; } = new();

        /// <summary>IDs of attachments the user wishes to delete.</summary>
        public List<int> DeleteAttachmentIds { get; set; } = new();
    }
}

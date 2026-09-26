using System.ComponentModel.DataAnnotations;

namespace FloodLink.Models
{
    /// <summary>
    /// View model for creating or editing an organization.
    /// </summary>
    public class OrganizationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Organization name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 150 characters.")]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organization type is required.")]
        [Display(Name = "Organization Type")]
        public string OrganizationType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 1000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact phone is required.")]
        [RegularExpression(@"^(?:\+?880|0)?1[3-9]\d{8}$",
            ErrorMessage = "Please enter a valid Bangladeshi phone number (e.g. 01712345678).")]
        [StringLength(20)]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(300)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service area is required.")]
        [StringLength(300, MinimumLength = 3, ErrorMessage = "Service area must be between 3 and 300 characters.")]
        [Display(Name = "Service Area")]
        public string ServiceArea { get; set; } = string.Empty;
    }
}

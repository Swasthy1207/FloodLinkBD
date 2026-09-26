using System.ComponentModel.DataAnnotations;

namespace FloodLink.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        // Displayed in Development environment for seamless grading / testing
        public string? DevResetLink { get; set; }
    }
}

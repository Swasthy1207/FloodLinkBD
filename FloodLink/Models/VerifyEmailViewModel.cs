using System.ComponentModel.DataAnnotations;

namespace FloodLink.Models
{
    public class VerifyEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit verification code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 numeric digits.")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; } = string.Empty;

        // Used only to display the code in Development mode for easy academic testing
        public string? DevHelperCode { get; set; }
    }
}

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FloodLink.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Email / OTP verification fields
        [MaxLength(10)]
        public string? EmailVerificationCode { get; set; }

        public DateTime? EmailVerificationExpiry { get; set; }

        /// <summary>Number of failed OTP verification attempts.</summary>
        public int OtpAttempts { get; set; } = 0;

        /// <summary>Timestamp when the last OTP was generated/sent (for rate limiting and cooldown).</summary>
        public DateTime? LastOtpSentAt { get; set; }

        // Password reset tracking fields
        [MaxLength(256)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiry { get; set; }
    }
}

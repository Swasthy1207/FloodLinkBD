namespace FloodLink.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
        /// <summary>Send a 6-digit OTP for email verification.</summary>
        Task<bool> SendEmailVerificationOtpAsync(string toEmail, string fullName, string otpCode, int expiryMinutes = 15);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);
    }
}

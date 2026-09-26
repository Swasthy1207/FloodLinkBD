using System.Net;
using System.Net.Mail;

namespace FloodLink.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // Read configuration from appsettings or environment variables
            var smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") 
                             ?? _configuration["EmailSettings:SmtpServer"];

            var smtpPortStr = Environment.GetEnvironmentVariable("SMTP_PORT") 
                              ?? _configuration["EmailSettings:SmtpPort"];
            int.TryParse(smtpPortStr, out int smtpPort);
            if (smtpPort <= 0) smtpPort = 587;

            var senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") 
                              ?? _configuration["EmailSettings:SenderEmail"] 
                              ?? "no-reply@floodlinkbd.org";

            var senderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") 
                             ?? _configuration["EmailSettings:SenderName"] 
                             ?? "FloodLink BD";

            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME") 
                           ?? _configuration["EmailSettings:Username"];

            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") 
                           ?? _configuration["EmailSettings:Password"];

            var sslStr = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") 
                         ?? _configuration["EmailSettings:EnableSsl"];
            bool enableSsl = true;
            if (bool.TryParse(sslStr, out bool parsedSsl)) enableSsl = parsedSsl;

            // If no SMTP host is configured, simulate sending safely (esp. in Development / test environments)
            if (string.IsNullOrWhiteSpace(smtpServer))
            {
                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation("[Email Simulated] To: {To}, Subject: {Subject}", toEmail, subject);
                }
                return true;
            }

            try
            {
                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 10000
                };

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email successfully sent to {ToEmail} with subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEmailVerificationOtpAsync(string toEmail, string fullName, string otpCode, int expiryMinutes = 15)
        {
            var subject = "FloodLink BD - Email Verification Code";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 20px; }}
    .container {{ max-width: 560px; margin: 0 auto; background: #ffffff; border-radius: 8px; border: 1px solid #e2e8f0; padding: 30px; }}
    .header {{ text-align: center; border-bottom: 2px solid #087f8c; padding-bottom: 15px; margin-bottom: 20px; }}
    .title {{ color: #087f8c; font-size: 24px; font-weight: bold; margin: 0; }}
    .subtitle {{ color: #64748b; font-size: 14px; margin-top: 5px; }}
    .otp-box {{ background-color: #f1f5f9; border: 2px dashed #087f8c; border-radius: 8px; text-align: center; padding: 18px; margin: 25px 0; }}
    .otp-code {{ font-size: 34px; font-weight: bold; letter-spacing: 8px; color: #087f8c; font-family: monospace; }}
    .footer {{ font-size: 12px; color: #94a3b8; text-align: center; margin-top: 30px; border-top: 1px solid #f1f5f9; padding-top: 15px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1 class='title'>FloodLink Bangladesh</h1>
      <p class='subtitle'>Emergency Relief & Disaster Coordination</p>
    </div>
    <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
    <p>Thank you for registering on FloodLink. Please use the following 6-digit verification code to activate your account:</p>
    <div class='otp-box'>
      <span class='otp-code'>{otpCode}</span>
    </div>
    <p style='color: #475569; font-size: 14px;'>This code is valid for <strong>{expiryMinutes} minutes</strong>. For your security, do not share this code with anyone.</p>
    <p style='color: #475569; font-size: 14px;'>If you did not request this verification, you can safely ignore this email.</p>
    <div class='footer'>
      &copy; 2026 FloodLink BD. All rights reserved.
    </div>
  </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
        {
            var subject = "FloodLink BD - Password Reset Request";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 20px; }}
    .container {{ max-width: 560px; margin: 0 auto; background: #ffffff; border-radius: 8px; border: 1px solid #e2e8f0; padding: 30px; }}
    .header {{ text-align: center; border-bottom: 2px solid #087f8c; padding-bottom: 15px; margin-bottom: 20px; }}
    .title {{ color: #087f8c; font-size: 24px; font-weight: bold; margin: 0; }}
    .btn {{ display: inline-block; background-color: #087f8c; color: #ffffff !important; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: bold; margin: 20px 0; }}
    .footer {{ font-size: 12px; color: #94a3b8; text-align: center; margin-top: 30px; border-top: 1px solid #f1f5f9; padding-top: 15px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1 class='title'>FloodLink Bangladesh</h1>
    </div>
    <p>Hello <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
    <p>We received a request to reset the password for your FloodLink account.</p>
    <div style='text-align: center;'>
      <a href='{resetLink}' class='btn'>Reset Password</a>
    </div>
    <p style='color: #475569; font-size: 13px;'>If the button above does not work, copy and paste this link into your browser:</p>
    <p style='word-break: break-all; font-size: 12px; color: #087f8c;'>{resetLink}</p>
    <p style='color: #475569; font-size: 13px;'>This link is valid for 2 hours. If you did not request a password reset, please ignore this email.</p>
    <div class='footer'>
      &copy; 2026 FloodLink BD. All rights reserved.
    </div>
  </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}

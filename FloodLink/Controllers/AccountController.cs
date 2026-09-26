using FloodLink.Data;
using FloodLink.Models;
using FloodLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace FloodLink.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        private static readonly string[] AllowedRegistrationRoles =
        {
            "Citizen",
            "Volunteer",
            "Community Reporter",
            "Coordinator",
            "Organization Admin"
        };

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger,
            IWebHostEnvironment environment,
            IEmailService emailService,
            ApplicationDbContext context)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _roleManager   = roleManager;
            _logger        = logger;
            _environment   = environment;
            _emailService  = emailService;
            _context       = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View(model);
            }

            if (!AllowedRegistrationRoles.Contains(model.Role) && model.Role != "Super Admin")
            {
                ModelState.AddModelError("Role", "Invalid role selected.");
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            string otpCode = GenerateNumericOtp(6);

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = model.Phone.Trim(),
                FullName = model.FullName.Trim(),
                Status = "PendingVerification",
                EmailConfirmed = false,
                EmailVerificationCode = otpCode,
                EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                await _userManager.AddClaimAsync(user, new Claim("FullName", user.FullName));

                // Send OTP via email
                try
                {
                    await _emailService.SendEmailVerificationOtpAsync(user.Email!, user.FullName, otpCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not send OTP email for {Email}. Code: [{Otp}]", user.Email, otpCode);
                }

                _logger.LogInformation("Verification OTP for {Email} is [{Otp}] (expires 15 mins).", user.Email, otpCode);

                TempData["SuccessMessage"] = "Account created! A 6-digit verification code has been sent to your email.";
                return RedirectToAction("VerifyEmail", new { email = user.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

       

        [HttpGet]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { available = false, message = "Email is required." });
            }

            var trimmedEmail = email.Trim();
            var user = await _userManager.FindByEmailAsync(trimmedEmail);

            if (user != null)
            {
                return Json(new { available = false, message = "Email is already registered." });
            }

            return Json(new { available = true, message = "Email is available." });
        }

        
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            if (user.EmailConfirmed && user.Status == "Active")
            {
                TempData["SuccessMessage"] = "Your email is already verified. Please login.";
                return RedirectToAction("Login");
            }

            var model = new VerifyEmailViewModel
            {
                Email = user.Email ?? email,
                DevHelperCode = _environment.IsDevelopment() ? user.EmailVerificationCode : null
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Account not found.");
                return View(model);
            }

            if (user.EmailConfirmed && user.Status == "Active")
            {
                TempData["SuccessMessage"] = "Your account is already verified. You can log in.";
                return RedirectToAction("Login");
            }

            if (user.EmailVerificationExpiry == null || user.EmailVerificationExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("VerificationCode", "Verification code has expired. Please click 'Resend Code'.");
                if (_environment.IsDevelopment())
                {
                    model.DevHelperCode = user.EmailVerificationCode;
                }
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.EmailVerificationCode) ||
                !string.Equals(user.EmailVerificationCode.Trim(), model.VerificationCode.Trim(), StringComparison.Ordinal))
            {
                ModelState.AddModelError("VerificationCode", "Invalid verification code. Please check and try again.");
                if (_environment.IsDevelopment())
                {
                    model.DevHelperCode = user.EmailVerificationCode;
                }
                return View(model);
            }

            // Mark user verified and active
            user.EmailConfirmed = true;
            user.Status = "Active";
            user.EmailVerificationCode = null;
            user.EmailVerificationExpiry = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to update account status. Please try again.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Email verified successfully! Your account is now active. Please login.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationCode(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && (!user.EmailConfirmed || user.Status == "PendingVerification"))
            {
                // Rate limiting: max 5 resends, cooldown 60 s between requests
                const int maxAttempts = 5;
                const int cooldownSeconds = 60;

                if (user.OtpAttempts >= maxAttempts)
                {
                    TempData["ErrorMessage"] = $"Too many resend attempts. Please wait before trying again.";
                    return RedirectToAction("VerifyEmail", new { email });
                }

                if (user.LastOtpSentAt.HasValue &&
                    (DateTime.UtcNow - user.LastOtpSentAt.Value).TotalSeconds < cooldownSeconds)
                {
                    int remaining = cooldownSeconds - (int)(DateTime.UtcNow - user.LastOtpSentAt.Value).TotalSeconds;
                    TempData["ErrorMessage"] = $"Please wait {remaining} seconds before requesting a new code.";
                    return RedirectToAction("VerifyEmail", new { email });
                }

                string newOtp = GenerateNumericOtp(6);
                user.EmailVerificationCode   = newOtp;
                user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15);
                user.OtpAttempts             = user.OtpAttempts + 1;
                user.LastOtpSentAt           = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                try
                {
                    await _emailService.SendEmailVerificationOtpAsync(user.Email!, user.FullName, newOtp);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resend OTP email for {Email}.", user.Email);
                }

                _logger.LogInformation("Resent OTP for {Email} is [{Otp}].", user.Email, newOtp);
                TempData["SuccessMessage"] = "A new verification code has been sent to your email.";
            }

            return RedirectToAction("VerifyEmail", new { email });
        }

        // ==========================================
        // 4. LOGIN
        // ==========================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Generic error message without revealing whether email or password specifically was incorrect
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Check if user is pending email verification
            if (!user.EmailConfirmed || user.Status == "PendingVerification")
            {
                ModelState.AddModelError("", "Your email is not verified yet. Please verify your account to continue.");
                ViewBag.UnverifiedEmail = user.Email;
                return View(model);
            }

            // Check if user account is deactivated
            if (user.Status == "Inactive")
            {
                ModelState.AddModelError("", "Your account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully.", user.Email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect to the appropriate role-based dashboard
                return await RedirectToDashboardAsync(user);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out.", user.Email);
                ModelState.AddModelError("", "This account has been locked due to multiple failed login attempts. Please try again later.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ==========================================
        // 5. ROLE-BASED REDIRECTION HELPER
        // ==========================================

        private async Task<IActionResult> RedirectToDashboardAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Super Admin") || roles.Contains("Admin"))
            {
                return RedirectToAction("SuperAdmin", "Dashboard");
            }
            if (roles.Contains("Organization Admin") || roles.Contains("Organization"))
            {
                return RedirectToAction("Organization", "Dashboard");
            }
            if (roles.Contains("Coordinator"))
            {
                return RedirectToAction("Coordinator", "Dashboard");
            }
            if (roles.Contains("Volunteer"))
            {
                return RedirectToAction("Volunteer", "Dashboard");
            }
            if (roles.Contains("Community Reporter"))
            {
                return RedirectToAction("CommunityReporter", "Dashboard");
            }

            // Default to Citizen dashboard
            return RedirectToAction("Citizen", "Dashboard");
        }

        // ==========================================
        // 6. LOGOUT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 7. PASSWORD RESET (FORGOT & RESET)
        // ==========================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Generic feedback to prevent email enumeration
            ViewBag.Message = "If an account with that email exists, password reset instructions have been sent.";

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
                await _userManager.UpdateAsync(user);

                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token, email = user.Email },
                    Request.Scheme)!;

                _logger.LogInformation("Password reset for {Email}. Link: {Link}", user.Email, resetLink);

                // Send reset email (non-blocking)
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName, resetLink);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not send password-reset email for {Email}.", user.Email);
                }

                if (_environment.IsDevelopment())
                    model.DevResetLink = resetLink;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset request.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                TempData["SuccessMessage"] = "Password has been reset successfully. Please log in.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Your password has been reset successfully! You can now log in with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ==========================================
        // 8. ACCESS DENIED
        // ==========================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // 9. SUPER ADMIN USER & ROLES MANAGEMENT
        // ==========================================

        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> Users(string? search, string? role, string? status, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u => u.UserName!.Contains(search) ||
                                         u.Email!.Contains(search) ||
                                         u.FullName.Contains(search) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status == status);
            }

            // Role filtering
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleObj = await _roleManager.FindByNameAsync(role);
                if (roleObj != null)
                {
                    var userIdsInRole = await _context.UserRoles
                        .Where(ur => ur.RoleId == roleObj.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var pagedUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Organizations managed by users
            var managedOrgs = await _context.Organizations
                .Where(o => o.ManagedByUserId != null)
                .ToDictionaryAsync(o => o.ManagedByUserId!, o => o.OrganizationName);

            var userViewModels = new List<UserManagementViewModel>();
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                managedOrgs.TryGetValue(user.Id, out var orgName);

                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? user.Email ?? "",
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    Roles = roles,
                    OrganizationName = orgName
                });
            }

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();

            return View(userViewModels);
        }

        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.ManagedByUserId == user.Id);
            var helpReqCount = await _context.HelpRequests.CountAsync(r => r.UserId == user.Id);

            var vm = new UserDetailsViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Email ?? "",
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                OrganizationName = org?.OrganizationName,
                OrganizationId = org?.Id,
                HelpRequestsCount = helpReqCount,
                LastOtpSentAt = user.LastOtpSentAt
            };

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Status = user.Status,
                SelectedRoles = roles.ToList(),
                AllRoles = allRoles
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Super Admin,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            if (!ModelState.IsValid)
            {
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
                return View(model);
            }

            user.FullName = model.FullName?.Trim() ?? user.FullName;
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Status = model.Status ?? "Active";

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
                return View(model);
            }

            // Update roles safely
            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.SelectedRoles ?? new List<string>();

            // Protection: prevent removing Super Admin role from default superadmin@floodlink.org
            if (user.Email == "superadmin@floodlink.org" && !selectedRoles.Contains("Super Admin"))
            {
                selectedRoles.Add("Super Admin");
            }

            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }
            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            TempData["SuccessMessage"] = $"User {user.FullName} ({user.Email}) updated successfully.";
            return RedirectToAction(nameof(UserDetails), new { id = user.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Super Admin,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Protection: cannot deactivate default super admin or self
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId || user.Email == "superadmin@floodlink.org")
            {
                TempData["ErrorMessage"] = "Cannot change status of the primary or currently active Super Admin account.";
                return RedirectToAction(nameof(Users));
            }

            user.Status = user.Status == "Active" ? "Inactive" : "Active";
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"User status for {user.Email} changed to {user.Status}.";
            return RedirectToAction(nameof(Users));
        }

        // ==========================================
        // UTILITIES
        // ==========================================

        private static string GenerateNumericOtp(int length)
        {
            const string digits = "0123456789";
            var result = new char[length];
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            for (int i = 0; i < length; i++)
            {
                result[i] = digits[bytes[i] % digits.Length];
            }

            return new string(result);
        }
    }
}
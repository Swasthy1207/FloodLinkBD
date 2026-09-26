using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class HelpRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public static readonly string[] ValidCategories =
        {
            "Food",
            "Drinking Water",
            "Medicine",
            "Rescue",
            "Temporary Shelter",
            "Baby Supplies",
            "Elderly Support",
            "Hygiene Supplies"
        };

        private static readonly string[] ValidStatuses =
        {
            "Submitted", "In Progress", "Resolved", "Closed"
        };

        private static readonly string[] AllowedMimeTypes =
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp"
        };

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private const long MaxImageBytes = 5 * 1024 * 1024; 

        private readonly FloodLink.Services.IAiUrgencyDetectionService _aiUrgencyService;
        private readonly FloodLink.Services.IAiDuplicateDetectionService _aiDuplicateService;
        private readonly FloodLink.Services.INotificationService _notificationService;

        public HelpRequestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            FloodLink.Services.IAiUrgencyDetectionService aiUrgencyService,
            FloodLink.Services.IAiDuplicateDetectionService aiDuplicateService,
            FloodLink.Services.INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _aiUrgencyService = aiUrgencyService;
            _aiDuplicateService = aiDuplicateService;
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize(Roles = "Citizen,Super Admin,Admin,Community Reporter")]
        public IActionResult Create()
        {
            ViewBag.Categories = ValidCategories;
            return View(new HelpRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Citizen,Super Admin,Admin,Community Reporter")]
        public async Task<IActionResult> Create(HelpRequestViewModel model)
        {
            ViewBag.Categories = ValidCategories;

            if (!string.IsNullOrEmpty(model.Category) && !ValidCategories.Contains(model.Category))
            {
                ModelState.AddModelError("Category", "Invalid category selected.");
            }

            var validFiles = new List<IFormFile>();
            if (model.ImageFiles != null)
            {
                if (model.ImageFiles.Count > 5)
                {
                    ModelState.AddModelError("ImageFiles", "You can upload a maximum of 5 images.");
                }
                else
                {
                    foreach (var file in model.ImageFiles.Where(f => f != null && f.Length > 0))
                    {
                        var err = ValidateImage(file);
                        if (err != null)
                        {
                            ModelState.AddModelError("ImageFiles", $"{file.FileName}: {err}");
                        }
                        else
                        {
                            validFiles.Add(file);
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            // Section 4.13: AI-Based Urgency Detection
            var urgencyAnalysis = _aiUrgencyService.AnalyzeUrgency(
                model.Title, model.Description, model.NumberOfPeople, model.Category);

            var helpRequest = new HelpRequest
            {
                UserId              = userId,
                UserEmail           = User.Identity?.Name ?? "",
                Title               = model.Title.Trim(),
                Description         = model.Description.Trim(),
                Category            = model.Category,
                NumberOfPeople      = model.NumberOfPeople,
                Location            = model.Location.Trim(),
                ContactInformation  = model.ContactInformation.Trim(),
                Status              = "Submitted",
                Priority            = urgencyAnalysis.SuggestedUrgency,
                AiSuggestedUrgency  = urgencyAnalysis.SuggestedUrgency,
                AiUrgencyRationale  = urgencyAnalysis.Rationale,
                Latitude            = model.Latitude,
                Longitude           = model.Longitude,
                CreatedAt           = DateTime.UtcNow,
                AffectedPersonName  = User.IsInRole("Community Reporter") ? model.AffectedPersonName?.Trim() : null
            };

            // Section 4.14: AI Duplicate Detection
            var dupCheck = await _aiDuplicateService.CheckForDuplicateAsync(helpRequest);
            if (dupCheck.IsDuplicate)
            {
                helpRequest.IsPotentialDuplicate = true;
                helpRequest.PotentialDuplicateRequestId = dupCheck.MatchedRequestId;
                helpRequest.DuplicateDetectionReason = dupCheck.Reason;
            }

            _context.HelpRequests.Add(helpRequest);
            await _context.SaveChangesAsync();

            // Save uploaded attachments
            foreach (var file in validFiles)
            {
                var path = await SaveImageAsync(file);
                _context.HelpRequestAttachments.Add(new HelpRequestAttachment
                {
                    HelpRequestId = helpRequest.Id,
                    FilePath      = path,
                    FileName      = Path.GetFileName(file.FileName),
                    ContentType   = file.ContentType,
                    FileSize      = file.Length,
                    UploadedAt    = DateTime.UtcNow
                });
                // Also keep backward-compat single image for first file
                if (helpRequest.ImagePath == null)
                    helpRequest.ImagePath = path;
            }
            if (validFiles.Count > 0)
                await _context.SaveChangesAsync();

            // Section 4.15 Notifications
            string notifTitle = dupCheck.IsDuplicate
                ? $"⚠️ [Possible Duplicate] New {urgencyAnalysis.SuggestedUrgency} Priority Request: {helpRequest.Title}"
                : $"🚨 New {urgencyAnalysis.SuggestedUrgency} Priority Request: {helpRequest.Title}";
            string notifMsg = $"Request submitted in {helpRequest.Location} for {helpRequest.NumberOfPeople} people. AI Urgency: {urgencyAnalysis.SuggestedUrgency}.";
            if (dupCheck.IsDuplicate) notifMsg += $" Alert: {dupCheck.Reason}";

            await _notificationService.NotifyRoleAsync("Coordinator", notifTitle, notifMsg, "NewRequest", $"/HelpRequest/Details/{helpRequest.Id}");
            await _notificationService.NotifyRoleAsync("Admin", notifTitle, notifMsg, "NewRequest", $"/HelpRequest/Details/{helpRequest.Id}");

            TempData["SuccessMessage"] = "Your help request has been submitted successfully.";
            if (User.IsInRole("Community Reporter"))
                return RedirectToAction(nameof(ReporterDashboard));
            return RedirectToAction(nameof(MyRequests));
        }

        // ================================================================
        // REPORTER DASHBOARD (Section 4.5)
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Community Reporter")]
        public async Task<IActionResult> ReporterDashboard(
            string? search,
            string? category,
            string? status,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 6;
            var userId = _userManager.GetUserId(User);

            var query = _context.HelpRequests
                .Where(r => r.UserId == userId || r.UserEmail == User.Identity!.Name)
                .AsQueryable();

            ApplySearchFilterSort(ref query, search, category, status, sortOrder);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            SetViewBagPaging(page, totalPages, totalCount, search, category, status, sortOrder);
            ViewBag.Categories = ValidCategories;
            return View(requests);
        }

        // ================================================================
        // MY REQUESTS — search, filter, sort, paginate
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> MyRequests(
            string? search,
            string? category,
            string? status,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 6;

            var userId = _userManager.GetUserId(User);

            var query = _context.HelpRequests
                .Where(r => r.UserId == userId || r.UserEmail == User.Identity!.Name)
                .AsQueryable();

            ApplySearchFilterSort(ref query, search, category, status, sortOrder);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            SetViewBagPaging(page, totalPages, totalCount, search, category, status, sortOrder);
            ViewBag.Categories = ValidCategories;
            return View(requests);
        }

        // ================================================================
        // TRACK REQUESTS — citizen status tracking
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> TrackRequests()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var requests = await _context.HelpRequests
                .Where(r => r.UserId == userId)
                .Include(r => r.StatusLogs)
                    .ThenInclude(l => l.ChangedBy)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // ================================================================
        // DETAILS
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.HelpRequests
                .Include(r => r.Submitter)
                .Include(r => r.DisasterOperation)
                .Include(r => r.Attachments)
                .Include(r => r.Reactions)
                .Include(r => r.Ratings)
                .Include(r => r.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(MyRequests));
            }

            var userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin") || User.IsInRole("Coordinator") || User.IsInRole("Organization Admin") || User.IsInRole("Organization");
            bool isOwner = request.UserId == userId || request.UserEmail == User.Identity?.Name;
            bool isVolunteerAssigned = request.VolunteerEmail == User.Identity?.Name;

            if (!isAdmin && !isOwner && !isVolunteerAssigned)
                return Forbid();

            // ================================================================
            // STATUS / TRACKING HISTORY
            // ================================================================
            var statusLogs = await _context.HelpRequestStatusLogs
                .Include(l => l.ChangedBy)
                .Where(l => l.HelpRequestId == id)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();


            // ================================================================
            // AVAILABLE VOLUNTEERS FOR ASSIGNMENT
            // ================================================================
            if (isAdmin)
            {
                IQueryable<VolunteerProfile> volunteerQuery =
                    _context.VolunteerProfiles
                        .Include(v => v.User)
                        .Include(v => v.Organization)
                        .Where(v =>
                            v.IsApprovedByOrg &&
                            v.AvailabilityStatus == "Available");

                // Organization Admin -> own organization volunteers only
                if (User.IsInRole("Organization Admin") ||
                    User.IsInRole("Organization"))
                {
                    var myOrg = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

                    if (myOrg != null)
                    {
                        volunteerQuery = volunteerQuery
                            .Where(v => v.OrganizationId == myOrg.Id);
                    }
                    else
                    {
                        volunteerQuery = volunteerQuery.Where(v => false);
                    }
                }

                ViewBag.AvailableVolunteers =
                    await volunteerQuery
                        .OrderBy(v => v.User!.FullName)
                        .ToListAsync();
            }


            // ================================================================
            // CURRENT USER'S REACTION / RATING
            // ================================================================

            // Current user's existing reaction/rating
            var myReaction = userId != null
                ? request.Reactions.FirstOrDefault(r => r.UserId == userId)
                : null;
            var myRating = userId != null
                ? request.Ratings.FirstOrDefault(r => r.UserId == userId)
                : null;

            ViewBag.IsAdmin      = isAdmin;
            ViewBag.IsOwner      = isOwner;
            ViewBag.IsReporter   = User.IsInRole("Community Reporter") && isOwner;
            ViewBag.ValidStatuses = ValidStatuses;
            ViewBag.StatusLogs   = statusLogs;
            ViewBag.LikeCount    = request.Reactions.Count(r => r.IsLike);
            ViewBag.DislikeCount = request.Reactions.Count(r => !r.IsLike);
            ViewBag.MyReaction   = myReaction?.IsLike; // null = no reaction
            ViewBag.AvgRating    = request.Ratings.Count > 0
                ? request.Ratings.Average(r => r.RatingValue)
                : (double?)null;
            ViewBag.RatingCount  = request.Ratings.Count;
            ViewBag.MyRating     = myRating?.RatingValue;
            ViewBag.MyRatingId   = myRating?.Id;
            return View(request);
        }



        // ================================================================
        // EDIT — GET
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var request = await _context.HelpRequests
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(MyRequests));
            }

            if (!CanEdit(request))
                return Forbid();

            ViewBag.Categories = ValidCategories;

            var vm = new HelpRequestViewModel
            {
                Id                  = request.Id,
                Title               = request.Title,
                Description         = request.Description,
                Category            = request.Category,
                NumberOfPeople      = request.NumberOfPeople,
                Location            = request.Location,
                ContactInformation  = request.ContactInformation,
                ExistingImagePath   = request.ImagePath,
                AffectedPersonName  = request.AffectedPersonName,
                Latitude            = request.Latitude,
                Longitude           = request.Longitude,
                ExistingAttachments = request.Attachments.ToList()
            };

            return View(vm);
        }

        // ================================================================
        // EDIT — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HelpRequestViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var request = await _context.HelpRequests
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(MyRequests));
            }

            if (!CanEdit(request))
                return Forbid();

            ViewBag.Categories = ValidCategories;

            // Server-side category validation
            if (!string.IsNullOrEmpty(model.Category) && !ValidCategories.Contains(model.Category))
                ModelState.AddModelError("Category", "Invalid category selected.");

            // Validate new uploads
            var validFiles = new List<IFormFile>();
            if (model.ImageFiles != null)
            {
                int existing = request.Attachments.Count;
                int toDelete = model.DeleteAttachmentIds?.Count ?? 0;
                int newCount = model.ImageFiles.Count(f => f != null && f.Length > 0);

                if (existing - toDelete + newCount > 5)
                    ModelState.AddModelError("ImageFiles", "Total attachments cannot exceed 5.");
                else
                {
                    foreach (var file in model.ImageFiles.Where(f => f != null && f.Length > 0))
                    {
                        var err = ValidateImage(file);
                        if (err != null) ModelState.AddModelError("ImageFiles", $"{file.FileName}: {err}");
                        else validFiles.Add(file);
                    }
                }
            }

            model.ExistingImagePath = request.ImagePath;
            model.ExistingAttachments = request.Attachments.ToList();

            if (!ModelState.IsValid)
                return View(model);

            // Delete requested attachments (owner or admin only)
            if (model.DeleteAttachmentIds?.Count > 0)
            {
                var toDelete = request.Attachments
                    .Where(a => model.DeleteAttachmentIds.Contains(a.Id))
                    .ToList();
                foreach (var att in toDelete)
                {
                    DeleteImageFile(att.FilePath);
                    _context.HelpRequestAttachments.Remove(att);
                }
            }

            // Save new attachments
            foreach (var file in validFiles)
            {
                var path = await SaveImageAsync(file);
                _context.HelpRequestAttachments.Add(new HelpRequestAttachment
                {
                    HelpRequestId = request.Id,
                    FilePath      = path,
                    FileName      = Path.GetFileName(file.FileName),
                    ContentType   = file.ContentType,
                    FileSize      = file.Length,
                    UploadedAt    = DateTime.UtcNow
                });
            }

            request.Title              = model.Title.Trim();
            request.Description        = model.Description.Trim();
            request.Category           = model.Category;
            request.NumberOfPeople     = model.NumberOfPeople;
            request.Location           = model.Location.Trim();
            request.ContactInformation = model.ContactInformation.Trim();
            request.Latitude           = model.Latitude;
            request.Longitude          = model.Longitude;
            request.UpdatedAt          = DateTime.UtcNow;

            if (User.IsInRole("Community Reporter"))
                request.AffectedPersonName = model.AffectedPersonName?.Trim();

            // Keep backward-compat ImagePath pointing to first remaining attachment
            await _context.SaveChangesAsync();
            var firstAtt = await _context.HelpRequestAttachments
                .Where(a => a.HelpRequestId == request.Id)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync();
            request.ImagePath = firstAtt?.FilePath;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Help request updated successfully.";
            if (User.IsInRole("Community Reporter"))
                return RedirectToAction(nameof(ReporterDashboard));
            return RedirectToAction(nameof(MyRequests));
        }

        // ================================================================
        // DELETE — GET
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(MyRequests));
            }

            if (!CanDelete(request))
                return Forbid();

            return View(request);
        }

        // ================================================================
        // DELETE — POST
        // ================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.HelpRequests
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(MyRequests));
            }

            if (!CanDelete(request))
                return Forbid();

            // Delete all physical attachment files
            foreach (var att in request.Attachments)
                DeleteImageFile(att.FilePath);
            DeleteImageFile(request.ImagePath); // backward compat

            _context.HelpRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Help request deleted successfully.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ================================================================
        // ALL REQUESTS — Coordinator / Admin view
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> AllRequests(
            string? search,
            string? category,
            string? status,
            string? priority,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 10;

            var query = _context.HelpRequests.AsQueryable();

            ApplySearchFilterSort(ref query, search, category, status, sortOrder);

            if (!string.IsNullOrWhiteSpace(priority))
                query = query.Where(r => r.Priority == priority);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            SetViewBagPaging(page, totalPages, totalCount, search, category, status, sortOrder);
            ViewBag.Priority   = priority;
            ViewBag.Categories = ValidCategories;
            return View(requests);
        }

        // ================================================================
        // UPDATE STATUS — Coordinator / Admin POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl)
        {
            if (!ValidStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(AllRequests));
            }

            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(AllRequests));
            }

            if (request.Status == "Completed" ||
     request.Status == "Resolved" ||
     request.Status == "Closed")
            {
                TempData["ErrorMessage"] =
                    "This request is already completed or closed and cannot be updated.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var oldStatus = request.Status;
            request.Status    = status;
            request.UpdatedAt = DateTime.UtcNow;

            // Section 4.7: log the status change
            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId   = id,
                ChangeType      = "Status",
                OldValue        = oldStatus,
                NewValue        = status,
                ChangedByUserId = _userManager.GetUserId(User),
                ChangedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Section 4.15: Send Notification to requester
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    $"Request Status Updated: {request.Title}",
                    $"Your request status has been updated from '{oldStatus}' to '{status}'.",
                    "StatusUpdated",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] = $"Request status updated to \"{status}\".";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // UPDATE PRIORITY — Coordinator / Admin POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> UpdatePriority(int id, string priority)
        {
            string[] valid = { "Low", "Medium", "High" };
            if (!valid.Contains(priority))
            {
                TempData["ErrorMessage"] = "Invalid priority.";
                return RedirectToAction(nameof(AllRequests));
            }

            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(AllRequests));
            }

            if (request.Status == "Completed" ||
    request.Status == "Resolved" ||
    request.Status == "Closed")
            {
                TempData["ErrorMessage"] =
                    "Completed or closed requests cannot be updated.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var oldPriority = request.Priority;
            request.Priority  = priority;
            request.UpdatedAt = DateTime.UtcNow;

            // Section 4.7: log the priority change
            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId   = id,
                ChangeType      = "Priority",
                OldValue        = oldPriority,
                NewValue        = priority,
                ChangedByUserId = _userManager.GetUserId(User),
                ChangedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Priority updated to \"{priority}\".";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // VERIFY REQUEST — Reporter / Admin POST
        // ================================================================
        // ================================================================
        // VERIFY REQUEST — Reporter / Admin / Organization Admin POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Community Reporter,Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> VerifyRequest(int id, string? verificationNotes)
        {
            var request = await _context.HelpRequests.FindAsync(id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(AllRequests));
            }

            if (request.Status == "Completed" ||
    request.Status == "Resolved" ||
    request.Status == "Closed")
            {
                TempData["ErrorMessage"] =
                    "Completed or closed requests cannot be updated.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = _userManager.GetUserId(User);

            bool isReporter = User.IsInRole("Community Reporter");

            bool isAdmin =
                User.IsInRole("Super Admin") ||
                User.IsInRole("Admin") ||
                User.IsInRole("Coordinator") ||
                User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization");

            bool isOwner =
                request.UserId == userId ||
                request.UserEmail == User.Identity?.Name;

            // Community Reporter can only verify their own report.
            // Admin/Coordinator/Organization Admin can verify requests.
            if (isReporter && !isOwner && !isAdmin)
            {
                return Forbid();
            }

            request.IsVerified = true;
            request.VerifiedById = userId;
            request.VerifiedAt = DateTime.UtcNow;
            request.VerificationNotes = verificationNotes?.Trim();
            request.UpdatedAt = DateTime.UtcNow;

            // Add verification to request history.
            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId = id,
                ChangeType = "Verification",
                OldValue = "Unverified",
                NewValue = "Verified",
                Note = verificationNotes?.Trim(),
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify the citizen/reporter who submitted the request.
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    $"Request Verified: {request.Title}",
                    "Your help request has been verified by the response team.",
                    "RequestVerified",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] = "Request has been successfully verified.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // UPDATE EMERGENCY INFO — Reporter / Admin POST
        // ================================================================
        // ================================================================
        // UPDATE EMERGENCY INFO — Reporter / Admin / Organization Admin POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Community Reporter,Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> UpdateEmergencyInfo(
            int id,
            string? emergencyNotes)
        {
            
            var request = await _context.HelpRequests.FindAsync(id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Help request not found.";
                return RedirectToAction(nameof(AllRequests));
            }

            if (request.Status == "Completed" ||
    request.Status == "Resolved" ||
    request.Status == "Closed")
            {
                TempData["ErrorMessage"] =
                    "Completed or closed requests cannot be updated.";

                return RedirectToAction(nameof(Details), new { id });
            }


            if (string.IsNullOrWhiteSpace(emergencyNotes))
            {
                TempData["ErrorMessage"] =
                    "Please enter the latest emergency information.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = _userManager.GetUserId(User);

            bool isReporter = User.IsInRole("Community Reporter");

            bool isAdmin =
                User.IsInRole("Super Admin") ||
                User.IsInRole("Admin") ||
                User.IsInRole("Coordinator") ||
                User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization");

            bool isOwner =
                request.UserId == userId ||
                request.UserEmail == User.Identity?.Name;

            // Community Reporter can only update their own request.
            // Admin/Coordinator/Organization Admin can update emergency information.
            if (isReporter && !isOwner && !isAdmin)
            {
                return Forbid();
            }

            request.EmergencyNotes = emergencyNotes.Trim();
            request.UpdatedAt = DateTime.UtcNow;

            // Keep an audit record.
            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId = id,
                ChangeType = "Emergency Info",
                OldValue = "Updated",
                NewValue = "Updated",
                Note = emergencyNotes.Trim(),
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify requester.
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    $"Emergency Information Updated: {request.Title}",
                    "New emergency information has been added to your help request.",
                    "EmergencyInfoUpdated",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] =
                "Emergency information updated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // AVAILABLE REQUESTS — Volunteer view (preserved)
        // ================================================================
        [Authorize(Roles = "Volunteer")]
        [HttpGet]
        public async Task<IActionResult> AvailableRequests(
            string? search,
            string? category,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 8;

            var query = _context.HelpRequests
                .Where(r => r.Status == "Submitted" || r.Status == "In Progress")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.Title.Contains(search) ||
                    r.Location.Contains(search) ||
                    r.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(r => r.Category == category);

            query = sortOrder switch
            {
                "oldest"    => query.OrderBy(r => r.CreatedAt),
                "title_asc" => query.OrderBy(r => r.Title),
                "priority"  => query.OrderByDescending(r =>
                    r.Priority == "High" ? 3 : r.Priority == "Medium" ? 2 : 1),
                _           => query.OrderByDescending(r => r.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            SetViewBagPaging(page, totalPages, totalCount, search, category, null, sortOrder);
            ViewBag.Categories = ValidCategories;
            return View(requests);
        }

        // ================================================================
        // ACCEPT REQUEST — Volunteer POST (preserved)
        // ================================================================
        [Authorize(Roles = "Volunteer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            if (request.Status != "Submitted")
            {
                TempData["ErrorMessage"] = "This request is no longer available.";
                return RedirectToAction(nameof(AvailableRequests));
            }

            request.Status         = "In Progress";
            request.VolunteerEmail = User.Identity?.Name ?? "";
            request.AssignedVolunteerId = _userManager.GetUserId(User);
            request.UpdatedAt      = DateTime.UtcNow;

            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId   = id,
                ChangeType      = "Status",
                OldValue        = "Submitted",
                NewValue        = "In Progress",
                ChangedByUserId = _userManager.GetUserId(User),
                ChangedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Section 4.15: Send Notification to requester
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    $"Volunteer Assigned: {request.Title}",
                    $"Volunteer ({User.Identity?.Name}) accepted your request and is now responding.",
                    "VolunteerAssigned",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] = "Help request accepted. You are now assigned to it.";
            return RedirectToAction(nameof(MyAssignments));
        }

        // ================================================================
        // ASSIGN VOLUNTEER — Organization Admin / Coordinator / Admin
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organization Admin,Organization,Coordinator,Super Admin,Admin")]
        public async Task<IActionResult> AssignVolunteer(
            int id,
            int volunteerProfileId)
        {
            var request = await _context.HelpRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (request.Status == "Resolved" ||
                request.Status == "Closed" ||
                request.Status == "Completed")
            {
                TempData["ErrorMessage"] =
                    "A completed or closed request cannot be assigned.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var volunteer = await _context.VolunteerProfiles
                .Include(v => v.User)
                .Include(v => v.Organization)
                .FirstOrDefaultAsync(v => v.Id == volunteerProfileId);

            if (volunteer == null ||
                volunteer.User == null ||
                volunteer.Organization == null)
            {
                TempData["ErrorMessage"] =
                    "Volunteer not found.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (!volunteer.IsApprovedByOrg)
            {
                TempData["ErrorMessage"] =
                    "This volunteer is not approved by the organization.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (volunteer.AvailabilityStatus != "Available")
            {
                TempData["ErrorMessage"] =
                    "This volunteer is not currently available.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUserId = _userManager.GetUserId(User);

            // Organization Admin can assign ONLY volunteers
            // belonging to their own organization.
            if (User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization"))
            {
                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o =>
                        o.ManagedByUserId == currentUserId);

                if (myOrg == null ||
                    volunteer.OrganizationId != myOrg.Id)
                {
                    return Forbid();
                }
            }

            // Free previous volunteer if the request is being reassigned.
            if (!string.IsNullOrEmpty(request.AssignedVolunteerId) &&
                request.AssignedVolunteerId != volunteer.UserId)
            {
                var oldVolunteer =
                    await _context.VolunteerProfiles
                        .FirstOrDefaultAsync(v =>
                            v.UserId == request.AssignedVolunteerId);

                if (oldVolunteer != null)
                {
                    oldVolunteer.AvailabilityStatus = "Available";
                    oldVolunteer.UpdatedAt = DateTime.UtcNow;
                }
            }

            var oldVolunteerName = request.VolunteerEmail;

            // Assign
            request.AssignedVolunteerId = volunteer.UserId;
            request.VolunteerEmail = volunteer.User.Email;
            request.Status = "In Progress";
            request.UpdatedAt = DateTime.UtcNow;

            // Assigned volunteer becomes busy
            volunteer.AvailabilityStatus = "Busy";
            volunteer.UpdatedAt = DateTime.UtcNow;

            // Audit trail
            _context.HelpRequestStatusLogs.Add(
                new HelpRequestStatusLog
                {
                    HelpRequestId = id,
                    ChangeType = "Assignment",
                    OldValue = oldVolunteerName ?? "Unassigned",
                    NewValue = volunteer.User.FullName,
                    Note = $"Assigned to {volunteer.User.FullName} " +
                           $"from {volunteer.Organization.OrganizationName}.",
                    ChangedByUserId = currentUserId,
                    ChangedAt = DateTime.UtcNow
                });

            // Status history
            _context.HelpRequestStatusLogs.Add(
                new HelpRequestStatusLog
                {
                    HelpRequestId = id,
                    ChangeType = "Status",
                    OldValue = request.Status == "In Progress"
                        ? "Submitted"
                        : request.Status,
                    NewValue = "In Progress",
                    Note = "Volunteer assigned by organization.",
                    ChangedByUserId = currentUserId,
                    ChangedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();

            // Notify volunteer
            await _notificationService.CreateNotificationAsync(
                volunteer.UserId,
                $"New Task Assigned: {request.Title}",
                $"You have been assigned a new help request at {request.Location}.",
                "VolunteerAssigned",
                $"/HelpRequest/Details/{id}"
            );

            // Notify requester
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    $"Volunteer Assigned: {request.Title}",
                    $"{volunteer.User.FullName} has been assigned to your request.",
                    "VolunteerAssigned",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] =
                $"{volunteer.User.FullName} has been assigned to this request.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // DUPLICATE ACTIONS (Section 4.14)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator")]
        public async Task<IActionResult> DismissDuplicate(int id)
        {
            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.DuplicateDismissed = true;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Duplicate warning dismissed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator")]
        public async Task<IActionResult> MarkAsDuplicate(int id, int masterRequestId)
        {
            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null) return NotFound();

            var oldStatus = request.Status;
            request.Status = "Closed";
            request.DuplicateDetectionReason = $"Merged as duplicate into Request #{masterRequestId}";
            request.UpdatedAt = DateTime.UtcNow;

            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId   = id,
                ChangeType      = "Status",
                OldValue        = oldStatus,
                NewValue        = "Closed",
                ChangedByUserId = _userManager.GetUserId(User),
                ChangedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request #{id} closed and marked as duplicate of Request #{masterRequestId}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // MY ASSIGNMENTS — Volunteer view (preserved)
        // ================================================================
        [Authorize(Roles = "Volunteer")]
        [HttpGet]
        public async Task<IActionResult> MyAssignments()
        {
            var email = User.Identity?.Name;
            var requests = await _context.HelpRequests
                .Where(r => r.VolunteerEmail == email)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // ================================================================
        // UPDATE STATUS (Volunteer) — fixed: allows "In Progress" + "Completed"
        // ================================================================
        [Authorize(Roles = "Volunteer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VolunteerUpdateStatus(int id, string status, string? progressNote)
        {
            // Volunteers may only move a request to "In Progress" or "Completed"
            string[] allowedStatuses = { "In Progress", "Completed" };
            if (!allowedStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(MyAssignments));
            }

            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            // Ownership check: only the assigned volunteer may update
            if (request.VolunteerEmail != User.Identity?.Name)
                return Forbid();

            var oldStatus = request.Status;
            var volunteerId = _userManager.GetUserId(User);

            request.Status    = status;
            request.UpdatedAt = DateTime.UtcNow;

            // Store latest progress note if provided
            if (!string.IsNullOrWhiteSpace(progressNote))
                request.VolunteerProgressNote = progressNote.Trim();

            // Store completion metadata for public accountability
            if (status == "Completed")
            {
                request.CompletedAt = DateTime.UtcNow;
                request.CompletedByVolunteerId = volunteerId;

                var volunteerProfile =
                    await _context.VolunteerProfiles
                        .FirstOrDefaultAsync(v => v.UserId == volunteerId);

                if (volunteerProfile != null)
                {
                    volunteerProfile.AvailabilityStatus = "Available";
                    volunteerProfile.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Section 4.7: audit log
            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId   = id,
                ChangeType      = "Status",
                OldValue        = oldStatus,
                NewValue        = status,
                Note            = string.IsNullOrWhiteSpace(progressNote) ? null : progressNote.Trim(),
                ChangedByUserId = volunteerId,
                ChangedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify the original citizen requester
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var notifTitle = status == "Completed"
                    ? $"Your request has been completed: {request.Title}"
                    : $"Volunteer update on your request: {request.Title}";

                var notifBody = status == "Completed"
                    ? $"Your request has been marked as Completed by the assigned volunteer. " +
                      (string.IsNullOrWhiteSpace(progressNote) ? "" : $"Note: {progressNote}")
                    : $"Status changed to '{status}'. " +
                      (string.IsNullOrWhiteSpace(progressNote) ? "" : $"Update: {progressNote}");

                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    notifTitle,
                    notifBody,
                    status == "Completed" ? "RequestCompleted" : "VolunteerUpdate",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] = status == "Completed"
                ? "Request marked as Completed. The requester has been notified."
                : $"Status updated to \"{status}\".";
            return RedirectToAction(nameof(MyAssignments));
        }

        // ================================================================
        // ADD PROGRESS NOTE (Volunteer) — standalone note without status change
        // ================================================================
        [Authorize(Roles = "Volunteer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VolunteerAddNote(int id, string progressNote)
        {
            if (string.IsNullOrWhiteSpace(progressNote))
            {
                TempData["ErrorMessage"] = "Note cannot be empty.";
                return RedirectToAction(nameof(MyAssignments));
            }

            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.VolunteerEmail != User.Identity?.Name) return Forbid();

            request.VolunteerProgressNote = progressNote.Trim();
            request.UpdatedAt = DateTime.UtcNow;

            // Log the note as an update entry
            _context.HelpRequestStatusLogs.Add(new HelpRequestStatusLog
            {
                HelpRequestId   = id,
                ChangeType      = "Note",
                OldValue        = request.Status,
                NewValue        = request.Status,
                Note            = progressNote.Trim(),
                ChangedByUserId = _userManager.GetUserId(User),
                ChangedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify the requester of the progress note
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    $"Volunteer update on: {request.Title}",
                    $"Update: {progressNote.Trim()}",
                    "VolunteerUpdate",
                    $"/HelpRequest/Details/{id}"
                );
            }

            TempData["SuccessMessage"] = "Progress note saved and requester notified.";
            return RedirectToAction(nameof(MyAssignments));
        }

        // ================================================================
        // DELETE ATTACHMENT — POST (AJAX)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var att = await _context.HelpRequestAttachments
                .Include(a => a.HelpRequest)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (att == null)
                return Json(new { success = false, message = "Attachment not found." });

            var userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");
            bool isOwner = att.HelpRequest != null &&
                (att.HelpRequest.UserId == userId || att.HelpRequest.UserEmail == User.Identity?.Name);

            if (!isAdmin && !isOwner)
                return Json(new { success = false, message = "Unauthorized." });

            DeleteImageFile(att.FilePath);
            _context.HelpRequestAttachments.Remove(att);

            // Update backward-compat ImagePath
            if (att.HelpRequest != null && att.HelpRequest.ImagePath == att.FilePath)
            {
                var next = await _context.HelpRequestAttachments
                    .Where(a => a.HelpRequestId == att.HelpRequestId && a.Id != attachmentId)
                    .OrderBy(a => a.Id)
                    .FirstOrDefaultAsync();
                att.HelpRequest.ImagePath = next?.FilePath;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ================================================================
        // REACTION (Like / Dislike) — AJAX POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> React(int id, bool isLike)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Json(new { success = false, message = "Not authenticated." });

            var exists = await _context.HelpRequests.AnyAsync(r => r.Id == id);
            if (!exists)
                return Json(new { success = false, message = "Request not found." });

            var existing = await _context.HelpRequestReactions
                .FirstOrDefaultAsync(r => r.HelpRequestId == id && r.UserId == userId);

            if (existing != null)
            {
                if (existing.IsLike == isLike)
                {
                    // Toggle off
                    _context.HelpRequestReactions.Remove(existing);
                }
                else
                {
                    // Switch reaction
                    existing.IsLike = isLike;
                }
            }
            else
            {
                _context.HelpRequestReactions.Add(new HelpRequestReaction
                {
                    HelpRequestId = id,
                    UserId        = userId,
                    IsLike        = isLike,
                    CreatedAt     = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var likes    = await _context.HelpRequestReactions.CountAsync(r => r.HelpRequestId == id && r.IsLike);
            var dislikes = await _context.HelpRequestReactions.CountAsync(r => r.HelpRequestId == id && !r.IsLike);
            var myNew    = await _context.HelpRequestReactions.FirstOrDefaultAsync(r => r.HelpRequestId == id && r.UserId == userId);

            return Json(new { success = true, likes, dislikes, myReaction = myNew?.IsLike });
        }

        // ================================================================
        // RATING — AJAX POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int id, int ratingValue, string? review)
        {
            if (ratingValue < 1 || ratingValue > 5)
                return Json(new { success = false, message = "Rating must be between 1 and 5." });

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Json(new { success = false, message = "Not authenticated." });

            var request = await _context.HelpRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Request not found." });

            var existing = await _context.HelpRequestRatings
                .FirstOrDefaultAsync(r => r.HelpRequestId == id && r.UserId == userId);

            if (existing != null)
            {
                existing.RatingValue = ratingValue;
                existing.Review      = review?.Trim();
                existing.UpdatedAt   = DateTime.UtcNow;
            }
            else
            {
                _context.HelpRequestRatings.Add(new HelpRequestRating
                {
                    HelpRequestId = id,
                    UserId        = userId,
                    RatingValue   = ratingValue,
                    Review        = review?.Trim(),
                    CreatedAt     = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var avg   = await _context.HelpRequestRatings.Where(r => r.HelpRequestId == id).AverageAsync(r => (double)r.RatingValue);
            var count = await _context.HelpRequestRatings.CountAsync(r => r.HelpRequestId == id);

            return Json(new { success = true, avgRating = Math.Round(avg, 1), count, myRating = ratingValue });
        }

        // ================================================================
        // COMMENTS — POST / EDIT / DELETE (AJAX)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostComment(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                return Json(new { success = false, message = "Comment must be 1–2000 characters." });

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Json(new { success = false, message = "Not authenticated." });

            var exists = await _context.HelpRequests.AnyAsync(r => r.Id == id);
            if (!exists)
                return Json(new { success = false, message = "Request not found." });

            var user = await _userManager.FindByIdAsync(userId);
            var comment = new HelpRequestComment
            {
                HelpRequestId = id,
                UserId        = userId,
                Content       = content.Trim(),
                CreatedAt     = DateTime.UtcNow
            };
            _context.HelpRequestComments.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success    = true,
                id         = comment.Id,
                authorName = user?.FullName ?? User.Identity?.Name ?? "User",
                content    = comment.Content,
                createdAt  = comment.CreatedAt.ToString("dd MMM yyyy, hh:mm tt")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                return Json(new { success = false, message = "Comment must be 1–2000 characters." });

            var userId = _userManager.GetUserId(User);
            var comment = await _context.HelpRequestComments.FindAsync(commentId);
            if (comment == null)
                return Json(new { success = false, message = "Comment not found." });

            bool isAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");
            if (!isAdmin && comment.UserId != userId)
                return Json(new { success = false, message = "Unauthorized." });

            comment.Content   = content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, content = comment.Content });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = _userManager.GetUserId(User);
            var comment = await _context.HelpRequestComments.FindAsync(commentId);
            if (comment == null)
                return Json(new { success = false, message = "Comment not found." });

            bool isAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");
            if (!isAdmin && comment.UserId != userId)
                return Json(new { success = false, message = "Unauthorized." });

            _context.HelpRequestComments.Remove(comment);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ================================================================
        // MAP DATA — Public endpoint for map markers
        // ================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MapData()
        {
            var requests = await _context.HelpRequests
                .Where(r => r.Latitude != null && r.Longitude != null && r.Status != "Closed")
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Category,
                    r.Status,
                    r.Priority,
                    r.Latitude,
                    r.Longitude,
                    Location = r.Location
                })
                .ToListAsync();

            return Json(requests);
        }

        // ================================================================
        // PRIVATE HELPERS
        // ================================================================

        private bool CanEdit(HelpRequest request)
        {
            // Completed / Closed requests are locked.
            if (request.Status == "Completed" ||
                request.Status == "Closed" ||
                request.Status == "Resolved")
            {
                return false;
            }

            // Admins can edit only active requests.
            if (User.IsInRole("Super Admin") ||
                User.IsInRole("Admin"))
            {
                return true;
            }

            var userId = _userManager.GetUserId(User);

            return request.UserId == userId ||
                   request.UserEmail == User.Identity?.Name;
        }

        private bool CanDelete(HelpRequest request)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Admin"))
                return true;
            var userId = _userManager.GetUserId(User);
            return request.UserId == userId || request.UserEmail == User.Identity?.Name;
        }

        private static string? ValidateImage(IFormFile file)
        {
            if (file.Length > MaxImageBytes)
                return "Image must be smaller than 5 MB.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return "Only JPG, PNG, and WEBP images are allowed.";

            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return "Invalid image file type.";

            return null;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "helprequests");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath   = Path.Combine(uploadsFolder, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/helprequests/{uniqueName}";
        }

        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            var fullPath = Path.Combine(_environment.WebRootPath,
                imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        private static void ApplySearchFilterSort(
            ref IQueryable<HelpRequest> query,
            string? search, string? category, string? status, string? sortOrder)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(r =>
                    r.Title.Contains(search) ||
                    r.Location.Contains(search) ||
                    r.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(r => r.Category == category);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            query = sortOrder switch
            {
                "oldest"     => query.OrderBy(r => r.CreatedAt),
                "title_asc"  => query.OrderBy(r => r.Title),
                "title_desc" => query.OrderByDescending(r => r.Title),
                "priority"   => query.OrderByDescending(r =>
                    r.Priority == "High" ? 3 : r.Priority == "Medium" ? 2 : 1),
                _            => query.OrderByDescending(r => r.CreatedAt)
            };
        }

        private void SetViewBagPaging(int page, int totalPages, int totalCount,
            string? search, string? category, string? status, string? sortOrder)
        {
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages  = totalPages;
            ViewBag.TotalCount  = totalCount;
            ViewBag.Search      = search;
            ViewBag.Category    = category;
            ViewBag.Status      = status;
            ViewBag.SortOrder   = sortOrder;
        }
    }
}
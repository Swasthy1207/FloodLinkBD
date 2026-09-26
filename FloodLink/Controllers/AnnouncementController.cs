using FloodLink.Data;
using FloodLink.Models;
using FloodLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class AnnouncementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public static readonly string[] ValidCategories =
        {
            "Flood Warning",
            "Shelter Opening",
            "Shelter Closing",
            "Volunteer Recruitment",
            "Relief Distribution Schedule",
            "Safety Instructions",
            "General Notice"
        };

        public static readonly string[] ValidUrgencies =
        {
            "Low",
            "Normal",
            "High",
            "Critical"
        };

        public AnnouncementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // ================================================================
        // INDEX — Publicly accessible to all users
        // ================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, string? category, string? urgency, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Announcements
                .Include(a => a.Organization)
                .Include(a => a.CreatedBy)
                .AsQueryable();

            // Non-admins see active notices only
            bool isPrivileged = User.IsInRole("Super Admin") || User.IsInRole("Admin") || User.IsInRole("Coordinator") || User.IsInRole("Organization Admin");
            if (!isPrivileged)
            {
                query = query.Where(a => a.IsActive && (!a.ExpiresAt.HasValue || a.ExpiresAt.Value > DateTime.UtcNow));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(s) || 
                                         a.Content.ToLower().Contains(s) ||
                                         (a.TargetArea != null && a.TargetArea.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(urgency))
            {
                query = query.Where(a => a.UrgencyLevel == urgency);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var list = await query
                .OrderByDescending(a => a.UrgencyLevel == "Critical")
                .ThenByDescending(a => a.UrgencyLevel == "High")
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Urgency = urgency;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Categories = ValidCategories;
            ViewBag.Urgencies = ValidUrgencies;

            return View(list);
        }

        // ================================================================
        // DETAILS
        // ================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var announcement = await _context.Announcements
                .Include(a => a.Organization)
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // ================================================================
        // CREATE — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = ValidCategories;
            ViewBag.Urgencies = ValidUrgencies;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(new Announcement());
        }

        // ================================================================
        // CREATE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create(Announcement model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = ValidCategories;
                ViewBag.Urgencies = ValidUrgencies;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            model.CreatedByUserId = _userManager.GetUserId(User);
            model.CreatedAt = DateTime.UtcNow;

            // Normalize ExpiresAt to UTC — HTML datetime-local inputs produce Kind=Unspecified
            // which Npgsql rejects for timestamptz columns.
            if (model.ExpiresAt.HasValue)
                model.ExpiresAt = DateTime.SpecifyKind(model.ExpiresAt.Value, DateTimeKind.Utc);

            _context.Announcements.Add(model);
            await _context.SaveChangesAsync();

            // Section 4.15: Broadcast notification for emergency alerts
            string alertPrefix = model.UrgencyLevel switch
            {
                "Critical" => "🚨 CRITICAL WARNING",
                "High" => "⚠️ URGENT ALERT",
                _ => "📢 Emergency Notice"
            };

            string notifTitle = $"{alertPrefix}: {model.Title}";
            string notifBody = $"Category: {model.Category}. {model.Content.Substring(0, Math.Min(120, model.Content.Length))}...";

            // Notify all roles
            await _notificationService.NotifyRoleAsync("Citizen", notifTitle, notifBody, "EmergencyAnnouncement", $"/Announcement/Details/{model.Id}");
            await _notificationService.NotifyRoleAsync("Volunteer", notifTitle, notifBody, "EmergencyAnnouncement", $"/Announcement/Details/{model.Id}");
            await _notificationService.NotifyRoleAsync("Coordinator", notifTitle, notifBody, "EmergencyAnnouncement", $"/Announcement/Details/{model.Id}");

            TempData["SuccessMessage"] = "Emergency announcement published and broadcasted to users.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // EDIT — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.Categories = ValidCategories;
            ViewBag.Urgencies = ValidUrgencies;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(item);
        }

        // ================================================================
        // EDIT — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Edit(int id, Announcement model)
        {
            if (id != model.Id) return NotFound();

            var existing = await _context.Announcements.FindAsync(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = ValidCategories;
                ViewBag.Urgencies = ValidUrgencies;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            existing.Title = model.Title.Trim();
            existing.Category = model.Category;
            existing.Content = model.Content.Trim();
            existing.UrgencyLevel = model.UrgencyLevel;
            existing.TargetArea = model.TargetArea?.Trim();
            existing.IsActive = model.IsActive;
            // Normalize ExpiresAt to UTC — HTML datetime-local inputs produce Kind=Unspecified
            existing.ExpiresAt = model.ExpiresAt.HasValue
                ? DateTime.SpecifyKind(model.ExpiresAt.Value, DateTimeKind.Utc)
                : null;
            existing.OrganizationId = model.OrganizationId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement updated.";
            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // ================================================================
        // TOGGLE ACTIVE
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

            item.IsActive = !item.IsActive;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = item.IsActive ? "Announcement activated." : "Announcement deactivated.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // DELETE
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Organization Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

            _context.Announcements.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

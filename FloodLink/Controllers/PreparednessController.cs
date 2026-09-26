using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class PreparednessController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public static readonly string[] ValidActivityTypes =
        {
            "Volunteer Training",
            "Emergency Drill",
            "Awareness Campaign",
            "Meeting Schedule",
            "Resource Preparation",
            "Shelter Update"
        };

        public static readonly string[] ValidStatuses =
        {
            "Planned",
            "In Progress",
            "Completed",
            "Postponed",
            "Cancelled"
        };

        public PreparednessController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // INDEX — List Pre-Disaster Activities
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? activityType, string? status, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.PreparednessActivities
                .Include(p => p.Organization)
                .Include(p => p.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(s) || 
                                         p.Description.ToLower().Contains(s) ||
                                         (p.Location != null && p.Location.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(activityType))
            {
                query = query.Where(p => p.ActivityType == activityType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var list = await query
                .OrderBy(p => p.Status == "Completed" ? 1 : 0)
                .ThenBy(p => p.ScheduledDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ActivityType = activityType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.ActivityTypes = ValidActivityTypes;
            ViewBag.Statuses = ValidStatuses;

            return View(list);
        }

        // ================================================================
        // CREATE — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create()
        {
            ViewBag.ActivityTypes = ValidActivityTypes;
            ViewBag.Statuses = ValidStatuses;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(new PreparednessActivity { ScheduledDate = DateTime.UtcNow.AddDays(7) });
        }

        // ================================================================
        // CREATE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create(PreparednessActivity model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActivityTypes = ValidActivityTypes;
                ViewBag.Statuses = ValidStatuses;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            model.CreatedByUserId = _userManager.GetUserId(User);
            model.CreatedAt = DateTime.UtcNow;

            _context.PreparednessActivities.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Preparedness activity '{model.Title}' scheduled.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // DETAILS
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.PreparednessActivities
                .Include(p => p.Organization)
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        // ================================================================
        // EDIT — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.PreparednessActivities.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.ActivityTypes = ValidActivityTypes;
            ViewBag.Statuses = ValidStatuses;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(item);
        }

        // ================================================================
        // EDIT — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Edit(int id, PreparednessActivity model)
        {
            if (id != model.Id) return NotFound();

            var existing = await _context.PreparednessActivities.FindAsync(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ActivityTypes = ValidActivityTypes;
                ViewBag.Statuses = ValidStatuses;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            existing.Title = model.Title.Trim();
            existing.ActivityType = model.ActivityType;
            existing.Description = model.Description.Trim();
            existing.ScheduledDate = model.ScheduledDate;
            existing.Location = model.Location?.Trim();
            existing.ParticipantCount = model.ParticipantCount;
            existing.Status = model.Status;
            existing.OutcomeNotes = model.OutcomeNotes?.Trim();
            existing.OrganizationId = model.OrganizationId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Preparedness activity updated.";
            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // ================================================================
        // UPDATE STATUS QUICK ACTION
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var item = await _context.PreparednessActivities.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = status;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Activity status changed to '{status}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // DELETE
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Organization Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.PreparednessActivities.FindAsync(id);
            if (item == null) return NotFound();

            _context.PreparednessActivities.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Activity record deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

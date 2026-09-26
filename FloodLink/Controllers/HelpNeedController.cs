using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class HelpNeedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HelpNeedController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // INDEX
        // Organization Admin -> own organization's requests
        // Coordinator/Admin -> all organization help needs
        // ============================================================
        [HttpGet]
        [Authorize(Roles =
            "Organization Admin,Organization,Coordinator,Super Admin,Admin")]
        public async Task<IActionResult> Index()
        {
            var query = _context.OrganizationHelpNeeds
                .Include(h => h.Organization)
                .Include(h => h.RequestedByUser)
                .AsQueryable();

            if (User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization"))
            {
                var userId = _userManager.GetUserId(User);

                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o =>
                        o.ManagedByUserId == userId);

                if (myOrg == null)
                    return Forbid();

                query = query.Where(h =>
                    h.OrganizationId == myOrg.Id);
            }

            var needs = await query
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            return View(needs);
        }

        // ============================================================
        // CREATE GET
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Organization Admin,Organization")]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o =>
                    o.ManagedByUserId == userId &&
                    o.Status == "Approved");

            if (myOrg == null)
                return Forbid();

            var bangladeshTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    OperatingSystem.IsWindows()
                        ? "Bangladesh Standard Time"
                        : "Asia/Dhaka");

            var bangladeshNow =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    bangladeshTimeZone);

            return View(new OrganizationHelpNeed
            {
                OrganizationId = myOrg.Id,
                NeededBy = bangladeshNow.AddHours(6),
                Priority = "Medium",
                Status = "Pending"
            });
        }

        // ============================================================
        // CREATE POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organization Admin,Organization")]
        public async Task<IActionResult> Create(
            OrganizationHelpNeed model)
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o =>
                    o.ManagedByUserId == userId &&
                    o.Status == "Approved");

            if (myOrg == null)
                return Forbid();

            // Never trust OrganizationId coming from the browser.
            model.OrganizationId = myOrg.Id;
            model.RequestedByUserId = userId!;
            model.Status = "Pending";
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = null;

            // Remove navigation-property validation.
            ModelState.Remove(nameof(model.Organization));
            ModelState.Remove(nameof(model.RequestedByUser));
            ModelState.Remove(nameof(model.ReviewedByUser));
            ModelState.Remove(nameof(model.ReviewedByUserId));
            ModelState.Remove(nameof(model.RequestedByUserId));

            if (!ModelState.IsValid)
                return View(model);

            // ============================================================
            // CONVERT datetime-local BANGLADESH TIME -> UTC
            // ============================================================

            if (model.NeededBy.HasValue)
            {
                var bangladeshTime =
                    DateTime.SpecifyKind(
                        model.NeededBy.Value,
                        DateTimeKind.Unspecified);

                var bangladeshTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        OperatingSystem.IsWindows()
                            ? "Bangladesh Standard Time"
                            : "Asia/Dhaka");

                model.NeededBy =
                    TimeZoneInfo.ConvertTimeToUtc(
                        bangladeshTime,
                        bangladeshTimeZone);
            }

            // ============================================================
            // SAVE
            // ============================================================

            _context.OrganizationHelpNeeds.Add(model);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Help need submitted successfully. The coordination team will review it.";

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // DETAILS
        // ============================================================
        [HttpGet]
        [Authorize(Roles =
            "Organization Admin,Organization,Coordinator,Super Admin,Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var need = await _context.OrganizationHelpNeeds
                .Include(h => h.Organization)
                .Include(h => h.RequestedByUser)
                .Include(h => h.ReviewedByUser)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (need == null)
                return NotFound();

            // Organization Admin can only see own organization's need
            if (User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization"))
            {
                var userId = _userManager.GetUserId(User);

                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o =>
                        o.ManagedByUserId == userId);

                if (myOrg == null ||
                    need.OrganizationId != myOrg.Id)
                {
                    return Forbid();
                }
            }

            return View(need);
        }

        // ============================================================
        // UPDATE STATUS
        // Coordinator / Admin
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Coordinator,Super Admin,Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status,
            string? responseNotes)
        {
            string[] valid =
            {
                "Pending",
                "Under Review",
                "Accepted",
                "In Progress",
                "Resolved",
                "Rejected"
            };

            if (!valid.Contains(status))
            {
                TempData["ErrorMessage"] =
                    "Invalid help need status.";

                return RedirectToAction(nameof(Index));
            }

            var need = await _context.OrganizationHelpNeeds
                .FirstOrDefaultAsync(h => h.Id == id);

            if (need == null)
                return NotFound();

            need.Status = status;
            need.ResponseNotes = responseNotes?.Trim();
            need.ReviewedByUserId = _userManager.GetUserId(User);
            need.ReviewedAt = DateTime.UtcNow;
            need.UpdatedAt = DateTime.UtcNow;

            if (status == "Resolved")
                need.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Help need status updated to \"{status}\".";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
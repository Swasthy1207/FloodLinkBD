using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class ShelterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] ValidStatuses = { "Open", "Full", "Closed", "Under Maintenance" };

        public ShelterController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // INDEX — public list with search/filter/pagination
        // ================================================================
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, string? status, string? sortOrder, int page = 1)
        {
            const int pageSize = 9;
            ViewBag.Search    = search;
            ViewBag.Status    = status;
            ViewBag.SortOrder = sortOrder;

            var query = _context.Shelters.Include(s => s.Organization).AsQueryable();

            // Organization Admin can see only shelters managed by their own organization
            if (User.IsInRole("Organization Admin") || User.IsInRole("Organization"))
            {
                var userId = _userManager.GetUserId(User);

                var myOrganizationId = await _context.Organizations
                    .Where(o => o.ManagedByUserId == userId)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefaultAsync();

                if (myOrganizationId.HasValue)
                {
                    query = query.Where(s => s.OrganizationId == myOrganizationId.Value);
                }
                else
                {
                    // No organization linked to this account
                    query = query.Where(s => false);
                }
            }

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.ShelterName.Contains(search) || s.Location.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            query = sortOrder switch
            {
                "capacity_desc" => query.OrderByDescending(s => s.Capacity),
                "capacity_asc"  => query.OrderBy(s => s.Capacity),
                "occupancy"     => query.OrderByDescending(s => s.CurrentOccupancy),
                "name_asc"      => query.OrderBy(s => s.ShelterName),
                _               => query.OrderByDescending(s => s.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var shelters = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages  = totalPages;
            ViewBag.TotalCount  = totalCount;
            ViewBag.ValidStatuses = ValidStatuses;
            return View(shelters);
        }

        // ================================================================
        // DETAILS
        // ================================================================
        public async Task<IActionResult> Details(int id)
        {
            var shelter = await _context.Shelters
                .Include(s => s.Organization)
                .Include(s => s.CreatedBy)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shelter == null) return NotFound();
            return View(shelter);
        }

        // ================================================================
        // CREATE — GET
        // ================================================================
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            ViewBag.ValidStatuses = ValidStatuses;
            return View();
        }

        // ================================================================
        // CREATE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> Create(Shelter model)
        {
            if (!ValidStatuses.Contains(model.Status))
                ModelState.AddModelError("Status", "Invalid status.");

            if (model.CurrentOccupancy > model.Capacity)
                ModelState.AddModelError("CurrentOccupancy", "Occupancy cannot exceed capacity.");

            if (!ModelState.IsValid)
            {
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                ViewBag.ValidStatuses = ValidStatuses;
                return View(model);
            }

            model.CreatedByUserId = _userManager.GetUserId(User);
            model.CreatedAt = DateTime.UtcNow;
            _context.Shelters.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Shelter \"{model.ShelterName}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // EDIT — GET
        // ================================================================
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null) return NotFound();

            if (!CanManage(shelter)) return Forbid();

            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            ViewBag.ValidStatuses = ValidStatuses;
            return View(shelter);
        }

        // ================================================================
        // EDIT — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> Edit(int id, Shelter model)
        {
            if (id != model.Id) return NotFound();

            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null) return NotFound();

            if (!CanManage(shelter)) return Forbid();

            if (!ValidStatuses.Contains(model.Status))
                ModelState.AddModelError("Status", "Invalid status.");

            if (model.CurrentOccupancy > model.Capacity)
                ModelState.AddModelError("CurrentOccupancy", "Occupancy cannot exceed capacity.");

            if (!ModelState.IsValid)
            {
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                ViewBag.ValidStatuses = ValidStatuses;
                return View(model);
            }

            shelter.ShelterName      = model.ShelterName;
            shelter.Location         = model.Location;
            shelter.Capacity         = model.Capacity;
            shelter.CurrentOccupancy = model.CurrentOccupancy;
            shelter.Status           = model.Status;
            shelter.ContactPerson    = model.ContactPerson;
            shelter.ContactPhone     = model.ContactPhone;
            shelter.Facilities       = model.Facilities;
            shelter.OrganizationId   = model.OrganizationId;
            shelter.UpdatedAt        = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Shelter updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // DELETE — GET
        // ================================================================
        [Authorize(Roles = "Super Admin,Admin,Coordinator")]
        public async Task<IActionResult> Delete(int id)
        {
            var shelter = await _context.Shelters.Include(s => s.Organization).FirstOrDefaultAsync(s => s.Id == id);
            if (shelter == null) return NotFound();
            return View(shelter);
        }

        // ================================================================
        // DELETE — POST
        // ================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null) return NotFound();

            _context.Shelters.Remove(shelter);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Shelter removed.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // UPDATE OCCUPANCY — Quick POST for coordinators
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin")]
        public async Task<IActionResult> UpdateOccupancy(int id, int currentOccupancy)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null) return NotFound();

            if (currentOccupancy < 0 || currentOccupancy > shelter.Capacity)
            {
                TempData["ErrorMessage"] = $"Occupancy must be between 0 and {shelter.Capacity}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            shelter.CurrentOccupancy = currentOccupancy;
            shelter.Status = currentOccupancy >= shelter.Capacity ? "Full" : "Open";
            shelter.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Occupancy updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private bool CanManage(Shelter shelter)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Admin") || User.IsInRole("Coordinator"))
                return true;

            // Org Admin can only manage shelters of their org
            var userId = _userManager.GetUserId(User);
            var myOrg  = _context.Organizations.FirstOrDefault(o => o.ManagedByUserId == userId);
            return myOrg != null && shelter.OrganizationId == myOrg.Id;
        }
    }
}

using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class HighRiskAreaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public static readonly string[] ValidRiskLevels = { "High", "Medium", "Low" };

        public static readonly string[] ValidAccessibilities =
        {
            "Easily Accessible",
            "Difficult",
            "Very Difficult",
            "Boat Required",
            "Road Inaccessible"
        };

        public HighRiskAreaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // INDEX — Search, Filter, Sort, Pagination
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search, 
            string? riskLevel, 
            string? accessibility, 
            string? sortBy, 
            int page = 1)
        {
            const int pageSize = 10;
            var query = _context.HighRiskAreas
                .Include(a => a.Organization)
                .Include(a => a.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.AreaName.ToLower().Contains(s) ||
                                         (a.DistrictOrUpazila != null && a.DistrictOrUpazila.ToLower().Contains(s)) ||
                                         (a.UnionOrWard != null && a.UnionOrWard.ToLower().Contains(s)) ||
                                         (a.TerrainAndHazardNotes != null && a.TerrainAndHazardNotes.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(riskLevel))
            {
                query = query.Where(a => a.RiskLevel == riskLevel);
            }

            if (!string.IsNullOrWhiteSpace(accessibility))
            {
                query = query.Where(a => a.Accessibility == accessibility);
            }

            // Sorting
            query = sortBy switch
            {
                "pop_desc" => query.OrderByDescending(a => a.PopulationEstimate),
                "pop_asc" => query.OrderBy(a => a.PopulationEstimate),
                "name_asc" => query.OrderBy(a => a.AreaName),
                "risk_desc" => query.OrderByDescending(a => a.RiskLevel == "High" ? 3 : a.RiskLevel == "Medium" ? 2 : 1),
                _ => query.OrderByDescending(a => a.RiskLevel == "High" ? 3 : a.RiskLevel == "Medium" ? 2 : 1).ThenByDescending(a => a.PopulationEstimate)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.RiskLevel = riskLevel;
            ViewBag.Accessibility = accessibility;
            ViewBag.SortBy = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.RiskLevels = ValidRiskLevels;
            ViewBag.Accessibilities = ValidAccessibilities;

            return View(list);
        }

        // ================================================================
        // CREATE — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create()
        {
            ViewBag.RiskLevels = ValidRiskLevels;
            ViewBag.Accessibilities = ValidAccessibilities;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(new HighRiskArea());
        }

        // ================================================================
        // CREATE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create(HighRiskArea model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RiskLevels = ValidRiskLevels;
                ViewBag.Accessibilities = ValidAccessibilities;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            model.CreatedByUserId = _userManager.GetUserId(User);
            model.CreatedAt = DateTime.UtcNow;

            _context.HighRiskAreas.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"High-risk area '{model.AreaName}' recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // DETAILS
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var area = await _context.HighRiskAreas
                .Include(a => a.Organization)
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (area == null) return NotFound();

            return View(area);
        }

        // ================================================================
        // EDIT — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.HighRiskAreas.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.RiskLevels = ValidRiskLevels;
            ViewBag.Accessibilities = ValidAccessibilities;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(item);
        }

        // ================================================================
        // EDIT — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Edit(int id, HighRiskArea model)
        {
            if (id != model.Id) return NotFound();

            var existing = await _context.HighRiskAreas.FindAsync(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.RiskLevels = ValidRiskLevels;
                ViewBag.Accessibilities = ValidAccessibilities;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            existing.AreaName = model.AreaName.Trim();
            existing.DistrictOrUpazila = model.DistrictOrUpazila?.Trim();
            existing.UnionOrWard = model.UnionOrWard?.Trim();
            existing.RiskLevel = model.RiskLevel;
            existing.Accessibility = model.Accessibility;
            existing.PopulationEstimate = model.PopulationEstimate;
            existing.TerrainAndHazardNotes = model.TerrainAndHazardNotes?.Trim();
            existing.IsActive = model.IsActive;
            existing.OrganizationId = model.OrganizationId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "High-risk area information updated.";
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
            var item = await _context.HighRiskAreas.FindAsync(id);
            if (item == null) return NotFound();

            item.IsActive = !item.IsActive;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = item.IsActive ? "Area marked active." : "Area marked inactive / safe.";
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
            var item = await _context.HighRiskAreas.FindAsync(id);
            if (item == null) return NotFound();

            _context.HighRiskAreas.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "High-risk area deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

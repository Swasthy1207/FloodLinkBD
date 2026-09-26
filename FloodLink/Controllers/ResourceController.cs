using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class ResourceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public static readonly string[] ValidCategories =
        {
            "Food Packets",
            "Drinking Water",
            "Medicine",
            "Blankets",
            "Baby Food",
            "Hygiene Kits",
            "Rescue Boats",
            "Life Jackets",
            "Ambulances",
            "Emergency Vehicles",
            "Other Supplies"
        };

        public static readonly string[] ValidUnits =
        {
            "Packets",
            "Liters",
            "Units",
            "Boxes",
            "Kits",
            "Bottles",
            "Bags"
        };

        public static readonly string[] ValidStatuses =
        {
            "Available",
            "In Use / Deployed",
            "Low Stock",
            "Depleted",
            "Under Maintenance"
        };

        public ResourceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // INDEX — List Resources & Equipment
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? classification, string? category, string? status, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Resources
                .Include(r => r.Organization)
                .Include(r => r.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(r => r.ResourceName.ToLower().Contains(s) || 
                                         (r.Location != null && r.Location.ToLower().Contains(s)) ||
                                         (r.Notes != null && r.Notes.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(classification))
            {
                query = query.Where(r => r.ItemClassification == classification);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(r => r.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Classification = classification;
            ViewBag.Category = category;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Categories = ValidCategories;
            ViewBag.Statuses = ValidStatuses;

            return View(items);
        }

        // ================================================================
        // CREATE — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = ValidCategories;
            ViewBag.Units = ValidUnits;
            ViewBag.Statuses = ValidStatuses;
            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(new Resource());
        }

        // ================================================================
        // CREATE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
        public async Task<IActionResult> Create(Resource model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = ValidCategories;
                ViewBag.Units = ValidUnits;
                ViewBag.Statuses = ValidStatuses;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            model.CreatedByUserId = _userManager.GetUserId(User);
            model.CreatedAt = DateTime.UtcNow;

            // Automatically deduce classification based on category
            if (model.Category == "Rescue Boats" || model.Category == "Life Jackets" || 
                model.Category == "Ambulances" || model.Category == "Emergency Vehicles")
            {
                model.ItemClassification = "Equipment";
            }
            else
            {
                model.ItemClassification = "Resource";
            }

            // Adjust status automatically if zero
            if (model.Quantity <= 0 && model.ItemClassification == "Resource")
            {
                model.Status = "Depleted";
            }

            _context.Resources.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Resource/Equipment '{model.ResourceName}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // DETAILS
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Resources
                .Include(r => r.Organization)
                .Include(r => r.CreatedBy)
                .Include(r => r.Distributions)
                    .ThenInclude(d => d.DistributedBy)
                .Include(r => r.Distributions)
                    .ThenInclude(d => d.DisasterOperation)
                .FirstOrDefaultAsync(r => r.Id == id);

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
            var item = await _context.Resources.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.Categories = ValidCategories;
            ViewBag.Units = ValidUnits;
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
        public async Task<IActionResult> Edit(int id, Resource model)
        {
            if (id != model.Id) return NotFound();

            var existing = await _context.Resources.FindAsync(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = ValidCategories;
                ViewBag.Units = ValidUnits;
                ViewBag.Statuses = ValidStatuses;
                ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
                return View(model);
            }

            existing.ResourceName = model.ResourceName.Trim();
            existing.Category = model.Category;
            existing.ItemClassification = model.ItemClassification;
            existing.Quantity = model.Quantity;
            existing.Unit = model.Unit;
            existing.Status = model.Status;
            existing.Location = model.Location?.Trim();
            existing.Notes = model.Notes?.Trim();
            existing.OrganizationId = model.OrganizationId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource updated successfully.";
            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // ================================================================
        // DISTRIBUTE — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization,Volunteer")]
        public async Task<IActionResult> Distribute(int id)
        {
            var item = await _context.Resources.FindAsync(id);
            if (item == null) return NotFound();

            if (item.Quantity <= 0 && item.ItemClassification == "Resource")
            {
                TempData["ErrorMessage"] = "Cannot distribute: item is currently out of stock.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Resource = item;
            ViewBag.Operations = await _context.DisasterOperations.Where(o => o.Status == "Active").ToListAsync();

            var distribution = new ResourceDistribution
            {
                ResourceId = item.Id,
                Quantity = 1,
                OrganizationId = item.OrganizationId
            };

            return View(distribution);
        }

        // ================================================================
        // DISTRIBUTE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization,Volunteer")]
        public async Task<IActionResult> Distribute(ResourceDistribution model)
        {
            var item = await _context.Resources.FindAsync(model.ResourceId);
            if (item == null) return NotFound();

            if (model.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Distribution quantity must be greater than zero.");
            }

            if (item.ItemClassification == "Resource" && model.Quantity > item.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Cannot distribute {model.Quantity} {item.Unit}. Only {item.Quantity} {item.Unit} available.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Resource = item;
                ViewBag.Operations = await _context.DisasterOperations.Where(o => o.Status == "Active").ToListAsync();
                return View(model);
            }

            model.DistributedByUserId = _userManager.GetUserId(User);
            model.DistributionDate = DateTime.UtcNow;

            // Deduct stock if relief resource
            if (item.ItemClassification == "Resource")
            {
                item.Quantity -= model.Quantity;
                if (item.Quantity == 0) item.Status = "Depleted";
                else if (item.Quantity <= 10) item.Status = "Low Stock";
                item.UpdatedAt = DateTime.UtcNow;
            }
            else // Equipment deployed
            {
                item.Status = "In Use / Deployed";
                item.UpdatedAt = DateTime.UtcNow;
            }

            _context.ResourceDistributions.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully recorded distribution of {model.Quantity} {item.Unit} to '{model.DistributedTo}'.";
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }

        // ================================================================
        // DISTRIBUTIONS LIST — Global Audit View
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Distributions(int page = 1)
        {
            const int pageSize = 15;
            var query = _context.ResourceDistributions
                .Include(d => d.Resource)
                .Include(d => d.Organization)
                .Include(d => d.DistributedBy)
                .Include(d => d.DisasterOperation)
                .OrderByDescending(d => d.DistributionDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(list);
        }

        // ================================================================
        // DELETE
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Organization Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Resources.FindAsync(id);
            if (item == null) return NotFound();

            _context.Resources.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource item removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}

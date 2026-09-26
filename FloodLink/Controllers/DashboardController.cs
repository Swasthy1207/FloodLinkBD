using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FloodLink.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Default entry point: route user to their specific dashboard based on role
        public IActionResult Index()
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(SuperAdmin));
            }

            if (User.IsInRole("Organization Admin") || User.IsInRole("Organization"))
            {
                return RedirectToAction(nameof(Organization));
            }

            if (User.IsInRole("Coordinator"))
            {
                return RedirectToAction(nameof(Coordinator));
            }

            if (User.IsInRole("Volunteer"))
            {
                return RedirectToAction(nameof(Volunteer));
            }

            if (User.IsInRole("Community Reporter"))
            {
                return RedirectToAction(nameof(CommunityReporter));
            }

            return RedirectToAction(nameof(Citizen));
        }

        // ================================================================
        // 1. Super Admin / Admin Dashboard (Section 4.16)
        // ================================================================
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> SuperAdmin()
        {
            await LoadDashboardMetricsAsync(null);
            return View("Admin");
        }

        [Authorize(Roles = "Super Admin,Admin")]
        public IActionResult Users()
        {
            return RedirectToAction("Users", "Account");
        }

        // ================================================================
        // 2. Organization Dashboard (Section 4.16 — Each Org has its own dashboard)
        // ================================================================
        [Authorize(Roles = "Organization Admin,Organization")]
        public async Task<IActionResult> Organization()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var org = userId != null
                ? await _context.Organizations
                    .FirstOrDefaultAsync(o => o.ManagedByUserId == userId)
                : null;

            await LoadDashboardMetricsAsync(org?.Id);

            return View("Organization", org);
        }

        // ================================================================
        // 3. Coordinator Dashboard (Section 4.16)
        // ================================================================
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> Coordinator()
        {
            await LoadDashboardMetricsAsync(null);
            return View("Coordinator");
        }

        // ================================================================
        // 4. Volunteer Dashboard
        // ================================================================
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> Volunteer()
        {
            var email = User.Identity?.Name;
            ViewBag.AssignedRequests = await _context.HelpRequests
                .Where(r => r.VolunteerEmail == email && (r.Status == "In Progress" || r.Status == "Submitted"))
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalAvailable = await _context.HelpRequests
                .CountAsync(r => r.Status == "Submitted");

            return View("Volunteer");
        }

        // ================================================================
        // 5. Community Reporter Dashboard (Section 4.5)
        // ================================================================
        [Authorize(Roles = "Community Reporter")]
        public async Task<IActionResult> CommunityReporter()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var query = _context.HelpRequests.Where(r => r.UserId == userId);

            ViewBag.TotalReports = await query.CountAsync();
            ViewBag.PendingReports = await query.CountAsync(r => r.Status == "Submitted");
            ViewBag.InProgressReports = await query.CountAsync(r => r.Status == "In Progress");
            ViewBag.ResolvedReports = await query.CountAsync(r => r.Status == "Resolved" || r.Status == "Closed");
            ViewBag.SubmittedOnBehalf = await query.CountAsync(r => !string.IsNullOrEmpty(r.AffectedPersonName));

            ViewBag.RecentReports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View("CommunityReporter");
        }

        // ================================================================
        // 6. Citizen Dashboard
        // ================================================================
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> Citizen()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            ViewBag.MyRequestsCount = await _context.HelpRequests
                .CountAsync(r => r.UserId == userId);

            ViewBag.ActiveAnnouncements = await _context.Announcements
                .Where(a => a.IsActive && (!a.ExpiresAt.HasValue || a.ExpiresAt.Value > DateTime.UtcNow))
                .OrderByDescending(a => a.UrgencyLevel == "Critical")
                .ThenByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            return View("Citizen");
        }

        // ================================================================
        // Helper to load Section 4.16 Comprehensive Dashboard Metrics
        // ================================================================
        private async Task LoadDashboardMetricsAsync(int? orgId)
        {
            // 1. Help Requests
            var reqQuery = _context.HelpRequests.AsQueryable();
            ViewBag.TotalRequests = await reqQuery.CountAsync();
            ViewBag.PendingRequests = await reqQuery.CountAsync(r => r.Status == "Submitted" || r.Status == "In Progress" || r.Status == "Pending");
            ViewBag.CompletedRequests = await reqQuery.CountAsync(r => r.Status == "Resolved" || r.Status == "Closed" || r.Status == "Completed");
            ViewBag.HighPriorityRequests = await reqQuery.CountAsync(r => r.Priority == "High" && (r.Status == "Submitted" || r.Status == "In Progress"));

            // 2. Active Volunteers
            var volQuery = _context.VolunteerProfiles.AsQueryable();
            if (orgId.HasValue)
            {
                volQuery = volQuery.Where(v => v.OrganizationId == orgId.Value && v.IsApprovedByOrg);
            }
            ViewBag.ActiveVolunteers = await volQuery.CountAsync(v => v.AvailabilityStatus == "Available");
            ViewBag.TotalVolunteers = await volQuery.CountAsync();

            // 3. Available Resources & Equipment
            var resQuery = _context.Resources.AsQueryable();
            if (orgId.HasValue)
            {
                resQuery = resQuery.Where(r => r.OrganizationId == orgId.Value);
            }
            ViewBag.TotalResourceItems = await resQuery.CountAsync();
            ViewBag.AvailableSuppliesCount = await resQuery.Where(r => r.ItemClassification == "Resource" && r.Status != "Depleted").SumAsync(r => (int?)r.Quantity) ?? 0;
            ViewBag.AvailableEquipmentCount = await resQuery.Where(r => r.ItemClassification == "Equipment" && r.Status != "Depleted").SumAsync(r => (int?)r.Quantity) ?? 0;
            ViewBag.LowStockItems = await resQuery.CountAsync(r => r.Status == "Low Stock");

            // 4. Shelter Information
            var shelterQuery = _context.Shelters.AsQueryable();
            if (orgId.HasValue)
            {
                shelterQuery = shelterQuery.Where(s => s.OrganizationId == orgId.Value);
            }
            ViewBag.TotalShelters = await shelterQuery.CountAsync();
            ViewBag.OpenShelters = await shelterQuery.CountAsync(s => s.Status == "Open");
            ViewBag.TotalCapacity = await shelterQuery.SumAsync(s => (int?)s.Capacity) ?? 0;
            ViewBag.TotalOccupancy = await shelterQuery.SumAsync(s => (int?)s.CurrentOccupancy) ?? 0;
            int capacity = ViewBag.TotalCapacity;
            int occupancy = ViewBag.TotalOccupancy;
            ViewBag.AvailableShelterSpaces = Math.Max(0, capacity - occupancy);

            // 5. Disaster Statistics
            ViewBag.TotalHighRiskAreas = await _context.HighRiskAreas.CountAsync();
            ViewBag.ActiveHighRiskAreas = await _context.HighRiskAreas.CountAsync(a => a.IsActive && a.RiskLevel == "High");
            ViewBag.BoatRequiredAreas = await _context.HighRiskAreas.CountAsync(a => a.IsActive && a.Accessibility == "Boat Required");
            ViewBag.ActiveOperations = await _context.DisasterOperations.CountAsync(o => o.Status == "Active");

            // 6. Recent Requests
            ViewBag.RecentRequests = await reqQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 7. Recent Announcements
            ViewBag.RecentAnnouncements = await _context.Announcements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();
        }
    }
}
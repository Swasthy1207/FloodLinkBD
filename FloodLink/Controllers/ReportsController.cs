using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize(Roles = "Super Admin,Admin,Coordinator,Organization Admin,Organization")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // INDEX — Relief Activities Report Generator
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            int? organizationId,
            int? operationId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            // ============================================================
            // DETERMINE CURRENT ORGANIZATION
            // ============================================================

            int? currentOrganizationId = null;

            bool isOrganizationUser =
                User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization");

            if (isOrganizationUser)
            {
                var userId = _userManager.GetUserId(User);

                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o =>
                        o.ManagedByUserId == userId &&
                        o.Status == "Approved");

                if (myOrg == null)
                    return Forbid();

                currentOrganizationId = myOrg.Id;

                // Organization users MUST only see their own organization.
                organizationId = myOrg.Id;
            }

            // ============================================================
            // HELP REQUEST QUERY
            // ============================================================

            var reqQuery = _context.HelpRequests
                .AsQueryable();

            // ============================================================
            // RESOURCE DISTRIBUTION QUERY
            // ============================================================

            var distQuery = _context.ResourceDistributions
                .Include(d => d.Resource)
                .Include(d => d.Organization)
                .Include(d => d.DisasterOperation)
                .AsQueryable();

            // ============================================================
            // DATE FILTER
            // ============================================================

            if (fromDate.HasValue)
            {
                var f = DateTime.SpecifyKind(
                    fromDate.Value.Date,
                    DateTimeKind.Utc);

                reqQuery = reqQuery.Where(r =>
                    r.CreatedAt >= f);

                distQuery = distQuery.Where(d =>
                    d.DistributionDate >= f);
            }

            if (toDate.HasValue)
            {
                var t = DateTime.SpecifyKind(
                    toDate.Value.Date
                        .AddDays(1)
                        .AddTicks(-1),
                    DateTimeKind.Utc);

                reqQuery = reqQuery.Where(r =>
                    r.CreatedAt <= t);

                distQuery = distQuery.Where(d =>
                    d.DistributionDate <= t);
            }

            // ============================================================
            // ORGANIZATION FILTER
            // ============================================================

            if (organizationId.HasValue)
            {
                distQuery = distQuery.Where(d =>
                    d.OrganizationId == organizationId.Value);
            }

            // ============================================================
            // OPERATION FILTER
            // ============================================================

            if (operationId.HasValue)
            {
                reqQuery = reqQuery.Where(r =>
                    r.DisasterOperationId == operationId.Value);

                distQuery = distQuery.Where(d =>
                    d.DisasterOperationId == operationId.Value);
            }

            // ============================================================
            // SUMMARY STATS
            // ============================================================

            ViewBag.TotalRequests =
                await reqQuery.CountAsync();

            ViewBag.ResolvedRequests =
                await reqQuery.CountAsync(r =>
                    r.Status == "Resolved" ||
                    r.Status == "Closed" ||
                    r.Status == "Completed");

            ViewBag.InProgressRequests =
                await reqQuery.CountAsync(r =>
                    r.Status == "In Progress");

            ViewBag.HighPriorityRequests =
                await reqQuery.CountAsync(r =>
                    r.Priority == "High");

            // ============================================================
            // CATEGORY BREAKDOWN
            // ============================================================

            ViewBag.RequestsByCategory =
                await reqQuery
                    .GroupBy(r => r.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.Category,
                        x => x.Count);

            // ============================================================
            // RESOURCE DISTRIBUTION
            // ============================================================

            var distributions =
                await distQuery.ToListAsync();

            ViewBag.TotalDistributions =
                distributions.Count;

            ViewBag.TotalUnitsDistributed =
                distributions.Sum(d => d.Quantity);

            ViewBag.Distributions =
                distributions;

            // ============================================================
            // SHELTERS
            // ============================================================

            var shelterQuery =
                _context.Shelters
                    .AsQueryable();

            if (organizationId.HasValue)
            {
                shelterQuery = shelterQuery.Where(s =>
                    s.OrganizationId == organizationId.Value);
            }

            var shelters =
                await shelterQuery.ToListAsync();

            ViewBag.TotalShelters =
                shelters.Count;

            ViewBag.TotalCapacity =
                shelters.Sum(s => s.Capacity);

            ViewBag.TotalOccupancy =
                shelters.Sum(s => s.CurrentOccupancy);

            ViewBag.Shelters =
                shelters;

            // ============================================================
            // HIGH RISK AREAS
            // ============================================================

            var areaQuery =
                _context.HighRiskAreas
                    .AsQueryable();

            if (organizationId.HasValue)
            {
                areaQuery = areaQuery.Where(a =>
                    a.OrganizationId == organizationId.Value);
            }

            var areas =
                await areaQuery.ToListAsync();

            ViewBag.HighRiskAreas =
                areas;

            // ============================================================
            // ORGANIZATION LIST
            // ============================================================

            if (isOrganizationUser)
            {
                ViewBag.Organizations =
                    await _context.Organizations
                        .Where(o =>
                            o.Id == currentOrganizationId.Value &&
                            o.Status == "Approved")
                        .ToListAsync();
            }
            else
            {
                ViewBag.Organizations =
                    await _context.Organizations
                        .Where(o => o.Status == "Approved")
                        .ToListAsync();
            }

            // ============================================================
            // OPERATIONS
            // ============================================================

            var operationsQuery =
                _context.DisasterOperations
                    .AsQueryable();

            if (organizationId.HasValue)
            {
                // Show operations where this organization
                // is the lead organization OR a participating organization.
                operationsQuery =
                    operationsQuery.Where(op =>
                        op.CreatedByOrganizationId ==
                            organizationId.Value
                        ||
                        _context.OrganizationOperations.Any(oo =>
                            oo.DisasterOperationId ==
                                op.DisasterOperationId
                            &&
                            oo.OrganizationId ==
                                organizationId.Value));
            }

            ViewBag.Operations =
                await operationsQuery.ToListAsync();

            // ============================================================
            // SELECTED FILTERS
            // ============================================================

            ViewBag.SelectedOrgId =
                organizationId;

            ViewBag.SelectedOpId =
                operationId;

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            return View();
        }

        // ================================================================
        // PRINT — Clean printer-friendly version
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Print(
            int? organizationId,
            int? operationId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            await Index(
                organizationId,
                operationId,
                fromDate,
                toDate);

            return View();
        }
    }
}
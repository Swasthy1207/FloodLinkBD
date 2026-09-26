using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Available organization types for dropdowns (kept as a constant list)
        public static readonly string[] OrgTypes =
        {
            "NGO",
            "Bangladesh Red Crescent",
            "University Volunteer Team",
            "Local Community Relief Team",
            "Government Agency",
            "Other"
        };

        public OrganizationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // LIST — search, filter, sort, pagination
        // Accessible to all authenticated users
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? orgType,
            string? status,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 8;

            var query = _context.Organizations.AsQueryable();

            // Super Admins see all statuses; others only see Approved
            bool isSuperAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");

            if (!isSuperAdmin)
            {
                // Non-admins can only browse approved organizations
                query = query.Where(o => o.Status == "Approved");
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o =>
                    o.OrganizationName.Contains(search) ||
                    o.OrganizationType.Contains(search) ||
                    o.ServiceArea.Contains(search));
            }

            // Filter by type
            if (!string.IsNullOrWhiteSpace(orgType))
            {
                query = query.Where(o => o.OrganizationType == orgType);
            }

            // Filter by status (Super Admin only)
            if (isSuperAdmin && !string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Sorting
            query = sortOrder switch
            {
                "name_asc"    => query.OrderBy(o => o.OrganizationName),
                "name_desc"   => query.OrderByDescending(o => o.OrganizationName),
                "status_asc"  => query.OrderBy(o => o.Status),
                "status_desc" => query.OrderByDescending(o => o.Status),
                "oldest"      => query.OrderBy(o => o.CreatedAt),
                _             => query.OrderByDescending(o => o.CreatedAt) // newest first
            };

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var organizations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pass ViewBag values for maintaining state in search/filter/sort controls
            ViewBag.Search    = search;
            ViewBag.OrgType   = orgType;
            ViewBag.Status    = status;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages  = totalPages;
            ViewBag.TotalCount  = totalCount;
            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.OrgTypes  = OrgTypes;

            return View(organizations);
        }

        // ================================================================
        // CREATE — GET
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin,Organization Admin,Organization")]
        public IActionResult Create()
        {
            ViewBag.OrgTypes = OrgTypes;
            return View(new OrganizationViewModel());
        }

        // ================================================================
        // CREATE — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Organization Admin,Organization")]
        public async Task<IActionResult> Create(OrganizationViewModel model)
        {
            ViewBag.OrgTypes = OrgTypes;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Server-side: validate org type
            if (!OrgTypes.Contains(model.OrganizationType))
            {
                ModelState.AddModelError("OrganizationType", "Invalid organization type selected.");
                return View(model);
            }

            // Server-side duplicate name check
            bool nameExists = await _context.Organizations
                .AnyAsync(o => o.OrganizationName.ToLower() == model.OrganizationName.Trim().ToLower());
            if (nameExists)
            {
                ModelState.AddModelError("OrganizationName", "An organization with this name already exists.");
                return View(model);
            }

            // Server-side duplicate email check
            bool emailExists = await _context.Organizations
                .AnyAsync(o => o.ContactEmail.ToLower() == model.ContactEmail.Trim().ToLower());
            if (emailExists)
            {
                ModelState.AddModelError("ContactEmail", "This contact email is already registered by another organization.");
                return View(model);
            }

            var currentUserId = _userManager.GetUserId(User);

            var org = new Organization
            {
                OrganizationName = model.OrganizationName.Trim(),
                OrganizationType = model.OrganizationType,
                Description      = model.Description.Trim(),
                ContactEmail     = model.ContactEmail.Trim(),
                ContactPhone     = model.ContactPhone.Trim(),
                Address          = model.Address.Trim(),
                ServiceArea      = model.ServiceArea.Trim(),
                Status           = "Pending",
                CreatedAt        = DateTime.UtcNow,
                ManagedByUserId  = currentUserId
            };

            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
    $"Organization \"{org.OrganizationName}\" registered successfully! It is pending Super Admin approval.";

            return RedirectToAction("Organization", "Dashboard");
        }

        // ================================================================
        // DETAILS
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var org = await _context.Organizations
                .Include(o => o.ManagedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            // Non-admin users can only view Approved organizations
            bool isSuperAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");
            bool isOwner = org.ManagedByUserId == _userManager.GetUserId(User);

            if (!isSuperAdmin && !isOwner && org.Status != "Approved")
            {
                TempData["ErrorMessage"] = "This organization is not available for viewing.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.IsOwner = isOwner;

            return View(org);
        }

        // ================================================================
        // EDIT — GET
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            // Only the owner or Super Admin can edit
            if (!CanEdit(org))
            {
                return Forbid();
            }

            ViewBag.OrgTypes = OrgTypes;

            var vm = new OrganizationViewModel
            {
                Id               = org.Id,
                OrganizationName = org.OrganizationName,
                OrganizationType = org.OrganizationType,
                Description      = org.Description,
                ContactEmail     = org.ContactEmail,
                ContactPhone     = org.ContactPhone,
                Address          = org.Address,
                ServiceArea      = org.ServiceArea
            };

            return View(vm);
        }

        // ================================================================
        // EDIT — POST
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrganizationViewModel model)
        {
            ViewBag.OrgTypes = OrgTypes;

            if (id != model.Id)
            {
                return NotFound();
            }

            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            // Server-side authorization: only owner or Super Admin
            if (!CanEdit(org))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate org type
            if (!OrgTypes.Contains(model.OrganizationType))
            {
                ModelState.AddModelError("OrganizationType", "Invalid organization type selected.");
                return View(model);
            }

            // Duplicate name check (exclude self)
            bool nameExists = await _context.Organizations
                .AnyAsync(o => o.OrganizationName.ToLower() == model.OrganizationName.Trim().ToLower()
                            && o.Id != id);
            if (nameExists)
            {
                ModelState.AddModelError("OrganizationName", "An organization with this name already exists.");
                return View(model);
            }

            // Duplicate email check (exclude self)
            bool emailExists = await _context.Organizations
                .AnyAsync(o => o.ContactEmail.ToLower() == model.ContactEmail.Trim().ToLower()
                            && o.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError("ContactEmail", "This contact email is already registered by another organization.");
                return View(model);
            }

            org.OrganizationName = model.OrganizationName.Trim();
            org.OrganizationType = model.OrganizationType;
            org.Description      = model.Description.Trim();
            org.ContactEmail     = model.ContactEmail.Trim();
            org.ContactPhone     = model.ContactPhone.Trim();
            org.Address          = model.Address.Trim();
            org.ServiceArea      = model.ServiceArea.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Organization profile updated successfully.";
            return RedirectToAction(nameof(Details), new { id = org.Id });
        }

        // ================================================================
        // APPROVE — POST (Super Admin only)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            if (org.Status == "Approved")
            {
                TempData["SuccessMessage"] = $"\"{org.OrganizationName}\" is already approved.";
                return RedirectToAction(nameof(Details), new { id });
            }

            org.Status = "Approved";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Organization \"{org.OrganizationName}\" has been approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // REJECT — POST (Super Admin only)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            if (org.Status == "Rejected")
            {
                TempData["SuccessMessage"] = $"\"{org.OrganizationName}\" is already rejected.";
                return RedirectToAction(nameof(Details), new { id });
            }

            org.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Organization \"{org.OrganizationName}\" has been rejected.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // DELETE — GET (Super Admin only, Pending/Rejected organizations)
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            if (org.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Approved organizations cannot be deleted to preserve data integrity.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(org);
        }

        // ================================================================
        // DELETE — POST (Super Admin only)
        // ================================================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization not found.";
                return RedirectToAction(nameof(Index));
            }

            if (org.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Approved organizations cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Organizations.Remove(org);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Organization \"{org.OrganizationName}\" has been deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // MY ORGANIZATION – redirects Organization Admin to their own org
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Organization Admin,Organization")]
        public async Task<IActionResult> MyOrganization()
        {
            var userId = _userManager.GetUserId(User);
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (org == null)
            {
                TempData["SuccessMessage"] = null;
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Details), new { id = org.Id });
        }

        // ================================================================
        // AJAX – Check organization name availability
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> CheckNameAvailability(string name, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { available = false, message = "Name is required." });
            }

            var trimmed = name.Trim();
            var query = _context.Organizations
                .Where(o => o.OrganizationName.ToLower() == trimmed.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(o => o.Id != excludeId.Value);
            }

            bool exists = await query.AnyAsync();
            return exists
                ? Json(new { available = false, message = "This organization name is already taken." })
                : Json(new { available = true, message = "Organization name is available." });
        }

        // ================================================================
        // PUBLIC LIST — No login required: shows all approved organizations
        // ================================================================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicList()
        {
            var orgs = await _context.Organizations
                .Where(o => o.Status == "Approved")
                .OrderBy(o => o.OrganizationName)
                .ToListAsync();

            return View(orgs);
        }

        // ================================================================
        // PUBLIC ACTIVITIES — Accountability Dashboard
        // No login required
        // Shows Active, Completed and Pending activities
        // ================================================================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicActivities(
            string? location,
            string? orgName,
            string? status,
            string? type)
        {
            var query = _context.DisasterOperations
                .Include(d => d.LeadOrganization)
                .AsQueryable();

            // ------------------------------------------------------------
            // Publicly visible activities
            // ------------------------------------------------------------
            query = query.Where(d =>
                d.Status == "Active" ||
                d.Status == "Completed" ||
                d.Status == "Pending");

            // ------------------------------------------------------------
            // Location filter
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(location))
            {
                location = location.Trim();

                query = query.Where(d =>
                    d.Location != null &&
                    d.Location.Contains(location));
            }

            // ------------------------------------------------------------
            // Organization filter
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(orgName))
            {
                orgName = orgName.Trim();

                query = query.Where(d =>
                    d.LeadOrganization != null &&
                    d.LeadOrganization.OrganizationName.Contains(orgName));
            }

            // ------------------------------------------------------------
            // Status filter
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim();

                query = query.Where(d =>
                    d.Status == status);
            }

            // ------------------------------------------------------------
            // Activity type filter
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(type))
            {
                type = type.Trim();

                query = query.Where(d =>
                    d.DisasterType != null &&
                    d.DisasterType.Contains(type));
            }

            var operations = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            // ------------------------------------------------------------
            // Load partner organizations
            // ------------------------------------------------------------
            var operationIds = operations
                .Select(o => o.DisasterOperationId)
                .ToList();

            var partnerLinks = await _context.OrganizationOperations
                .Include(oo => oo.Organization)
                .Where(oo =>
                    operationIds.Contains(oo.DisasterOperationId) &&
                    oo.ParticipationStatus == "Active")
                .ToListAsync();

            ViewBag.PartnerMap = partnerLinks
                .GroupBy(oo => oo.DisasterOperationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(oo => oo.Organization).ToList()
                );

            ViewBag.FilterLocation = location;
            ViewBag.FilterStatus = status;
            ViewBag.FilterType = type;
            ViewBag.FilterOrgName = orgName;

            return View(operations);
        }

        // ================================================================
        // ACTIVITIES — Org-scoped
        // Organization sees:
        // 1. Activities created by its organization
        // 2. Activities where its organization is a participating partner
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Organization Admin,Organization,Coordinator,Super Admin,Admin")]
        public async Task<IActionResult> Activities()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            IQueryable<DisasterOperation> query = _context.DisasterOperations
                .Include(d => d.LeadOrganization)
                .AsQueryable();

            // ------------------------------------------------------------
            // Organization Admin / Organization
            // ------------------------------------------------------------
            if (User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization"))
            {
                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o =>
                        o.ManagedByUserId == user.Id &&
                        o.Status == "Approved");

                if (myOrg == null)
                {
                    TempData["ErrorMessage"] =
                        "You are not managing an approved organization.";

                    return RedirectToAction("Index", "Dashboard");
                }

                // Show:
                // - activities created by this organization
                // - activities where this organization is a partner
                query = query.Where(d =>
                    d.CreatedByOrganizationId == myOrg.Id
                    ||
                    _context.OrganizationOperations.Any(oo =>
                        oo.DisasterOperationId == d.DisasterOperationId &&
                        oo.OrganizationId == myOrg.Id
                    )
                );

                ViewBag.MyOrgId = myOrg.Id;
                ViewBag.MyOrgName = myOrg.OrganizationName;
            }

            var ops = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(ops);
        }

        // ================================================================
        // CREATE OPERATION — Org Admin creates a relief operation/activity
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Organization Admin,Organization,Super Admin,Admin")]
        public async Task<IActionResult> CreateOperation()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Org admins can only create for their own org
            if (User.IsInRole("Organization Admin") || User.IsInRole("Organization"))
            {
                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.ManagedByUserId == user.Id);
                if (myOrg == null)
                {
                    TempData["ErrorMessage"] = "You must manage an approved organization to create activities.";
                    return RedirectToAction("Index", "Dashboard");
                }
                ViewBag.MyOrgId   = myOrg.Id;
                ViewBag.MyOrgName = myOrg.OrganizationName;
            }
            else
            {
                ViewBag.Organizations = await _context.Organizations
                    .Where(o => o.Status == "Approved").ToListAsync();
            }

            return View(new DisasterOperation
            {
                StartDate = DateTime.UtcNow,
                Status    = "Active"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organization Admin,Organization,Super Admin,Admin")]
        public async Task<IActionResult> CreateOperation(DisasterOperation model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Org admins: auto-assign their org
            if (User.IsInRole("Organization Admin") || User.IsInRole("Organization"))
            {
                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.ManagedByUserId == user.Id);
                if (myOrg == null) return Forbid();
                model.CreatedByOrganizationId = myOrg.Id;
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("Organization Admin") || User.IsInRole("Organization"))
                {
                    var myOrg2 = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.ManagedByUserId == user.Id);
                    ViewBag.MyOrgId = myOrg2?.Id;
                    ViewBag.MyOrgName = myOrg2?.OrganizationName;
                }
                else
                {
                    ViewBag.Organizations = await _context.Organizations
                        .Where(o => o.Status == "Approved").ToListAsync();
                }
                return View(model);
            }

            // Normalize StartDate to UTC
            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            if (model.EndDate.HasValue)
                model.EndDate = DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc);

            model.Status = "Active";
            model.CreatedAt = DateTime.UtcNow;

            _context.DisasterOperations.Add(model);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Activity could not be saved: " + ex.Message;

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Activity '{model.OperationName}' created successfully. ID: {model.DisasterOperationId}";

            return RedirectToAction(nameof(Activities));
        }

            // ================================================================
            // OPERATION DETAILS — Public (no login required)
            // ================================================================
            [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> OperationDetails(int id)
        {
            var op = await _context.DisasterOperations
                .Include(d => d.LeadOrganization)
                .FirstOrDefaultAsync(d => d.DisasterOperationId == id);

            if (op == null) return NotFound();

            var partners = await _context.OrganizationOperations
                .Include(oo => oo.Organization)
                .Where(oo => oo.DisasterOperationId == id)
                .ToListAsync();

            ViewBag.Partners = partners;

            // Related help requests
            var relatedRequests = await _context.HelpRequests
                .Where(h => h.DisasterOperationId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.RelatedRequests = relatedRequests;

            return View(op);
        }

        // ================================================================
        // COORDINATION REQUESTS — Super Admin management panel
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> CoordinationRequests(string? status)
        {
            var query = _context.DisasterOperations
                .Include(d => d.LeadOrganization)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status == status);

            var ops = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

            var allPartners = await _context.OrganizationOperations
                .Include(oo => oo.Organization)
                .ToListAsync();

            ViewBag.PartnerMap = allPartners
                .GroupBy(oo => oo.DisasterOperationId)
                .ToDictionary(g => g.Key, g => g.Select(oo => oo.Organization).ToList());

            ViewBag.FilterStatus = status;
            return View(ops);
        }

        // ================================================================
        // UPDATE OPERATION STATUS — Super Admin only
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> UpdateOperationStatus(int id, string newStatus)
        {
            string[] valid = { "Pending", "Active", "Completed", "Cancelled" };
            if (!valid.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(CoordinationRequests));
            }

            var op = await _context.DisasterOperations.FindAsync(id);
            if (op == null) return NotFound();

            op.Status = newStatus;
            if (newStatus == "Completed" && !op.EndDate.HasValue)
                op.EndDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Operation status updated to \"{newStatus}\".";
            return RedirectToAction(nameof(CoordinationRequests));
        }

        // ================================================================
        // ADD PARTNER ORG TO OPERATION — Super Admin / Org Admin
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Organization Admin")]
        public async Task<IActionResult> AddPartnerOrg(int operationId, int organizationId, string? assignedArea)
        {
            // Check the operation exists
            var op = await _context.DisasterOperations.FindAsync(operationId);
            if (op == null) return NotFound();

            // Check org exists and is approved
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org == null || org.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Organization not found or not approved.";
                return RedirectToAction(nameof(OperationDetails), new { id = operationId });
            }

            // Avoid duplicates
            var existing = await _context.OrganizationOperations
                .FirstOrDefaultAsync(oo => oo.DisasterOperationId == operationId
                                         && oo.OrganizationId == organizationId);
            if (existing != null)
            {
                TempData["ErrorMessage"] = "This organization is already participating.";
                return RedirectToAction(nameof(OperationDetails), new { id = operationId });
            }

            _context.OrganizationOperations.Add(new OrganizationOperation
            {
                DisasterOperationId = operationId,
                OrganizationId      = organizationId,
                ParticipationStatus = "Active",
                AssignedArea        = assignedArea?.Trim(),
                JoinedAt            = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{org.OrganizationName} added as a partner organization.";
            return RedirectToAction(nameof(OperationDetails), new { id = operationId });
        }

        // ================================================================
        // PRIVATE HELPERS
        // ================================================================

        /// <summary>
        /// Returns true if the current user is allowed to edit this organization.
        /// </summary>
        private bool CanEdit(Organization org)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Admin"))
                return true;

            var userId = _userManager.GetUserId(User);
            return org.ManagedByUserId == userId;
        }
    }
}

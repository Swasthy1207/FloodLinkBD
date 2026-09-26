using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize(Roles = "Organization Admin,Organization,Coordinator,Admin,Super Admin,Volunteer")]
    public class HelpCoordinationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HelpCoordinationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // INDEX
        // Open help needs from OTHER organizations
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.OrganizationHelpNeeds
                .Include(h => h.Organization)
                .Where(h =>
                    h.Status == "Pending" ||
                    h.Status == "Under Review" ||
                    h.Status == "In Progress")
                .AsQueryable();

            if (User.IsInRole("Organization Admin") ||
                User.IsInRole("Organization"))
            {
                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

                if (myOrg != null)
                {
                    query = query.Where(h =>
                        h.OrganizationId != myOrg.Id);
                }
            }

            var needs = await query
                .OrderByDescending(h => h.Priority == "High")
                .ThenByDescending(h => h.CreatedAt)
                .ToListAsync();

            return View(needs);
        }

        // ============================================================
        // DETAILS
        // Shows help need + offers
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var need = await _context.OrganizationHelpNeeds
                .Include(h => h.Organization)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (need == null)
                return NotFound();

            var offers = await _context.OrganizationHelpOffers
                .Include(o => o.OfferingOrganization)
                .Where(o => o.HelpNeedId == id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Offers = offers;

            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            ViewBag.MyOrganizationId = myOrg?.Id;

            return View(need);
        }

        // ============================================================
        // OFFER HELP
        // GET
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> OfferHelp(int id)
        {
            var need = await _context.OrganizationHelpNeeds
                .Include(h => h.Organization)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (need == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
            {
                TempData["ErrorMessage"] =
                    "Your user account is not connected to an organization.";

                return RedirectToAction(nameof(Index));
            }

            if (myOrg.Id == need.OrganizationId)
            {
                TempData["ErrorMessage"] =
                    "You cannot offer help to your own organization.";

                return RedirectToAction(nameof(Details), new { id });
            }

            return View(need);
        }

        // ============================================================
        // OFFER HELP
        // POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OfferHelp(
            int id,
            string offeredHelpType,
            string? offeredQuantity,
            string? message)
        {
            if (string.IsNullOrWhiteSpace(offeredHelpType))
            {
                TempData["ErrorMessage"] =
                    "Please specify what help your organization can provide.";

                return RedirectToAction(nameof(OfferHelp), new { id });
            }

            var need = await _context.OrganizationHelpNeeds
                .FirstOrDefaultAsync(h => h.Id == id);

            if (need == null)
                return NotFound();

            if (need.Status == "Resolved" ||
                need.Status == "Rejected")
            {
                TempData["ErrorMessage"] =
                    "This help need is no longer open.";

                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
            {
                TempData["ErrorMessage"] =
                    "Your account is not connected to an organization.";

                return RedirectToAction(nameof(Index));
            }

            // Cannot help own organization
            if (myOrg.Id == need.OrganizationId)
            {
                TempData["ErrorMessage"] =
                    "You cannot offer help to your own organization.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Organization must be approved
            if (!string.Equals(myOrg.Status, "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only approved organizations can offer help.";

                return RedirectToAction(nameof(Index));
            }

            // Prevent duplicate pending offer
            var existingOffer =
                await _context.OrganizationHelpOffers
                    .AnyAsync(o =>
                        o.HelpNeedId == id &&
                        o.OfferingOrganizationId == myOrg.Id &&
                        o.Status == "Pending");

            if (existingOffer)
            {
                TempData["ErrorMessage"] =
                    "Your organization already has a pending offer for this request.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var offer = new OrganizationHelpOffer
            {
                HelpNeedId = id,
                OfferingOrganizationId = myOrg.Id,
                OfferedHelpType = offeredHelpType.Trim(),
                OfferedQuantity = string.IsNullOrWhiteSpace(offeredQuantity)
                    ? null
                    : offeredQuantity.Trim(),
                Message = string.IsNullOrWhiteSpace(message)
                    ? null
                    : message.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.OrganizationHelpOffers.Add(offer);

            // Move request to Under Review when first offer arrives
            if (need.Status == "Pending")
            {
                need.Status = "Under Review";
                need.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your help offer has been submitted successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ============================================================
        // MY OFFERS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> MyOffers()
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return View(new List<OrganizationHelpOffer>());

            var offers = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                    .ThenInclude(h => h.Organization)
                .Where(o =>
                    o.OfferingOrganizationId == myOrg.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(offers);
        }

        // ================================================================
        // MY HELP NEEDS — Organization's own requests
        // ================================================================
        [HttpGet]
        [Authorize(Roles = "Organization Admin,Organization")]
        public async Task<IActionResult> MyHelpNeeds()
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
            {
                TempData["ErrorMessage"] =
                    "Your account is not connected to an organization.";

                return RedirectToAction(nameof(Index));
            }

            var needs = await _context.OrganizationHelpNeeds
                .Where(h => h.OrganizationId == myOrg.Id)
                .Include(h => h.Organization)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            return View(needs);
        }

        // ============================================================
        // ACCEPT OFFER
        // Only requesting organization can accept
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(int id)
        {
            var offer = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                .FirstOrDefaultAsync(o => o.HelpOfferId == id);

            if (offer == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return Forbid();

            // Only organization that requested help can accept
            if (offer.HelpNeed.OrganizationId != myOrg.Id)
                return Forbid();

            if (offer.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This offer is no longer pending.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = offer.HelpNeedId });
            }

            // Accept selected offer
            offer.Status = "Accepted";
            offer.RespondedAt = DateTime.UtcNow;

            // Mark help need as accepted
            offer.HelpNeed.Status = "Accepted";
            offer.HelpNeed.UpdatedAt = DateTime.UtcNow;

            // Other pending offers are rejected because
            // one organization has been selected for this need.
            var otherOffers =
                await _context.OrganizationHelpOffers
                    .Where(o =>
                        o.HelpNeedId == offer.HelpNeedId &&
                        o.HelpOfferId != offer.HelpOfferId &&
                        o.Status == "Pending")
                    .ToListAsync();

            foreach (var other in otherOffers)
            {
                other.Status = "Rejected";
                other.RespondedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Help offer accepted. The offering organization can now assign its own volunteers.";

            return RedirectToAction(
                nameof(Details),
                new { id = offer.HelpNeedId });
        }



        // ============================================================
        // REJECT OFFER
        // Only requesting organization can reject
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOffer(int id)
        {
            var offer = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                .FirstOrDefaultAsync(o => o.HelpOfferId == id);

            if (offer == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return Forbid();

            if (offer.HelpNeed.OrganizationId != myOrg.Id)
                return Forbid();

            if (offer.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This offer is no longer pending.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = offer.HelpNeedId });
            }

            offer.Status = "Rejected";
            offer.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Help offer rejected.";

            return RedirectToAction(
                nameof(Details),
                new { id = offer.HelpNeedId });
        }

        // ============================================================
        // WITHDRAW OFFER
        // Offering organization can withdraw its own pending offer
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawOffer(int id)
        {
            var offer = await _context.OrganizationHelpOffers
                .FirstOrDefaultAsync(o => o.HelpOfferId == id);

            if (offer == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return Forbid();

            if (offer.OfferingOrganizationId != myOrg.Id)
                return Forbid();

            if (offer.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending offers can be withdrawn.";

                return RedirectToAction(nameof(MyOffers));
            }

            offer.Status = "Withdrawn";
            offer.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Help offer withdrawn.";

            return RedirectToAction(nameof(MyOffers));
        }

        // ============================================================
        // COORDINATOR DASHBOARD
        // Coordinator sees ALL help needs and ALL offers
        // ============================================================
        [Authorize(Roles = "Coordinator,Admin,Super Admin")]
        [HttpGet]
        public async Task<IActionResult> Coordinator()
        {
            var needs = await _context.OrganizationHelpNeeds
                .Include(h => h.Organization)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            var offers = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                    .ThenInclude(h => h.Organization)
                .Include(o => o.OfferingOrganization)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Needs = needs;
            ViewBag.Offers = offers;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AcceptedOffers()
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
            {
                TempData["ErrorMessage"] =
                    "Your account is not connected to an organization.";

                return RedirectToAction(nameof(Index));
            }

            var offers = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                    .ThenInclude(h => h.Organization)
                .Where(o =>
                    o.OfferingOrganizationId == myOrg.Id &&
                    o.Status == "Accepted")
                .OrderByDescending(o => o.RespondedAt)
                .ToListAsync();

            return View(offers);
        }
        [HttpGet]
        public async Task<IActionResult> AssignVolunteer(int id)
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return Forbid();

            var offer = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                    .ThenInclude(h => h.Organization)
                .FirstOrDefaultAsync(o => o.HelpOfferId == id);

            if (offer == null)
                return NotFound();

            // Only the organization that offered the help
            // can assign its volunteers.
            if (offer.OfferingOrganizationId != myOrg.Id)
                return Forbid();

            if (offer.Status != "Accepted")
            {
                TempData["ErrorMessage"] =
                    "Only accepted help offers can have volunteers assigned.";

                return RedirectToAction(nameof(AcceptedOffers));
            }

            var volunteers = await _context.VolunteerProfiles
                .Include(v => v.User)
                .Where(v =>
                    v.OrganizationId == myOrg.Id &&
                    v.IsApprovedByOrg &&
                    v.AvailabilityStatus == "Available")
                .OrderBy(v => v.User!.FullName)
                .ToListAsync();

            ViewBag.Offer = offer;

            return View(volunteers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignVolunteer(
    int id,
    int volunteerProfileId,
    string? notes)
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return Forbid();

            var offer = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                .FirstOrDefaultAsync(o => o.HelpOfferId == id);

            if (offer == null)
                return NotFound();

            // Security: only the offering organization
            // can assign its volunteers.
            if (offer.OfferingOrganizationId != myOrg.Id)
                return Forbid();

            if (offer.Status != "Accepted")
            {
                TempData["ErrorMessage"] =
                    "This help offer is not accepted.";

                return RedirectToAction(nameof(AcceptedOffers));
            }

            var volunteer = await _context.VolunteerProfiles
                .Include(v => v.User)
                .FirstOrDefaultAsync(v =>
                    v.Id == volunteerProfileId &&
                    v.OrganizationId == myOrg.Id);

            if (volunteer == null)
            {
                TempData["ErrorMessage"] =
                    "Volunteer not found or does not belong to your organization.";

                return RedirectToAction(nameof(AssignVolunteer), new { id });
            }

            if (!volunteer.IsApprovedByOrg)
            {
                TempData["ErrorMessage"] =
                    "This volunteer has not been approved by your organization.";

                return RedirectToAction(nameof(AssignVolunteer), new { id });
            }

            if (volunteer.AvailabilityStatus != "Available")
            {
                TempData["ErrorMessage"] =
                    "This volunteer is not currently available.";

                return RedirectToAction(nameof(AssignVolunteer), new { id });
            }

            // Prevent duplicate assignment
            var alreadyAssigned =
                await _context.OrganizationHelpOfferAssignments
                    .AnyAsync(a =>
                        a.HelpOfferId == id &&
                        a.VolunteerProfileId == volunteerProfileId &&
                        a.Status != "Completed" &&
                        a.Status != "Cancelled");

            if (alreadyAssigned)
            {
                TempData["ErrorMessage"] =
                    "This volunteer is already assigned to this help offer.";

                return RedirectToAction(nameof(AssignVolunteer), new { id });
            }

            var assignment = new OrganizationHelpOfferAssignment
            {
                HelpOfferId = id,
                VolunteerProfileId = volunteerProfileId,
                AssignedByUserId = userId!,
                Status = "Assigned",
                Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim(),
                AssignedAt = DateTime.UtcNow
            };

            _context.OrganizationHelpOfferAssignments.Add(assignment);

            // Volunteer becomes busy
            volunteer.AvailabilityStatus = "Busy";
            volunteer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{volunteer.User?.FullName ?? "Volunteer"} has been assigned successfully.";

            return RedirectToAction(nameof(AssignVolunteer), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> OfferAssignments(int id)
        {
            var userId = _userManager.GetUserId(User);

            var myOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

            if (myOrg == null)
                return Forbid();

            var offer = await _context.OrganizationHelpOffers
                .Include(o => o.HelpNeed)
                    .ThenInclude(h => h.Organization)
                .FirstOrDefaultAsync(o => o.HelpOfferId == id);

            if (offer == null)
                return NotFound();

            if (offer.OfferingOrganizationId != myOrg.Id)
                return Forbid();

            var assignments = await _context.OrganizationHelpOfferAssignments
                .Include(a => a.VolunteerProfile)
                    .ThenInclude(v => v!.User)
                .Include(a => a.AssignedByUser)
                .Where(a => a.HelpOfferId == id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            ViewBag.Offer = offer;

            return View(assignments);
        }

        [Authorize(Roles = "Volunteer")]
        [HttpGet]
        public async Task<IActionResult> MyHelpAssignments()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var volunteer = await _context.VolunteerProfiles
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                TempData["ErrorMessage"] =
                    "Your volunteer profile was not found.";

                return View(new List<OrganizationHelpOfferAssignment>());
            }

            var assignments = await _context.OrganizationHelpOfferAssignments
                .Include(a => a.HelpOffer)
                    .ThenInclude(o => o!.HelpNeed)
                        .ThenInclude(h => h!.Organization)
                .Include(a => a.HelpOffer)
                    .ThenInclude(o => o!.OfferingOrganization)
                .Where(a => a.VolunteerProfileId == volunteer.Id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return View(assignments);
        }

        [Authorize(Roles = "Volunteer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHelpAssignmentStatus(
    int id,
    string status,
    string? note)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var volunteer = await _context.VolunteerProfiles
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
                return Forbid();

            var assignment =
                await _context.OrganizationHelpOfferAssignments
                    .Include(a => a.HelpOffer)
                        .ThenInclude(o => o!.HelpNeed)
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
                return NotFound();

            // Important security check:
            // volunteer can update ONLY their own assignment.
            if (assignment.VolunteerProfileId != volunteer.Id)
                return Forbid();

            var allowedStatuses = new[]
            {
        "Assigned",
        "In Progress",
        "Completed",
        "Cancelled"
    };

            if (!allowedStatuses.Contains(status))
            {
                TempData["ErrorMessage"] =
                    "Invalid assignment status.";

                return RedirectToAction(nameof(MyHelpAssignments));
            }

            assignment.Status = status;

            if (!string.IsNullOrWhiteSpace(note))
            {
                assignment.Notes = note.Trim();
            }

            if (status == "In Progress" &&
                !assignment.StartedAt.HasValue)
            {
                assignment.StartedAt = DateTime.UtcNow;
            }

            if (status == "Completed")
            {
                assignment.CompletedAt = DateTime.UtcNow;

                // Make volunteer available again.
                volunteer.AvailabilityStatus = "Available";
                volunteer.UpdatedAt = DateTime.UtcNow;
            }

            if (status == "Cancelled")
            {
                volunteer.AvailabilityStatus = "Available";
                volunteer.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Assignment status updated to '{status}'.";

            return RedirectToAction(nameof(MyHelpAssignments));
        }
    }


}
using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FloodLink.Controllers
{
    [Authorize]
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VolunteerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Volunteer/Index
        // Organization Admin -> only own approved volunteers
        // Coordinator/Admin -> volunteers across organizations
        [Authorize(Roles = "Coordinator,Organization Admin,Super Admin,Admin")]
        public async Task<IActionResult> Index(
            string? search,
            string? skills,
            string? status)
        {
            ViewBag.Search = search;
            ViewBag.Skills = skills;
            ViewBag.Status = status;

            var query = _context.VolunteerProfiles
                .Include(v => v.User)
                .Include(v => v.Organization)
                .Where(v => v.IsApprovedByOrg)
                .AsQueryable();

            // Organization Admin sees ONLY their own organization's volunteers
            if (User.IsInRole("Organization Admin"))
            {
                var userId = _userManager.GetUserId(User);

                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

                if (myOrg == null)
                {
                    TempData["ErrorMessage"] =
                        "You do not manage any organization.";

                    return RedirectToAction("Index", "Dashboard");
                }

                query = query.Where(v => v.OrganizationId == myOrg.Id);

                ViewBag.MyOrganizationId = myOrg.Id;
                ViewBag.MyOrganizationName = myOrg.OrganizationName;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v =>
                    (v.User != null &&
                     v.User.FullName.Contains(search)) ||

                    (v.User != null &&
                     v.User.Email != null &&
                     v.User.Email.Contains(search)) ||

                    (v.CurrentLocation != null &&
                     v.CurrentLocation.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(skills))
            {
                query = query.Where(v =>
                    v.Skills != null &&
                    v.Skills.Contains(skills));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v =>
                    v.AvailabilityStatus == status);
            }

            var volunteers = await query
                .OrderBy(v => v.AvailabilityStatus == "Available" ? 0 : 1)
                .ThenBy(v => v.User!.FullName)
                .ToListAsync();

            ViewBag.AvailableCount =
                volunteers.Count(v => v.AvailabilityStatus == "Available");

            ViewBag.BusyCount =
                volunteers.Count(v => v.AvailabilityStatus == "Busy");

            return View(volunteers);
        }

        // GET: /Volunteer/MyProfile
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.VolunteerProfiles
                .Include(v => v.Organization)
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            if (profile == null)
            {
                profile = new VolunteerProfile { UserId = user.Id, User = user };
            }

            ViewBag.Organizations = await _context.Organizations.Where(o => o.Status == "Approved").ToListAsync();
            return View(profile);
        }

        // POST: /Volunteer/MyProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> MyProfile(VolunteerProfile model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var existingProfile = await _context.VolunteerProfiles.FirstOrDefaultAsync(v => v.UserId == user.Id);

            if (existingProfile == null)
            {
                model.UserId = user.Id;
                if (model.OrganizationId.HasValue)
                {
                    model.IsApprovedByOrg = false; // Pending approval by the org
                }
                _context.VolunteerProfiles.Add(model);
            }
            else
            {
                existingProfile.Skills = model.Skills;
                existingProfile.CurrentLocation = model.CurrentLocation;
                existingProfile.AvailabilityStatus = model.AvailabilityStatus;
                existingProfile.UpdatedAt = DateTime.UtcNow;
                
                // If org changed, reset approval status
                if (existingProfile.OrganizationId != model.OrganizationId)
                {
                    existingProfile.OrganizationId = model.OrganizationId;
                    existingProfile.IsApprovedByOrg = false;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(MyProfile));
        }

        // GET: /Volunteer/ManageAffiliations
        [Authorize(Roles = "Organization Admin")]
        public async Task<IActionResult> ManageAffiliations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var myOrg = await _context.Organizations.FirstOrDefaultAsync(o => o.ManagedByUserId == user.Id);
            if (myOrg == null)
            {
                TempData["ErrorMessage"] = "You do not manage any organization.";
                return RedirectToAction("Index", "Dashboard");
            }

            var pendingVolunteers = await _context.VolunteerProfiles
                .Include(v => v.User)
                .Where(v => v.OrganizationId == myOrg.Id && !v.IsApprovedByOrg)
                .ToListAsync();

            var approvedVolunteers = await _context.VolunteerProfiles
                .Include(v => v.User)
                .Where(v => v.OrganizationId == myOrg.Id && v.IsApprovedByOrg)
                .ToListAsync();

            ViewBag.ApprovedVolunteers = approvedVolunteers;
            return View(pendingVolunteers);
        }

        // POST: /Volunteer/ApproveAffiliation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organization Admin")]
        public async Task<IActionResult> ApproveAffiliation(int profileId, string action)
        {
            var user = await _userManager.GetUserAsync(User);
            var myOrg = await _context.Organizations.FirstOrDefaultAsync(o => user != null && o.ManagedByUserId == user.Id);
            if (myOrg == null) return Unauthorized();

            var profile = await _context.VolunteerProfiles.FindAsync(profileId);
            if (profile == null || profile.OrganizationId != myOrg.Id) return NotFound();

            if (action == "Approve")
            {
                profile.IsApprovedByOrg = true;
                TempData["SuccessMessage"] = "Volunteer approved.";
            }
            else if (action == "Reject")
            {
                profile.OrganizationId = null;
                profile.IsApprovedByOrg = false;
                TempData["SuccessMessage"] = "Volunteer request rejected.";
            }

            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageAffiliations));
        }

        // GET: /Volunteer/Activity/5
        // Organization Admin can see activity of volunteers belonging
        // to their own organization.
        [Authorize(Roles = "Organization Admin,Coordinator,Super Admin,Admin")]
        public async Task<IActionResult> Activity(int id)
        {
            var profile = await _context.VolunteerProfiles
                .Include(v => v.User)
                .Include(v => v.Organization)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (profile == null)
                return NotFound();

            // Organization Admin can only view own organization's volunteers
            if (User.IsInRole("Organization Admin"))
            {
                var userId = _userManager.GetUserId(User);

                var myOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.ManagedByUserId == userId);

                if (myOrg == null || profile.OrganizationId != myOrg.Id)
                    return Forbid();
            }

            var requests = await _context.HelpRequests
                .Where(r =>
                    r.AssignedVolunteerId == profile.UserId ||
                    r.VolunteerEmail == profile.User!.Email)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var requestIds = requests
                .Select(r => r.Id)
                .ToList();

            var logs = await _context.HelpRequestStatusLogs
                .Include(l => l.ChangedBy)
                .Where(l => requestIds.Contains(l.HelpRequestId))
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();

            var vm = new VolunteerActivityViewModel
            {
                Volunteer = profile,
                AssignedRequests = requests,
                StatusLogs = logs
                    .GroupBy(l => l.HelpRequestId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList())
            };

            return View(vm);
        }
    }
}

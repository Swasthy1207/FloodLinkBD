using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FloodLink.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.ApprovedOrgsCount = await _context.Organizations.CountAsync(o => o.Status == "Approved");
                ViewBag.ActiveOperationsCount = await _context.DisasterOperations.CountAsync(o => o.Status == "Active" || o.Status == "Completed");
                ViewBag.TotalHelpRequestsCount = await _context.HelpRequests.CountAsync();
                ViewBag.ResolvedHelpRequestsCount = await _context.HelpRequests.CountAsync(r => r.Status == "Resolved" || r.Status == "Completed");
                ViewBag.SheltersCount = await _context.Shelters.CountAsync(s => s.Status != "Closed");

                ViewBag.RecentAnnouncements = await _context.Announcements
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                ViewBag.RecentOperations = await _context.DisasterOperations
                    .Include(d => d.LeadOrganization)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(3)
                    .ToListAsync();
            }
            catch
            {
                ViewBag.ApprovedOrgsCount = 0;
                ViewBag.ActiveOperationsCount = 0;
                ViewBag.TotalHelpRequestsCount = 0;
                ViewBag.ResolvedHelpRequestsCount = 0;
                ViewBag.SheltersCount = 0;
                ViewBag.RecentAnnouncements = new List<Announcement>();
                ViewBag.RecentOperations = new List<DisasterOperation>();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

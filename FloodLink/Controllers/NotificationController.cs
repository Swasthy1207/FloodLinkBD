using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        // INDEX — User Notification Inbox
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? filter, int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            const int pageSize = 15;
            var query = _context.UserNotifications
                .Where(n => n.UserId == userId)
                .AsQueryable();

            if (filter == "unread")
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.UnreadCount = await _context.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            ViewBag.Filter = filter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(notifications);
        }

        // ================================================================
        // MARK AS READ
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.UserNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(notification.TargetUrl))
                {
                    return Redirect(notification.TargetUrl);
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // MARK ALL AS READ
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var unread = await _context.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var n in unread)
                {
                    n.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // GET UNREAD COUNT (AJAX API)
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });

            var count = await _context.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Json(new { count });
        }
    }
}

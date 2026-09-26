using FloodLink.Data;
using FloodLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string type = "General", string? targetUrl = null);
        Task NotifyRoleAsync(string roleName, string title, string message, string type = "General", string? targetUrl = null);
        Task NotifyOrganizationMembersAsync(int organizationId, string title, string message, string type = "General", string? targetUrl = null);
        Task<int> GetUnreadCountAsync(string userId);
    }

    /// <summary>
    /// Section 4.15 — Notification System Implementation
    /// Manages dispatching notifications for:
    /// Request Accepted, Volunteer Assigned, Request Status Updated, Emergency Announcement, New Task Assignment.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type = "General", string? targetUrl = null)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var notification = new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = type,
                TargetUrl = targetUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyRoleAsync(string roleName, string title, string message, string type = "General", string? targetUrl = null)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            if (!usersInRole.Any()) return;

            var notifications = usersInRole.Select(u => new UserNotification
            {
                UserId = u.Id,
                Title = title,
                Message = message,
                NotificationType = type,
                TargetUrl = targetUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.UserNotifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyOrganizationMembersAsync(int organizationId, string title, string message, string type = "General", string? targetUrl = null)
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org != null && !string.IsNullOrEmpty(org.ManagedByUserId))
            {
                await CreateNotificationAsync(org.ManagedByUserId, title, message, type, targetUrl);
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return 0;
            return await _context.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}

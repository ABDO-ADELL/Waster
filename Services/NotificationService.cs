using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Waster.Hubs;
using Waster.Models;
using Waster.Models.DbModels;

namespace Waster.Services
{

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;  // To send real-time messages
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AppDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        //  When someone claims YOUR post
        public async Task SendClaimNotificationAsync(
            string recipientUserId,  // Post owner
            string senderName,       // Who claimed it
            string postTitle,
            Guid claimId)
        {
            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = "NewClaim",
                Message = $"{senderName} wants to claim your post '{postTitle}'",
                ClaimId = claimId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            // 1. Save to database
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Send real-time notification via SignalR
            await _hubContext.Clients
                .Group(recipientUserId)  // Send only to this user
                .SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    type = notification.Type,
                    message = notification.Message,
                    claimId = notification.ClaimId,
                    createdAt = notification.CreatedAt,
                    isRead = notification.IsRead
                });

            _logger.LogInformation($"Sent claim notification to user {recipientUserId}");
        }

        // When the claim is ACCEPTED
        public async Task SendClaimAcceptedNotificationAsync(
            string recipientUserId,  // Person who claimed
            string postOwnerName,
            string postTitle,
            Guid claimId)
        {
            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = "ClaimAccepted",
                Message = $"🎉 {postOwnerName} accepted your claim for '{postTitle}'!",
                ClaimId = claimId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(recipientUserId)
                .SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    type = notification.Type,
                    message = notification.Message,
                    claimId = notification.ClaimId,
                    createdAt = notification.CreatedAt,
                    isRead = notification.IsRead
                });

            _logger.LogInformation($"Sent acceptance notification to user {recipientUserId}");
        }

        // When the claim is REJECTED
        public async Task SendClaimRejectedNotificationAsync(
            string recipientUserId,
            string postOwnerName,
            string postTitle,
            Guid claimId)
        {
            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = "ClaimRejected",
                Message = $"{postOwnerName} declined your claim for '{postTitle}'",
                ClaimId = claimId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(recipientUserId)
                .SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    type = notification.Type,
                    message = notification.Message,
                    claimId = notification.ClaimId,
                    createdAt = notification.CreatedAt,
                    isRead = notification.IsRead
                });

            _logger.LogInformation($"Sent rejection notification to user {recipientUserId}");
        }

        //Get all notifications for a user
        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)  // Last 50 notifications
                .ToListAsync();
        }

        //Mark notification as read
        public async Task MarkAsReadAsync(Guid notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        //Count unread notifications
        
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}
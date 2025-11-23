using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Waster.Services;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService,ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // Get all notifications for current user
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);

                return Ok(new
                {
                    count = notifications.Count,
                    notifications = notifications.Select(n => new
                    {
                        id = n.Id,
                        type = n.Type,
                        message = n.Message,
                        claimId = n.ClaimId,
                        postId = n.PostId,
                        createdAt = n.CreatedAt,
                        isRead = n.IsRead
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
        // Get count of unread notifications (for badge)        
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var count = await _notificationService.GetUnreadCountAsync(userId);

                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { message = "An error occurred" });
            }
        } 
        // Mark a notification as read
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                await _notificationService.MarkAsReadAsync(id, userId);

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        // Mark ALL notifications as read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly: true);

                foreach (var notification in notifications)
                {
                    await _notificationService.MarkAsReadAsync(notification.Id, userId);
                }

                return Ok(new { message = $"Marked {notifications.Count} notifications as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}
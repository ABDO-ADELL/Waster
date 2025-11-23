using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Waster.Hubs
{
    [Authorize] 
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        // Called when a user connects (like opening WhatsApp)
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal "room" using their UserId
                // Like subscribing to your personal notification channel
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation($"User {userId} connected to notifications");
            }
            await base.OnConnectedAsync();
        }
        // Called when user disconnects (like closing the app)
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation($"User {userId} disconnected from notifications");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Client can call this to mark notification as read
        public async Task MarkAsRead(Guid notificationId)
        {
            // We'll implement this in the service
            _logger.LogInformation($"Mark notification {notificationId} as read");
        }
    }
}
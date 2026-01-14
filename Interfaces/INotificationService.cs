using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Waster.Hubs;
using Waster.Models;
using Waster.Models.DbModels;

namespace Waster.Interfaces
{
    public interface INotificationService
    {
        Task SendClaimNotificationAsync(string recipientUserId, string senderName, string postTitle, Guid claimId);
        Task SendClaimAcceptedNotificationAsync(string recipientUserId, string postOwnerName, string postTitle, Guid claimId);
        Task SendClaimRejectedNotificationAsync(string recipientUserId, string postOwnerName, string postTitle, Guid claimId);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task MarkAsReadAsync(Guid notificationId, string userId);
        Task<int> GetUnreadCountAsync(string userId);
    }
    }

using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data.Services;

public interface INotificationService
{
    Task<List<Notification>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);
}

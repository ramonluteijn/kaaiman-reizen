using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class NotificationService : INotificationService
{
    private readonly MainContext _db;

    public NotificationService(MainContext db)
    {
        _db = db;
    }

    public Task<List<Notification>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _db.Notifications
            .Where(n => n.ApplicationUserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.ApplicationUserId == userId, cancellationToken);
        
        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

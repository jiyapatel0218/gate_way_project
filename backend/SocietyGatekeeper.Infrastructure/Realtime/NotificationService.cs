using Microsoft.AspNetCore.SignalR;
using SocietyGatekeeper.Application.Interfaces;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.Infrastructure.Realtime;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task NotifyUserAsync(Guid recipientUserId, NotificationCategory category, string title, string message, string? linkUrl = null)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Category = category,
            Title = title,
            Message = message,
            LinkUrl = linkUrl
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(recipientUserId.ToString()).SendAsync("ReceiveNotification", new
        {
            notification.Id,
            Category = category.ToString(),
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            CreatedAt = notification.CreatedAt
        });
    }
}

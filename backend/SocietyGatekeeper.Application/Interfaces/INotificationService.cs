using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Application.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid recipientUserId, NotificationCategory category, string title, string message, string? linkUrl = null);
}

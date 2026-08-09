using SocietyGatekeeper.Domain.Common;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class NotificationTemplate : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; }
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Notification : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public ApplicationUser RecipientUser { get; set; } = null!;
    public NotificationCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class LoginHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
}

public class PasswordResetOtp : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string OtpHash { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty; // "Email" or "Sms"
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public int ResendCount { get; set; }
    public bool IsUsed { get; set; }
    public DateTime LastSentAt { get; set; } = DateTime.UtcNow;
}

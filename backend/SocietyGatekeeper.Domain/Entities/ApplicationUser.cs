using Microsoft.AspNetCore.Identity;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? SocietyId { get; set; }
    public Society? Society { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Resident? ResidentProfile { get; set; }
    public SecurityGuard? SecurityGuardProfile { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}

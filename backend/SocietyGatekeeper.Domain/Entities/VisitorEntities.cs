using SocietyGatekeeper.Domain.Common;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class VisitorType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Visitor : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public Guid FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public string Purpose { get; set; } = string.Empty;
    public VisitorEntryType EntryType { get; set; } = VisitorEntryType.Visitor;
    public Guid? VisitorTypeId { get; set; }
    public VisitorType? VisitorTypeRef { get; set; }
    public string? VehicleNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public VisitorStatus Status { get; set; } = VisitorStatus.Pending;
    public Guid CreatedByGuardUserId { get; set; }
    public ApplicationUser CreatedByGuardUser { get; set; } = null!;
    public DateTime EntryTime { get; set; } = DateTime.UtcNow;
    public DateTime? ExitTime { get; set; }
    public Guid? ApprovedByResidentId { get; set; }
    public Resident? ApprovedByResident { get; set; }
    public DateTime? RespondedAt { get; set; }
}

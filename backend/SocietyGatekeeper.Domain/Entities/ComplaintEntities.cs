using SocietyGatekeeper.Domain.Common;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class ComplaintCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Complaint : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public ComplaintCategory Category { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public string? AttachmentUrl { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<ComplaintRemark> Remarks { get; set; } = new List<ComplaintRemark>();
}

public class ComplaintRemark : BaseEntity
{
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public Guid AddedByUserId { get; set; }
    public ApplicationUser AddedByUser { get; set; } = null!;
    public string Remark { get; set; } = string.Empty;
    public ComplaintStatus StatusAtTime { get; set; }
}

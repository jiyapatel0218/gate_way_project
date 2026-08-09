using SocietyGatekeeper.Domain.Common;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class PropertyListing : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ListingType Type { get; set; }
    public decimal Price { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.PendingApproval;
    public string? RejectionReason { get; set; }

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
}

public class PropertyImage : BaseEntity
{
    public Guid PropertyListingId { get; set; }
    public PropertyListing PropertyListing { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
}

public class Notice : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NoticeType Type { get; set; } = NoticeType.Notice;
    public DateTime? EventDate { get; set; }
    public Guid PublishedByUserId { get; set; }
    public ApplicationUser PublishedByUser { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? AttachmentUrl { get; set; }
}

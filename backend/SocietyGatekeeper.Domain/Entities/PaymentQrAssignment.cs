using SocietyGatekeeper.Domain.Common;

namespace SocietyGatekeeper.Domain.Entities;

/// <summary>
/// A payment-collection QR code assigned by an admin to a Society (and optionally narrowed to
/// one Block within it) plus the user/owner who receives the money. Residents paying maintenance
/// see whichever assignment matches their own flat's Society/Block — never anyone else's.
/// </summary>
public class PaymentQrAssignment : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public Guid? BlockId { get; set; }
    public Block? Block { get; set; }
    public Guid AssignedToUserId { get; set; }
    public ApplicationUser AssignedToUser { get; set; } = null!;
    public string QrImageUrl { get; set; } = string.Empty;
    public string? PayeeName { get; set; }
    public bool IsActive { get; set; } = true;
}

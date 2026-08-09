using SocietyGatekeeper.Domain.Common;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class MaintenanceType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MaintenanceInvoice : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public Guid FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public Guid MaintenanceTypeId { get; set; }
    public MaintenanceType MaintenanceType { get; set; } = null!;
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Payment : BaseEntity
{
    public Guid MaintenanceInvoiceId { get; set; }
    public MaintenanceInvoice MaintenanceInvoice { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentMode Mode { get; set; }
    public string? TransactionReference { get; set; }
    public DateTime PaidOn { get; set; } = DateTime.UtcNow;
    public Guid RecordedByUserId { get; set; }
    public ApplicationUser RecordedByUser { get; set; } = null!;
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}

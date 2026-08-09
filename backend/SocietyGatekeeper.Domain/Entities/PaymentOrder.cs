using SocietyGatekeeper.Domain.Common;

namespace SocietyGatekeeper.Domain.Entities;

public enum PaymentOrderStatus
{
    Created,
    Paid,
    Failed
}

public class PaymentOrder : BaseEntity
{
    public Guid MaintenanceInvoiceId { get; set; }
    public MaintenanceInvoice MaintenanceInvoice { get; set; } = null!;
    public string Provider { get; set; } = string.Empty;
    public string ProviderOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Created;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

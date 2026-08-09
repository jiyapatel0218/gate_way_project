using SocietyGatekeeper.Domain.Common;

namespace SocietyGatekeeper.Domain.Entities;

/// <summary>
/// An immutable receipt/invoice generated the moment a maintenance payment succeeds.
/// Fields are snapshotted at generation time (society name, resident name, etc.) so a
/// historical invoice never silently changes if the underlying records are edited later.
/// </summary>
public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public Guid MaintenanceInvoiceId { get; set; }
    public MaintenanceInvoice MaintenanceInvoice { get; set; } = null!;

    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;

    // Snapshot fields
    public string SocietyName { get; set; } = string.Empty;
    public string? SocietyLogoUrl { get; set; }
    public string FlatNumber { get; set; } = string.Empty;
    public string BlockName { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime PaymentDateTime { get; set; }
}

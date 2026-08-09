using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Services;

public class InvoiceService
{
    private readonly ApplicationDbContext _db;

    public InvoiceService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates the invoice snapshot for a payment, or returns the existing one if already generated
    /// (guarantees exactly one invoice per payment, even if called twice for the same payment).
    /// </summary>
    public async Task<Invoice> GenerateForPaymentAsync(Guid paymentId)
    {
        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.PaymentId == paymentId);
        if (existing is not null) return existing;

        var payment = await _db.Payments
            .Include(p => p.MaintenanceInvoice).ThenInclude(m => m.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block)
            .Include(p => p.MaintenanceInvoice).ThenInclude(m => m.Flat).ThenInclude(f => f.Residents).ThenInclude(r => r.User)
            .Include(p => p.MaintenanceInvoice).ThenInclude(m => m.Society)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null) throw new InvalidOperationException($"Payment {paymentId} not found");

        var maintenanceInvoice = payment.MaintenanceInvoice;
        var flat = maintenanceInvoice.Flat;
        var owner = flat.Residents.FirstOrDefault(r => r.IsOwner && r.IsActive) ?? flat.Residents.FirstOrDefault(r => r.IsActive);

        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            PaymentId = payment.Id,
            MaintenanceInvoiceId = maintenanceInvoice.Id,
            ResidentId = owner?.Id ?? Guid.Empty,
            SocietyName = maintenanceInvoice.Society.Name,
            SocietyLogoUrl = maintenanceInvoice.Society.LogoUrl,
            FlatNumber = flat.FlatNumber,
            BlockName = flat.Wing.Block.Name,
            WingName = flat.Wing.Name,
            OwnerName = owner?.User.FullName ?? "N/A",
            OwnerPhone = owner?.User.PhoneNumber,
            OwnerEmail = owner?.User.Email,
            BillingPeriod = $"{maintenanceInvoice.Month} {maintenanceInvoice.Year}",
            AmountPaid = payment.Amount,
            PaymentMode = payment.Mode.ToString(),
            TransactionReference = payment.TransactionReference,
            PaymentDateTime = payment.PaidOn
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"INV-{datePart}-";

        var countToday = await _db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix));
        var sequence = (countToday + 1).ToString("D4");

        var candidate = $"{prefix}{sequence}";

        // Extremely unlikely, but guarantee uniqueness even under a race.
        while (await _db.Invoices.AnyAsync(i => i.InvoiceNumber == candidate))
        {
            sequence = (int.Parse(sequence) + 1).ToString("D4");
            candidate = $"{prefix}{sequence}";
        }

        return candidate;
    }
}

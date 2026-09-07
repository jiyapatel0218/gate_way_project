using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.API.Services;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Application.Interfaces;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly InvoiceService _invoiceService;
    private readonly IConfiguration _config;

    public MaintenanceController(ApplicationDbContext db, INotificationService notificationService, IPaymentGatewayService paymentGateway, InvoiceService invoiceService, IConfiguration config)
    {
        _db = db;
        _notificationService = notificationService;
        _paymentGateway = paymentGateway;
        _invoiceService = invoiceService;
        _config = config;
    }

    private IQueryable<MaintenanceInvoice> BaseQuery()
    {
        var societyId = User.GetSocietyId();
        var query = _db.MaintenanceInvoices
            .Include(m => m.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block)
            .Include(m => m.MaintenanceType)
            .AsQueryable();

        if (societyId.HasValue) query = query.Where(m => m.SocietyId == societyId);
        return query;
    }

    private static MaintenanceInvoiceDto ToDto(MaintenanceInvoice m) => new(
        m.Id, m.Flat.FlatNumber, m.Flat.Wing.Name, m.Flat.Wing.Block.Name, m.MaintenanceType.Name,
        m.Month, m.Year, m.Amount, m.PenaltyAmount, m.PaidAmount, m.DueDate, m.Status
    );

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<List<MaintenanceInvoiceDto>>> GetAll(
        [FromQuery] Guid? blockId, [FromQuery] Guid? flatId, [FromQuery] string? month,
        [FromQuery] int? year, [FromQuery] PaymentStatus? status)
    {
        var query = BaseQuery();
        if (blockId.HasValue) query = query.Where(m => m.Flat.Wing.BlockId == blockId);
        if (flatId.HasValue) query = query.Where(m => m.FlatId == flatId);
        if (!string.IsNullOrWhiteSpace(month)) query = query.Where(m => m.Month == month);
        if (year.HasValue) query = query.Where(m => m.Year == year);
        if (status.HasValue) query = query.Where(m => m.Status == status);

        var invoices = await query.OrderByDescending(m => m.DueDate).ToListAsync();
        return Ok(invoices.Select(ToDto).ToList());
    }

    [HttpGet("my")]
    [Authorize(Roles = "Resident")]
    public async Task<ActionResult<List<MaintenanceInvoiceDto>>> GetMyInvoices()
    {
        var userId = User.GetUserId();
        var resident = await _db.Residents.FirstOrDefaultAsync(r => r.UserId == userId);
        if (resident is null) return Ok(new List<MaintenanceInvoiceDto>());

        var invoices = await BaseQuery().Where(m => m.FlatId == resident.FlatId)
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.DueDate).ToListAsync();

        return Ok(invoices.Select(ToDto).ToList());
    }

    [HttpPost("generate")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> GenerateMonthly(GenerateMaintenanceRequest request)
    {
        var societyId = User.GetSocietyId() ?? throw new InvalidOperationException("Society context required");
        var type = await _db.MaintenanceTypes.FindAsync(request.MaintenanceTypeId);
        if (type is null) return BadRequest(new { message = "Maintenance type not found" });

        var flatsQuery = _db.Flats.Where(f => f.Wing.Block.SocietyId == societyId && f.OccupancyStatus != FlatOccupancyStatus.Vacant);
        if (request.BlockId.HasValue) flatsQuery = flatsQuery.Where(f => f.Wing.BlockId == request.BlockId);

        var flats = await flatsQuery.ToListAsync();
        int created = 0;

        foreach (var flat in flats)
        {
            var exists = await _db.MaintenanceInvoices.AnyAsync(m =>
                m.FlatId == flat.Id && m.MaintenanceTypeId == request.MaintenanceTypeId && m.Month == request.Month && m.Year == request.Year);
            if (exists) continue;

            _db.MaintenanceInvoices.Add(new MaintenanceInvoice
            {
                SocietyId = societyId,
                FlatId = flat.Id,
                MaintenanceTypeId = request.MaintenanceTypeId,
                Month = request.Month,
                Year = request.Year,
                Amount = type.DefaultAmount,
                DueDate = request.DueDate,
                Status = PaymentStatus.Pending
            });
            created++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Generated {created} invoices" });
    }

    [HttpPost("{id}/payments")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> RecordPayment(Guid id, RecordPaymentRequest request)
    {
        var invoice = await _db.MaintenanceInvoices.Include(m => m.Flat).ThenInclude(f => f.Residents)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (invoice is null) return NotFound();

        var userId = User.GetUserId();

        var payment = new Payment
        {
            MaintenanceInvoiceId = id,
            Amount = request.Amount,
            Mode = request.Mode,
            TransactionReference = request.TransactionReference,
            Notes = request.Notes,
            RecordedByUserId = userId,
            ReceiptNumber = $"RCPT-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };
        _db.Payments.Add(payment);

        invoice.PaidAmount += request.Amount;
        invoice.Status = invoice.PaidAmount >= invoice.Amount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var generatedInvoice = await _invoiceService.GenerateForPaymentAsync(payment.Id);

        foreach (var resident in invoice.Flat.Residents.Where(r => r.IsActive))
        {
            await _notificationService.NotifyUserAsync(
                resident.UserId, NotificationCategory.Payment,
                "Payment Recorded", $"Payment of {request.Amount:C} recorded for {invoice.Month} {invoice.Year}.", $"/maintenance/{invoice.Id}");
        }

        var recordedBy = await _db.Users.FindAsync(userId);
        return Ok(new PaymentDto(payment.Id, payment.Amount, payment.Mode, payment.TransactionReference, payment.PaidOn, payment.ReceiptNumber, recordedBy?.FullName ?? "", generatedInvoice.Id, generatedInvoice.InvoiceNumber));
    }

    [HttpPost("{id}/payments/online/create-order")]
    [Authorize]
    public async Task<ActionResult<CreatePaymentOrderResponse>> CreateOnlineOrder(Guid id)
    {
        var invoice = await _db.MaintenanceInvoices.FindAsync(id);
        if (invoice is null) return NotFound();

        var due = invoice.Amount - invoice.PaidAmount;
        if (due <= 0) return BadRequest(new { message = "This invoice is already fully paid." });

        var orderResult = await _paymentGateway.CreateOrderAsync(due, "INR", $"INV-{invoice.Id}");

        var order = new PaymentOrder
        {
            MaintenanceInvoiceId = id,
            Provider = _paymentGateway.ProviderName,
            ProviderOrderId = orderResult.ProviderOrderId,
            Amount = due,
            Currency = "INR",
            Status = PaymentOrderStatus.Created,
            CreatedByUserId = User.GetUserId()
        };
        _db.PaymentOrders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new CreatePaymentOrderResponse(order.Id, orderResult.ProviderOrderId, _paymentGateway.ProviderName, orderResult.CheckoutKeyId, due, "INR"));
    }

    [HttpPost("payments/online/verify")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> VerifyOnlinePayment(VerifyPaymentRequest request)
    {
        var order = await _db.PaymentOrders
            .Include(o => o.MaintenanceInvoice).ThenInclude(i => i.Flat).ThenInclude(f => f.Residents)
            .FirstOrDefaultAsync(o => o.ProviderOrderId == request.ProviderOrderId);

        if (order is null) return NotFound(new { message = "Order not found" });
        if (order.Status == PaymentOrderStatus.Paid) return BadRequest(new { message = "This order has already been processed." });

        var isValid = _paymentGateway.VerifyPayment(request.ProviderOrderId, request.ProviderPaymentId, request.Signature);
        if (!isValid)
        {
            order.Status = PaymentOrderStatus.Failed;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Payment verification failed." });
        }

        var (payment, generatedInvoice) = await MarkOrderPaidAsync(order, request.ProviderPaymentId, User.GetUserId());

        var recordedBy = await _db.Users.FindAsync(payment.RecordedByUserId);
        return Ok(new PaymentDto(payment.Id, payment.Amount, payment.Mode, payment.TransactionReference, payment.PaidOn, payment.ReceiptNumber, recordedBy?.FullName ?? "", generatedInvoice.Id, generatedInvoice.InvoiceNumber));
    }

    // Server-to-server fallback for the client-driven /verify call above: if the browser closes
    // or the network drops after Razorpay captures the payment but before the client can call
    // /verify, this webhook is what still marks the invoice paid. Configure the webhook URL and
    // its secret in the Razorpay Dashboard (Settings > Webhooks) — see README for details.
    [HttpPost("payments/online/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> RazorpayWebhook()
    {
        var webhookSecret = _config["PaymentGateway:Razorpay:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret)) return NotFound();

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var expectedSignature = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), Encoding.UTF8.GetBytes(rawBody))
        ).ToLowerInvariant();

        if (string.IsNullOrEmpty(signature) || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(signature.ToLowerInvariant())))
        {
            return Unauthorized();
        }

        using var json = JsonDocument.Parse(rawBody);
        var root = json.RootElement;
        if (root.GetProperty("event").GetString() != "payment.captured") return Ok();

        var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
        var providerOrderId = paymentEntity.GetProperty("order_id").GetString()!;
        var providerPaymentId = paymentEntity.GetProperty("id").GetString()!;

        var order = await _db.PaymentOrders
            .Include(o => o.MaintenanceInvoice).ThenInclude(i => i.Flat).ThenInclude(f => f.Residents)
            .FirstOrDefaultAsync(o => o.ProviderOrderId == providerOrderId);

        // Idempotent: unknown order, or already marked paid by the client-driven /verify call.
        if (order is null || order.Status == PaymentOrderStatus.Paid) return Ok();

        await MarkOrderPaidAsync(order, providerPaymentId, order.CreatedByUserId);
        return Ok();
    }

    private async Task<(Payment Payment, Invoice Invoice)> MarkOrderPaidAsync(PaymentOrder order, string providerPaymentId, Guid recordedByUserId)
    {
        order.Status = PaymentOrderStatus.Paid;

        var invoice = order.MaintenanceInvoice;

        var payment = new Payment
        {
            MaintenanceInvoiceId = invoice.Id,
            Amount = order.Amount,
            Mode = PaymentMode.Online,
            TransactionReference = providerPaymentId,
            RecordedByUserId = recordedByUserId,
            ReceiptNumber = $"RCPT-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };
        _db.Payments.Add(payment);

        invoice.PaidAmount += order.Amount;
        invoice.Status = invoice.PaidAmount >= invoice.Amount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var generatedInvoice = await _invoiceService.GenerateForPaymentAsync(payment.Id);

        foreach (var resident in invoice.Flat.Residents.Where(r => r.IsActive))
        {
            await _notificationService.NotifyUserAsync(
                resident.UserId, NotificationCategory.Payment,
                "Payment Successful", $"Online payment of {order.Amount:C} received for {invoice.Month} {invoice.Year}.", $"/maintenance/{invoice.Id}");
        }

        return (payment, generatedInvoice);
    }

    [HttpPut("payments/{paymentId}")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> EditPayment(Guid paymentId, RecordPaymentRequest request)
    {
        var payment = await _db.Payments.Include(p => p.MaintenanceInvoice).FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment is null) return NotFound();

        var invoice = payment.MaintenanceInvoice;
        invoice.PaidAmount = invoice.PaidAmount - payment.Amount + request.Amount;
        invoice.Status = invoice.PaidAmount >= invoice.Amount ? PaymentStatus.Paid
            : invoice.PaidAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending;

        payment.Amount = request.Amount;
        payment.Mode = request.Mode;
        payment.TransactionReference = request.TransactionReference;
        payment.Notes = request.Notes;
        payment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/payments")]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(Guid id)
    {
        var payments = await _db.Payments.Include(p => p.RecordedByUser)
            .Where(p => p.MaintenanceInvoiceId == id)
            .OrderByDescending(p => p.PaidOn)
            .ToListAsync();

        var paymentIds = payments.Select(p => p.Id).ToList();
        var invoicesByPayment = await _db.Invoices
            .Where(i => paymentIds.Contains(i.PaymentId))
            .ToDictionaryAsync(i => i.PaymentId);

        var result = payments.Select(p =>
        {
            invoicesByPayment.TryGetValue(p.Id, out var inv);
            return new PaymentDto(p.Id, p.Amount, p.Mode, p.TransactionReference, p.PaidOn, p.ReceiptNumber, p.RecordedByUser.FullName, inv?.Id, inv?.InvoiceNumber);
        }).ToList();

        return Ok(result);
    }

    [HttpGet("summary")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> GetSummary([FromQuery] string? month, [FromQuery] int? year)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(month)) query = query.Where(m => m.Month == month);
        if (year.HasValue) query = query.Where(m => m.Year == year);

        var invoices = await query.ToListAsync();

        return Ok(new
        {
            TotalInvoices = invoices.Count,
            PaidCount = invoices.Count(i => i.Status == PaymentStatus.Paid),
            PendingCount = invoices.Count(i => i.Status == PaymentStatus.Pending || i.Status == PaymentStatus.PartiallyPaid),
            OverdueCount = invoices.Count(i => i.Status == PaymentStatus.Overdue),
            TotalCollected = invoices.Sum(i => i.PaidAmount),
            TotalPending = invoices.Sum(i => i.Amount - i.PaidAmount)
        });
    }

    [HttpGet("export/excel")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> ExportExcel([FromQuery] string? month, [FromQuery] int? year, [FromQuery] PaymentStatus? status)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(month)) query = query.Where(m => m.Month == month);
        if (year.HasValue) query = query.Where(m => m.Year == year);
        if (status.HasValue) query = query.Where(m => m.Status == status);

        var invoices = await query.ToListAsync();

        var headers = new[] { "Block", "Wing", "Flat", "Type", "Month", "Year", "Amount", "Paid", "Status", "Due Date" };
        var rows = invoices.Select(i => (IReadOnlyList<object?>)new object?[]
        {
            i.Flat.Wing.Block.Name, i.Flat.Wing.Name, i.Flat.FlatNumber, i.MaintenanceType.Name,
            i.Month, i.Year, i.Amount, i.PaidAmount, i.Status.ToString(), i.DueDate.ToString("yyyy-MM-dd")
        });

        var file = ExportService.ToExcel("Maintenance", headers, rows);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "maintenance-report.xlsx");
    }

    [HttpGet("export/pdf")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> ExportPdf([FromQuery] string? month, [FromQuery] int? year, [FromQuery] PaymentStatus? status)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(month)) query = query.Where(m => m.Month == month);
        if (year.HasValue) query = query.Where(m => m.Year == year);
        if (status.HasValue) query = query.Where(m => m.Status == status);

        var invoices = await query.ToListAsync();

        var headers = new[] { "Block", "Flat", "Type", "Month/Year", "Amount", "Paid", "Status" };
        var rows = invoices.Select(i => (IReadOnlyList<object?>)new object?[]
        {
            i.Flat.Wing.Block.Name, i.Flat.FlatNumber, i.MaintenanceType.Name,
            $"{i.Month} {i.Year}", i.Amount, i.PaidAmount, i.Status.ToString()
        });

        var file = ExportService.ToPdf("Maintenance Report", headers, rows);
        return File(file, "application/pdf", "maintenance-report.pdf");
    }

    private static InvoiceDto ToInvoiceDto(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.SocietyName, i.SocietyLogoUrl, i.FlatNumber, i.BlockName, i.WingName,
        i.OwnerName, i.OwnerPhone, i.OwnerEmail, i.BillingPeriod, i.AmountPaid,
        i.PaymentMode, i.TransactionReference, i.PaymentDateTime, i.CreatedAt
    );

    [HttpGet("invoices/mine")]
    [Authorize(Roles = "Resident")]
    public async Task<ActionResult<List<InvoiceDto>>> GetMyPaymentInvoices()
    {
        var userId = User.GetUserId();
        var resident = await _db.Residents.FirstOrDefaultAsync(r => r.UserId == userId);
        if (resident is null) return Ok(new List<InvoiceDto>());

        var invoices = await _db.Invoices
            .Where(i => i.ResidentId == resident.Id)
            .OrderByDescending(i => i.PaymentDateTime)
            .ToListAsync();

        return Ok(invoices.Select(ToInvoiceDto).ToList());
    }

    [HttpGet("invoices/by-payment/{paymentId}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceByPayment(Guid paymentId)
    {
        var invoiceEntity = await _db.Invoices.Include(i => i.Resident).FirstOrDefaultAsync(i => i.PaymentId == paymentId);
        if (invoiceEntity is null) return NotFound();
        if (!await CanAccessInvoiceAsync(invoiceEntity)) return Forbid();

        return Ok(ToInvoiceDto(invoiceEntity));
    }

    [HttpGet("invoices/{invoiceId}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId, [FromQuery] string mode = "view")
    {
        var invoiceEntity = await _db.Invoices.Include(i => i.Resident).FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoiceEntity is null) return NotFound();
        if (!await CanAccessInvoiceAsync(invoiceEntity)) return Forbid();

        var pdfBytes = InvoicePdfGenerator.Generate(invoiceEntity);
        var fileName = $"{invoiceEntity.InvoiceNumber}.pdf";

        if (mode == "download")
            return File(pdfBytes, "application/pdf", fileName);

        Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");
    }

    private async Task<bool> CanAccessInvoiceAsync(Invoice invoiceEntity)
    {
        if (User.IsInRole("SuperAdmin")) return true;

        var societyId = User.GetSocietyId();
        if (User.IsInRole("SocietyAdmin") && societyId.HasValue)
        {
            var maintenanceInvoice = await _db.MaintenanceInvoices.FindAsync(invoiceEntity.MaintenanceInvoiceId);
            return maintenanceInvoice?.SocietyId == societyId.Value;
        }

        if (User.IsInRole("Resident"))
        {
            var userId = User.GetUserId();
            return invoiceEntity.Resident.UserId == userId;
        }

        return false;
    }
}

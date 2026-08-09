using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.API.Services;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "SuperAdmin,SocietyAdmin")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    [HttpGet("visitors")]
    public async Task<IActionResult> VisitorReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? format)
    {
        var societyId = User.GetSocietyId();
        var query = _db.Visitors.Include(v => v.Flat).Include(v => v.CreatedByGuardUser).AsQueryable();
        if (societyId.HasValue) query = query.Where(v => v.SocietyId == societyId);
        if (from.HasValue) query = query.Where(v => v.EntryTime >= from.Value);
        if (to.HasValue) query = query.Where(v => v.EntryTime <= to.Value);

        var visitors = await query.OrderByDescending(v => v.EntryTime).ToListAsync();

        var headers = new[] { "Name", "Mobile", "Block", "Purpose", "Type", "Status", "Entry Time", "Exit Time" };
        var rows = visitors.Select(v => (IReadOnlyList<object?>)new object?[]
        {
            v.Name, v.MobileNumber, v.Flat.FlatNumber, v.Purpose, v.EntryType.ToString(), v.Status.ToString(),
            v.EntryTime.ToString("yyyy-MM-dd HH:mm"), v.ExitTime?.ToString("yyyy-MM-dd HH:mm") ?? "-"
        });

        return format == "pdf"
            ? File(ExportService.ToPdf("Visitor Report", headers, rows), "application/pdf", "visitor-report.pdf")
            : File(ExportService.ToExcel("Visitors", headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "visitor-report.xlsx");
    }

    [HttpGet("complaints")]
    public async Task<IActionResult> ComplaintReport([FromQuery] string? format)
    {
        var societyId = User.GetSocietyId();
        var query = _db.Complaints
            .Include(c => c.Category).Include(c => c.Resident).ThenInclude(r => r.User)
            .Include(c => c.Resident).ThenInclude(r => r.Flat).AsQueryable();
        if (societyId.HasValue) query = query.Where(c => c.SocietyId == societyId);

        var complaints = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var headers = new[] { "Title", "Category", "Resident", "Block", "Status", "Created" };
        var rows = complaints.Select(c => (IReadOnlyList<object?>)new object?[]
        {
            c.Title, c.Category.Name, c.Resident.User.FullName, c.Resident.Flat.FlatNumber, c.Status.ToString(), c.CreatedAt.ToString("yyyy-MM-dd")
        });

        return format == "pdf"
            ? File(ExportService.ToPdf("Complaint Report", headers, rows), "application/pdf", "complaint-report.pdf")
            : File(ExportService.ToExcel("Complaints", headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "complaint-report.xlsx");
    }

    [HttpGet("property-listings")]
    public async Task<IActionResult> PropertyReport([FromQuery] string? format)
    {
        var societyId = User.GetSocietyId();
        var query = _db.PropertyListings.Include(p => p.Resident).ThenInclude(r => r.User).AsQueryable();
        if (societyId.HasValue) query = query.Where(p => p.SocietyId == societyId);

        var listings = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        var headers = new[] { "Title", "Type", "Price", "Resident", "Status", "Created" };
        var rows = listings.Select(p => (IReadOnlyList<object?>)new object?[]
        {
            p.Title, p.Type.ToString(), p.Price, p.Resident.User.FullName, p.Status.ToString(), p.CreatedAt.ToString("yyyy-MM-dd")
        });

        return format == "pdf"
            ? File(ExportService.ToPdf("Property Listing Report", headers, rows), "application/pdf", "property-report.pdf")
            : File(ExportService.ToExcel("Listings", headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "property-report.xlsx");
    }

    [HttpGet("residents")]
    public async Task<IActionResult> ResidentReport([FromQuery] string? format)
    {
        var societyId = User.GetSocietyId();
        var query = _db.Residents.Include(r => r.User).Include(r => r.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block).AsQueryable();
        if (societyId.HasValue) query = query.Where(r => r.Flat.Wing.Block.SocietyId == societyId);

        var residents = await query.ToListAsync();

        var headers = new[] { "Name", "Email", "Phone", "Block", "Wing", "Flat", "Owner/Tenant", "Active" };
        var rows = residents.Select(r => (IReadOnlyList<object?>)new object?[]
        {
            r.User.FullName, r.User.Email, r.User.PhoneNumber, r.Flat.Wing.Block.Name, r.Flat.Wing.Name, r.Flat.FlatNumber,
            r.IsOwner ? "Owner" : "Tenant", r.IsActive ? "Yes" : "No"
        });

        return format == "pdf"
            ? File(ExportService.ToPdf("Resident Report", headers, rows), "application/pdf", "resident-report.pdf")
            : File(ExportService.ToExcel("Residents", headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "resident-report.xlsx");
    }

    [HttpGet("payments")]
    public async Task<IActionResult> PaymentReport([FromQuery] string? format)
    {
        var societyId = User.GetSocietyId();
        var query = _db.Payments.Include(p => p.MaintenanceInvoice).ThenInclude(m => m.Flat)
            .Include(p => p.RecordedByUser).AsQueryable();
        if (societyId.HasValue) query = query.Where(p => p.MaintenanceInvoice.SocietyId == societyId);

        var payments = await query.OrderByDescending(p => p.PaidOn).ToListAsync();

        var headers = new[] { "Receipt#", "Block", "Amount", "Mode", "Paid On", "Recorded By" };
        var rows = payments.Select(p => (IReadOnlyList<object?>)new object?[]
        {
            p.ReceiptNumber, p.MaintenanceInvoice.Flat.FlatNumber, p.Amount, p.Mode.ToString(),
            p.PaidOn.ToString("yyyy-MM-dd"), p.RecordedByUser.FullName
        });

        return format == "pdf"
            ? File(ExportService.ToPdf("Payment Report", headers, rows), "application/pdf", "payment-report.pdf")
            : File(ExportService.ToExcel("Payments", headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "payment-report.xlsx");
    }
}

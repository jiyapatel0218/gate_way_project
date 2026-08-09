using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "SuperAdmin,SocietyAdmin")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var societyId = User.GetSocietyId();
        var today = DateTime.UtcNow.Date;
        var currentMonth = DateTime.UtcNow.ToString("MMMM");
        var currentYear = DateTime.UtcNow.Year;

        var residentsQuery = _db.Residents.Where(r => r.IsActive);
        var visitorsQuery = _db.Visitors.AsQueryable();
        var complaintsQuery = _db.Complaints.AsQueryable();
        var invoicesQuery = _db.MaintenanceInvoices.AsQueryable();
        var listingsQuery = _db.PropertyListings.Where(p => p.Status == ListingStatus.Approved);
        var noticesQuery = _db.Notices.Where(n => n.IsActive);

        if (societyId.HasValue)
        {
            residentsQuery = residentsQuery.Where(r => r.Flat.Wing.Block.SocietyId == societyId);
            visitorsQuery = visitorsQuery.Where(v => v.SocietyId == societyId);
            complaintsQuery = complaintsQuery.Where(c => c.SocietyId == societyId);
            invoicesQuery = invoicesQuery.Where(m => m.SocietyId == societyId);
            listingsQuery = listingsQuery.Where(p => p.SocietyId == societyId);
            noticesQuery = noticesQuery.Where(n => n.SocietyId == societyId);
        }

        var totalResidents = await residentsQuery.CountAsync();
        var visitorsToday = await visitorsQuery.CountAsync(v => v.EntryTime.Date == today);
        var activeComplaints = await complaintsQuery.CountAsync(c => c.Status != ComplaintStatus.Closed && c.Status != ComplaintStatus.Completed);
        var pendingComplaints = await complaintsQuery.CountAsync(c => c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.Assigned);
        var closedComplaints = await complaintsQuery.CountAsync(c => c.Status == ComplaintStatus.Closed);

        var monthInvoices = await invoicesQuery.Where(m => m.Month == currentMonth && m.Year == currentYear).ToListAsync();
        var monthlyCollection = monthInvoices.Sum(m => m.PaidAmount);
        var pendingMaintenance = monthInvoices.Sum(m => m.Amount - m.PaidAmount);

        var propertyCount = await listingsQuery.CountAsync();
        var noticesCount = await noticesQuery.CountAsync();

        var recentComplaints = await complaintsQuery.OrderByDescending(c => c.CreatedAt).Take(5)
            .Select(c => new RecentActivityDto("Complaint", c.Title, c.CreatedAt)).ToListAsync();
        var recentVisitors = await visitorsQuery.OrderByDescending(v => v.EntryTime).Take(5)
            .Select(v => new RecentActivityDto("Visitor", v.Name + " visited", v.EntryTime)).ToListAsync();

        var recentActivities = recentComplaints.Concat(recentVisitors)
            .OrderByDescending(a => a.Timestamp).Take(8).ToList();

        return Ok(new DashboardSummaryDto(
            totalResidents, visitorsToday, activeComplaints, pendingComplaints, closedComplaints,
            monthlyCollection, pendingMaintenance, propertyCount, noticesCount, recentActivities
        ));
    }
}

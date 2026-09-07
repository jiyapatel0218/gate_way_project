using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Application.Interfaces;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/visitors")]
[Authorize]
public class VisitorsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public VisitorsController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    private static VisitorDto ToDto(Visitor v) => new(
        v.Id, v.Name, v.MobileNumber, v.Flat.FlatNumber, v.FlatId, v.Purpose,
        v.EntryType, v.VehicleNumber, v.PhotoUrl, v.Status, v.EntryTime, v.ExitTime,
        v.CreatedByGuardUser.FullName
    );

    [HttpGet]
    public async Task<ActionResult<List<VisitorDto>>> GetAll([FromQuery] DateTime? date, [FromQuery] string? search, [FromQuery] VisitorStatus? status)
    {
        var societyId = User.GetSocietyId();
        var userId = User.GetUserId();
        var isResident = User.IsInRole("Resident");

        var query = _db.Visitors
            .Include(v => v.Flat)
            .Include(v => v.CreatedByGuardUser)
            .AsQueryable();

        if (societyId.HasValue) query = query.Where(v => v.SocietyId == societyId);

        if (isResident)
        {
            var resident = await _db.Residents.FirstOrDefaultAsync(r => r.UserId == userId);
            if (resident is null) return Ok(new List<VisitorDto>());
            query = query.Where(v => v.FlatId == resident.FlatId);
        }

        if (date.HasValue)
            query = query.Where(v => v.EntryTime.Date == date.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Name.Contains(search) || v.MobileNumber.Contains(search) || v.Flat.FlatNumber.Contains(search));

        if (status.HasValue)
            query = query.Where(v => v.Status == status);

        var visitors = await query.OrderByDescending(v => v.EntryTime).ToListAsync();
        return Ok(visitors.Select(ToDto).ToList());
    }

    [HttpGet("today")]
    [Authorize(Roles = "SecurityGuard,SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<List<VisitorDto>>> GetToday()
    {
        var societyId = User.GetSocietyId();
        var query = _db.Visitors.Include(v => v.Flat).Include(v => v.CreatedByGuardUser)
            .Where(v => v.EntryTime.Date == DateTime.UtcNow.Date);
        if (societyId.HasValue) query = query.Where(v => v.SocietyId == societyId);

        var visitors = await query.OrderByDescending(v => v.EntryTime).ToListAsync();
        return Ok(visitors.Select(ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "SecurityGuard,SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<VisitorDto>> Create(CreateVisitorRequest request)
    {
        var guardUserId = User.GetUserId();
        var flat = await _db.Flats.Include(f => f.Wing).ThenInclude(w => w.Block).FirstOrDefaultAsync(f => f.Id == request.FlatId);
        if (flat is null) return BadRequest(new { message = "Flat not found" });

        var visitor = new Visitor
        {
            SocietyId = flat.Wing.Block.SocietyId,
            Name = request.Name,
            MobileNumber = request.MobileNumber,
            FlatId = request.FlatId,
            Purpose = request.Purpose,
            EntryType = request.EntryType,
            VisitorTypeId = request.VisitorTypeId,
            VehicleNumber = request.VehicleNumber,
            PhotoUrl = request.PhotoUrl,
            CreatedByGuardUserId = guardUserId,
            Status = VisitorStatus.Pending,
            EntryTime = DateTime.UtcNow
        };
        _db.Visitors.Add(visitor);
        await _db.SaveChangesAsync();

        var residentUsers = await _db.Residents.Where(r => r.FlatId == request.FlatId && r.IsActive)
            .Select(r => r.UserId).ToListAsync();

        foreach (var residentUserId in residentUsers)
        {
            await _notificationService.NotifyUserAsync(
                residentUserId, NotificationCategory.Visitor,
                "New Visitor Request", $"{visitor.Name} is waiting at the gate for {visitor.Purpose}.", $"/visitors/{visitor.Id}");
        }

        var full = await _db.Visitors.Include(v => v.Flat).Include(v => v.CreatedByGuardUser).FirstAsync(v => v.Id == visitor.Id);
        return Ok(ToDto(full));
    }

    [HttpPost("{id}/respond")]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Respond(Guid id, VisitorActionRequest request)
    {
        if (request.Status != VisitorStatus.Approved && request.Status != VisitorStatus.Rejected)
            return BadRequest(new { message = "Status must be Approved or Rejected" });

        var userId = User.GetUserId();
        var resident = await _db.Residents.FirstOrDefaultAsync(r => r.UserId == userId);
        if (resident is null) return NotFound();

        var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == id && v.FlatId == resident.FlatId);
        if (visitor is null) return NotFound();

        visitor.Status = request.Status;
        visitor.ApprovedByResidentId = resident.Id;
        visitor.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notificationService.NotifyUserAsync(
            visitor.CreatedByGuardUserId, NotificationCategory.Visitor,
            "Visitor Response", $"Visitor {visitor.Name} was {request.Status} by the resident.", $"/visitors/{visitor.Id}");

        return NoContent();
    }

    [HttpPost("{id}/entry")]
    [Authorize(Roles = "SecurityGuard,SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> MarkEntered(Guid id)
    {
        var visitor = await _db.Visitors.FindAsync(id);
        if (visitor is null) return NotFound();
        visitor.Status = VisitorStatus.Entered;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/exit")]
    [Authorize(Roles = "SecurityGuard,SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> MarkExited(Guid id)
    {
        var visitor = await _db.Visitors.FindAsync(id);
        if (visitor is null) return NotFound();
        visitor.Status = VisitorStatus.Exited;
        visitor.ExitTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

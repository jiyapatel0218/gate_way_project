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
[Route("api/complaints")]
[Authorize]
public class ComplaintsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ComplaintsController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    private static ComplaintDto ToDto(Complaint c) => new(
        c.Id, c.Title, c.Description, c.Status, c.Category.Name, c.CategoryId,
        c.Resident.User.FullName, c.Resident.Flat.FlatNumber, c.AttachmentUrl,
        c.AssignedToUser != null ? c.AssignedToUser.FullName : null, c.CreatedAt, c.ClosedAt
    );

    [HttpGet]
    public async Task<ActionResult<List<ComplaintDto>>> GetAll([FromQuery] ComplaintStatus? status, [FromQuery] Guid? categoryId)
    {
        var societyId = User.GetSocietyId();
        var userId = User.GetUserId();
        var isResident = User.IsInRole("Resident");

        var query = _db.Complaints
            .Include(c => c.Category)
            .Include(c => c.Resident).ThenInclude(r => r.User)
            .Include(c => c.Resident).ThenInclude(r => r.Flat)
            .Include(c => c.AssignedToUser)
            .AsQueryable();

        if (societyId.HasValue) query = query.Where(c => c.SocietyId == societyId);
        if (isResident) query = query.Where(c => c.Resident.UserId == userId);
        if (status.HasValue) query = query.Where(c => c.Status == status);
        if (categoryId.HasValue) query = query.Where(c => c.CategoryId == categoryId);

        var complaints = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return Ok(complaints.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComplaintDto>> GetById(Guid id)
    {
        var complaint = await _db.Complaints
            .Include(c => c.Category)
            .Include(c => c.Resident).ThenInclude(r => r.User)
            .Include(c => c.Resident).ThenInclude(r => r.Flat)
            .Include(c => c.AssignedToUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (complaint is null) return NotFound();
        return Ok(ToDto(complaint));
    }

    [HttpGet("{id}/remarks")]
    public async Task<ActionResult<List<ComplaintRemarkDto>>> GetRemarks(Guid id)
    {
        var remarks = await _db.ComplaintRemarks
            .Include(r => r.AddedByUser)
            .Where(r => r.ComplaintId == id)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ComplaintRemarkDto(r.Id, r.Remark, r.AddedByUser.FullName, r.StatusAtTime, r.CreatedAt))
            .ToListAsync();

        return Ok(remarks);
    }

    [HttpPost]
    [Authorize(Roles = "Resident")]
    public async Task<ActionResult<ComplaintDto>> Create(CreateComplaintRequest request)
    {
        var userId = User.GetUserId();
        var resident = await _db.Residents.Include(r => r.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block)
            .FirstOrDefaultAsync(r => r.UserId == userId);
        if (resident is null) return BadRequest(new { message = "Resident profile not found" });

        var complaint = new Complaint
        {
            SocietyId = resident.Flat.Wing.Block.SocietyId,
            ResidentId = resident.Id,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            AttachmentUrl = request.AttachmentUrl,
            Status = ComplaintStatus.Open
        };
        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync();

        var full = await _db.Complaints.Include(c => c.Category)
            .Include(c => c.Resident).ThenInclude(r => r.User)
            .Include(c => c.Resident).ThenInclude(r => r.Flat)
            .Include(c => c.AssignedToUser)
            .FirstAsync(c => c.Id == complaint.Id);

        return Ok(ToDto(full));
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> Assign(Guid id, AssignComplaintRequest request)
    {
        var complaint = await _db.Complaints.Include(c => c.Resident).FirstOrDefaultAsync(c => c.Id == id);
        if (complaint is null) return NotFound();

        complaint.AssignedToUserId = request.AssignedToUserId;
        complaint.Status = ComplaintStatus.Assigned;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notificationService.NotifyUserAsync(
            complaint.Resident.UserId, NotificationCategory.Complaint,
            "Complaint Assigned", $"Your complaint '{complaint.Title}' has been assigned.", $"/complaints/{complaint.Id}");

        return NoContent();
    }

    [HttpPost("{id}/status")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateComplaintStatusRequest request)
    {
        var complaint = await _db.Complaints.Include(c => c.Resident).FirstOrDefaultAsync(c => c.Id == id);
        if (complaint is null) return NotFound();

        var userId = User.GetUserId();
        complaint.Status = request.Status;
        complaint.UpdatedAt = DateTime.UtcNow;
        if (request.Status == ComplaintStatus.Closed) complaint.ClosedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Remark))
        {
            _db.ComplaintRemarks.Add(new ComplaintRemark
            {
                ComplaintId = complaint.Id,
                AddedByUserId = userId,
                Remark = request.Remark,
                StatusAtTime = request.Status
            });
        }

        await _db.SaveChangesAsync();

        await _notificationService.NotifyUserAsync(
            complaint.Resident.UserId, NotificationCategory.Complaint,
            "Complaint Status Updated", $"Your complaint '{complaint.Title}' is now {request.Status}.", $"/complaints/{complaint.Id}");

        return NoContent();
    }
}

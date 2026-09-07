using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

/// <summary>
/// Admin-managed assignment of a payment-collection QR code to a Society (optionally narrowed to
/// one Block) and the user/owner who receives the money. Residents/admins never browse this list —
/// they only ever hit GET /mine, which resolves strictly to the single assignment that applies to
/// their own flat's Society/Block.
/// </summary>
[ApiController]
[Route("api/payment-qr-codes")]
[Authorize]
public class PaymentQrCodesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PaymentQrCodesController(ApplicationDbContext db) => _db = db;

    private static PaymentQrAssignmentDto ToDto(PaymentQrAssignment a) => new(
        a.Id, a.SocietyId, a.Society.Name, a.BlockId, a.Block?.Name,
        a.AssignedToUserId, a.AssignedToUser.FullName, a.QrImageUrl, a.PayeeName, a.IsActive
    );

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<List<PaymentQrAssignmentDto>>> GetAll([FromQuery] Guid? societyId)
    {
        var callerSocietyId = User.GetSocietyId();
        var effectiveSocietyId = callerSocietyId ?? societyId;

        var query = _db.PaymentQrAssignments
            .Include(a => a.Society).Include(a => a.Block).Include(a => a.AssignedToUser)
            .AsQueryable();

        if (effectiveSocietyId.HasValue) query = query.Where(a => a.SocietyId == effectiveSocietyId);

        var assignments = await query.OrderBy(a => a.Society.Name).ThenBy(a => a.Block!.Name).ToListAsync();
        return Ok(assignments.Select(ToDto).ToList());
    }

    [HttpGet("assignable-users")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<List<AssignableUserDto>>> GetAssignableUsers([FromQuery] Guid societyId)
    {
        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && callerSocietyId.Value != societyId) return Forbid();

        var users = await _db.Users.Where(u => u.SocietyId == societyId && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new AssignableUserDto(u.Id, u.FullName, u.Email!, u.Role))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<PaymentQrAssignmentDto>> Create(CreatePaymentQrAssignmentRequest request)
    {
        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && callerSocietyId.Value != request.SocietyId) return Forbid();

        var society = await _db.Societies.FindAsync(request.SocietyId);
        if (society is null) return BadRequest(new { message = "Society not found." });

        if (request.BlockId.HasValue)
        {
            var blockExists = await _db.Blocks.AnyAsync(b => b.Id == request.BlockId && b.SocietyId == request.SocietyId);
            if (!blockExists) return BadRequest(new { message = "Selected block does not belong to this society." });
        }

        var assignedUser = await _db.Users.FindAsync(request.AssignedToUserId);
        if (assignedUser is null || assignedUser.SocietyId != request.SocietyId)
            return BadRequest(new { message = "Selected user does not belong to this society." });

        var conflict = await _db.PaymentQrAssignments.AnyAsync(a =>
            a.SocietyId == request.SocietyId && a.BlockId == request.BlockId && a.IsActive);
        if (conflict)
            return BadRequest(new { message = "An active QR code is already assigned for this scope. Deactivate it first." });

        var assignment = new PaymentQrAssignment
        {
            SocietyId = request.SocietyId,
            BlockId = request.BlockId,
            AssignedToUserId = request.AssignedToUserId,
            QrImageUrl = request.QrImageUrl,
            PayeeName = request.PayeeName
        };
        _db.PaymentQrAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        await _db.Entry(assignment).Reference(a => a.Society).LoadAsync();
        await _db.Entry(assignment).Reference(a => a.AssignedToUser).LoadAsync();
        if (assignment.BlockId.HasValue) await _db.Entry(assignment).Reference(a => a.Block).LoadAsync();

        return Ok(ToDto(assignment));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<PaymentQrAssignmentDto>> Update(Guid id, UpdatePaymentQrAssignmentRequest request)
    {
        var assignment = await _db.PaymentQrAssignments
            .Include(a => a.Society).Include(a => a.Block).Include(a => a.AssignedToUser)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment is null) return NotFound();

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && callerSocietyId.Value != assignment.SocietyId) return Forbid();

        if (request.BlockId.HasValue)
        {
            var blockExists = await _db.Blocks.AnyAsync(b => b.Id == request.BlockId && b.SocietyId == assignment.SocietyId);
            if (!blockExists) return BadRequest(new { message = "Selected block does not belong to this society." });
        }

        var assignedUser = await _db.Users.FindAsync(request.AssignedToUserId);
        if (assignedUser is null || assignedUser.SocietyId != assignment.SocietyId)
            return BadRequest(new { message = "Selected user does not belong to this society." });

        var conflict = await _db.PaymentQrAssignments.AnyAsync(a =>
            a.Id != id && a.SocietyId == assignment.SocietyId && a.BlockId == request.BlockId && a.IsActive);
        if (conflict)
            return BadRequest(new { message = "An active QR code is already assigned for this scope. Deactivate it first." });

        assignment.BlockId = request.BlockId;
        assignment.AssignedToUserId = request.AssignedToUserId;
        assignment.QrImageUrl = request.QrImageUrl;
        assignment.PayeeName = request.PayeeName;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _db.Entry(assignment).Reference(a => a.AssignedToUser).LoadAsync();
        await _db.Entry(assignment).Reference(a => a.Block).LoadAsync();

        return Ok(ToDto(assignment));
    }

    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var assignment = await _db.PaymentQrAssignments.FindAsync(id);
        if (assignment is null) return NotFound();

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && callerSocietyId.Value != assignment.SocietyId) return Forbid();

        if (!assignment.IsActive)
        {
            var conflict = await _db.PaymentQrAssignments.AnyAsync(a =>
                a.Id != id && a.SocietyId == assignment.SocietyId && a.BlockId == assignment.BlockId && a.IsActive);
            if (conflict)
                return BadRequest(new { message = "An active QR code is already assigned for this scope." });
        }

        assignment.IsActive = !assignment.IsActive;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { assignment.IsActive });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var assignment = await _db.PaymentQrAssignments.FindAsync(id);
        if (assignment is null) return NotFound();

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && callerSocietyId.Value != assignment.SocietyId) return Forbid();

        assignment.IsDeleted = true;
        assignment.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Resolves the single QR the calling user is allowed to see: their own flat's Block-specific
    /// assignment if one exists, falling back to their Society-wide assignment. Never exposes any
    /// other society's/block's/user's assignment.
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<MyPaymentQrDto>> GetMine()
    {
        var userId = User.GetUserId();
        Guid? societyId = User.GetSocietyId();
        Guid? blockId = null;

        if (User.IsInRole("Resident"))
        {
            var resident = await _db.Residents.Include(r => r.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block)
                .FirstOrDefaultAsync(r => r.UserId == userId);
            if (resident is not null)
            {
                societyId = resident.Flat.Wing.Block.SocietyId;
                blockId = resident.Flat.Wing.BlockId;
            }
        }

        if (!societyId.HasValue) return NotFound(new { message = "No payment QR has been configured for your account yet." });

        var assignment = await _db.PaymentQrAssignments
            .Where(a => a.SocietyId == societyId && a.IsActive && (a.BlockId == blockId || a.BlockId == null))
            .OrderByDescending(a => a.BlockId != null)
            .FirstOrDefaultAsync();

        if (assignment is null)
            return NotFound(new { message = "No payment QR has been configured for your society yet. Please contact your admin." });

        return Ok(new MyPaymentQrDto(assignment.QrImageUrl, assignment.PayeeName));
    }
}

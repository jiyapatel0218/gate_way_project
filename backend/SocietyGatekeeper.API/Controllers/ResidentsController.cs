using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/residents")]
[Authorize]
public class ResidentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResidentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private IQueryable<Resident> BaseQuery()
    {
        var societyId = User.GetSocietyId();
        var query = _db.Residents
            .Include(r => r.User)
            .Include(r => r.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block).ThenInclude(b => b.Society)
            .AsQueryable();

        if (societyId.HasValue)
            query = query.Where(r => r.Flat.Wing.Block.SocietyId == societyId);

        return query;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<List<ResidentDto>>> GetAll([FromQuery] string? search, [FromQuery] Guid? blockId, [FromQuery] bool? isActive)
    {
        var query = BaseQuery();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.User.FullName.Contains(search) || r.Flat.FlatNumber.Contains(search) || r.User.Email!.Contains(search));

        if (blockId.HasValue)
            query = query.Where(r => r.Flat.Wing.BlockId == blockId);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive);

        var residents = await query.Select(r => new ResidentDto(
            r.Id, r.UserId, r.User.FullName, r.User.Email!, r.User.PhoneNumber,
            r.FlatId, r.Flat.FlatNumber, r.Flat.Wing.Name, r.Flat.Wing.Block.Name,
            r.Flat.Wing.Block.SocietyId, r.Flat.Wing.Block.Society.Name,
            r.IsOwner, r.IsActive, r.MoveInDate
        )).ToListAsync();

        return Ok(residents);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResidentDto>> GetById(Guid id)
    {
        var r = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();

        return Ok(ToDto(r));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ResidentDto>> GetMyProfile()
    {
        var userId = User.GetUserId();
        var r = await BaseQuery().FirstOrDefaultAsync(x => x.UserId == userId);
        if (r is null) return NotFound();

        return Ok(ToDto(r));
    }

    private static ResidentDto ToDto(Resident r) => new(
        r.Id, r.UserId, r.User.FullName, r.User.Email!, r.User.PhoneNumber,
        r.FlatId, r.Flat.FlatNumber, r.Flat.Wing.Name, r.Flat.Wing.Block.Name,
        r.Flat.Wing.Block.SocietyId, r.Flat.Wing.Block.Society.Name,
        r.IsOwner, r.IsActive, r.MoveInDate
    );

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<ResidentDto>> Create(CreateResidentRequest request)
    {
        var flat = await _db.Flats.Include(f => f.Wing).ThenInclude(w => w.Block).ThenInclude(b => b.Society).FirstOrDefaultAsync(f => f.Id == request.FlatId);
        if (flat is null) return BadRequest(new { message = "Flat not found" });

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && flat.Wing.Block.SocietyId != callerSocietyId.Value)
            return Forbid();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName,
            Role = UserRole.Resident,
            SocietyId = flat.Wing.Block.SocietyId,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, nameof(UserRole.Resident));

        var resident = new Resident { UserId = user.Id, FlatId = request.FlatId, IsOwner = request.IsOwner };
        _db.Residents.Add(resident);

        flat.OccupancyStatus = request.IsOwner ? FlatOccupancyStatus.Owner : FlatOccupancyStatus.Tenant;

        await _db.SaveChangesAsync();

        return Ok(new ResidentDto(
            resident.Id, user.Id, user.FullName, user.Email!, user.PhoneNumber,
            flat.Id, flat.FlatNumber, flat.Wing.Name, flat.Wing.Block.Name,
            flat.Wing.Block.SocietyId, flat.Wing.Block.Society.Name,
            resident.IsOwner, resident.IsActive, resident.MoveInDate
        ));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<ResidentDto>> Update(Guid id, UpdateResidentRequest request)
    {
        var resident = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        if (resident is null) return NotFound();

        if (resident.FlatId != request.FlatId)
        {
            var newFlat = await _db.Flats.Include(f => f.Wing).ThenInclude(w => w.Block).ThenInclude(b => b.Society)
                .FirstOrDefaultAsync(f => f.Id == request.FlatId);
            if (newFlat is null) return BadRequest(new { message = "Selected flat not found." });

            var callerSocietyId = User.GetSocietyId();
            if (callerSocietyId.HasValue && newFlat.Wing.Block.SocietyId != callerSocietyId.Value)
                return Forbid();

            var oldFlat = resident.Flat;
            var otherActiveResidentsOnOldFlat = await _db.Residents
                .AnyAsync(r => r.FlatId == oldFlat.Id && r.Id != resident.Id && r.IsActive);
            if (!otherActiveResidentsOnOldFlat)
                oldFlat.OccupancyStatus = FlatOccupancyStatus.Vacant;

            newFlat.OccupancyStatus = request.IsOwner ? FlatOccupancyStatus.Owner : FlatOccupancyStatus.Tenant;
            resident.FlatId = newFlat.Id;
            resident.User.SocietyId = newFlat.Wing.Block.SocietyId;
        }

        resident.User.FullName = request.FullName;
        resident.AlternatePhone = request.AlternatePhone;
        resident.IsOwner = request.IsOwner;
        resident.IsActive = request.IsActive;
        resident.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(r => r.Id == id);
        return Ok(ToDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resident = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        if (resident is null) return NotFound();
        resident.IsDeleted = true;
        resident.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var resident = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        if (resident is null) return NotFound();
        resident.IsActive = !resident.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { resident.IsActive });
    }

    // ---------- Family Members ----------
    [HttpGet("{residentId}/family-members")]
    public async Task<ActionResult<List<FamilyMemberDto>>> GetFamilyMembers(Guid residentId)
    {
        var members = await _db.FamilyMembers.Where(f => f.ResidentId == residentId)
            .Select(f => new FamilyMemberDto(f.Id, f.Name, f.Relation, f.Age, f.Phone)).ToListAsync();
        return Ok(members);
    }

    [HttpPost("{residentId}/family-members")]
    public async Task<ActionResult<FamilyMemberDto>> AddFamilyMember(Guid residentId, CreateFamilyMemberRequest request)
    {
        var member = new FamilyMember { ResidentId = residentId, Name = request.Name, Relation = request.Relation, Age = request.Age, Phone = request.Phone };
        _db.FamilyMembers.Add(member);
        await _db.SaveChangesAsync();
        return Ok(new FamilyMemberDto(member.Id, member.Name, member.Relation, member.Age, member.Phone));
    }

    [HttpDelete("family-members/{id}")]
    public async Task<IActionResult> DeleteFamilyMember(Guid id)
    {
        var member = await _db.FamilyMembers.FindAsync(id);
        if (member is null) return NotFound();
        _db.FamilyMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Vehicles ----------
    [HttpGet("{residentId}/vehicles")]
    public async Task<ActionResult<List<VehicleDto>>> GetVehicles(Guid residentId)
    {
        var vehicles = await _db.Vehicles.Where(v => v.ResidentId == residentId)
            .Select(v => new VehicleDto(v.Id, v.VehicleNumber, v.VehicleType, v.Model)).ToListAsync();
        return Ok(vehicles);
    }

    [HttpPost("{residentId}/vehicles")]
    public async Task<ActionResult<VehicleDto>> AddVehicle(Guid residentId, CreateVehicleRequest request)
    {
        var vehicle = new Vehicle { ResidentId = residentId, VehicleNumber = request.VehicleNumber, VehicleType = request.VehicleType, Model = request.Model };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return Ok(new VehicleDto(vehicle.Id, vehicle.VehicleNumber, vehicle.VehicleType, vehicle.Model));
    }

    [HttpDelete("vehicles/{id}")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();
        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Emergency Contacts ----------
    [HttpGet("{residentId}/emergency-contacts")]
    public async Task<ActionResult<List<EmergencyContactDto>>> GetEmergencyContacts(Guid residentId)
    {
        var contacts = await _db.EmergencyContacts.Where(e => e.ResidentId == residentId)
            .Select(e => new EmergencyContactDto(e.Id, e.Name, e.Phone, e.Relation)).ToListAsync();
        return Ok(contacts);
    }

    [HttpPost("{residentId}/emergency-contacts")]
    public async Task<ActionResult<EmergencyContactDto>> AddEmergencyContact(Guid residentId, CreateEmergencyContactRequest request)
    {
        var contact = new EmergencyContact { ResidentId = residentId, Name = request.Name, Phone = request.Phone, Relation = request.Relation };
        _db.EmergencyContacts.Add(contact);
        await _db.SaveChangesAsync();
        return Ok(new EmergencyContactDto(contact.Id, contact.Name, contact.Phone, contact.Relation));
    }

    [HttpDelete("emergency-contacts/{id}")]
    public async Task<IActionResult> DeleteEmergencyContact(Guid id)
    {
        var contact = await _db.EmergencyContacts.FindAsync(id);
        if (contact is null) return NotFound();
        _db.EmergencyContacts.Remove(contact);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Documents ----------
    [HttpPost("{residentId}/documents")]
    public async Task<IActionResult> AddDocument(Guid residentId, [FromBody] AddDocumentRequest request)
    {
        var doc = new ResidentDocument { ResidentId = residentId, DocumentName = request.DocumentName, DocumentUrl = request.DocumentUrl, DocumentType = request.DocumentType };
        _db.ResidentDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return Ok(new { doc.Id, doc.DocumentName, doc.DocumentUrl, doc.DocumentType });
    }

    [HttpGet("{residentId}/documents")]
    public async Task<IActionResult> GetDocuments(Guid residentId)
    {
        var docs = await _db.ResidentDocuments.Where(d => d.ResidentId == residentId)
            .Select(d => new { d.Id, d.DocumentName, d.DocumentUrl, d.DocumentType }).ToListAsync();
        return Ok(docs);
    }
}

public record AddDocumentRequest(
    [Required, StringLength(150, MinimumLength = 1)] string DocumentName,
    [Required, StringLength(500)] string DocumentUrl,
    [StringLength(50)] string? DocumentType
);

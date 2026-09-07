using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/societies")]
[Authorize]
public class SocietiesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SocietiesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<SocietyDto>>> GetAll()
    {
        var societies = await _db.Societies
            .Select(s => new SocietyDto(s.Id, s.Name, s.Address, s.City, s.State, s.PinCode, s.ContactEmail, s.ContactPhone, s.IsActive))
            .ToListAsync();
        return Ok(societies);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<SocietyDto>> GetMine()
    {
        var societyId = User.GetSocietyId();
        if (!societyId.HasValue) return NotFound();

        var s = await _db.Societies.FindAsync(societyId.Value);
        if (s is null) return NotFound();
        return Ok(new SocietyDto(s.Id, s.Name, s.Address, s.City, s.State, s.PinCode, s.ContactEmail, s.ContactPhone, s.IsActive));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SocietyDto>> GetById(Guid id)
    {
        var s = await _db.Societies.FindAsync(id);
        if (s is null) return NotFound();
        return Ok(new SocietyDto(s.Id, s.Name, s.Address, s.City, s.State, s.PinCode, s.ContactEmail, s.ContactPhone, s.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SocietyDto>> Create(CreateSocietyRequest request)
    {
        var society = new Society
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            PinCode = request.PinCode,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone
        };
        _db.Societies.Add(society);
        await _db.SaveChangesAsync();
        return Ok(new SocietyDto(society.Id, society.Name, society.Address, society.City, society.State, society.PinCode, society.ContactEmail, society.ContactPhone, society.IsActive));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, CreateSocietyRequest request)
    {
        var society = await _db.Societies.FindAsync(id);
        if (society is null) return NotFound();

        society.Name = request.Name;
        society.Address = request.Address;
        society.City = request.City;
        society.State = request.State;
        society.PinCode = request.PinCode;
        society.ContactEmail = request.ContactEmail;
        society.ContactPhone = request.ContactPhone;
        society.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var society = await _db.Societies.FindAsync(id);
        if (society is null) return NotFound();
        society.IsActive = !society.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { society.IsActive });
    }
}

[ApiController]
[Route("api/blocks")]
[Authorize(Roles = "SuperAdmin,SocietyAdmin")]
public class BlocksController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BlocksController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<BlockDto>>> GetBySociety([FromQuery] Guid societyId)
    {
        // A caller scoped to a single society (SocietyAdmin) can never list another society's
        // blocks, regardless of what societyId they pass in.
        var effectiveSocietyId = User.GetSocietyId() ?? societyId;

        var blocks = await _db.Blocks.Where(b => b.SocietyId == effectiveSocietyId)
            .Select(b => new BlockDto(b.Id, b.SocietyId, b.Name, b.Description))
            .ToListAsync();
        return Ok(blocks);
    }

    [HttpPost]
    public async Task<ActionResult<BlockDto>> Create(CreateBlockRequest request)
    {
        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && request.SocietyId != callerSocietyId.Value)
            return Forbid();

        var block = new Block { SocietyId = request.SocietyId, Name = request.Name, Description = request.Description };
        _db.Blocks.Add(block);
        await _db.SaveChangesAsync();
        return Ok(new BlockDto(block.Id, block.SocietyId, block.Name, block.Description));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BlockDto>> Update(Guid id, UpdateBlockRequest request)
    {
        var block = await _db.Blocks.FindAsync(id);
        if (block is null) return NotFound();

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && block.SocietyId != callerSocietyId.Value)
            return Forbid();

        block.Name = request.Name;
        block.Description = request.Description;
        block.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new BlockDto(block.Id, block.SocietyId, block.Name, block.Description));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var block = await _db.Blocks.FindAsync(id);
        if (block is null) return NotFound();

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && block.SocietyId != callerSocietyId.Value)
            return Forbid();

        block.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/wings")]
[Authorize(Roles = "SuperAdmin,SocietyAdmin")]
public class WingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WingsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<WingDto>>> GetByBlock([FromQuery] Guid blockId)
    {
        var wings = await _db.Wings.Where(w => w.BlockId == blockId)
            .Select(w => new WingDto(w.Id, w.BlockId, w.Name, w.TotalFloors))
            .ToListAsync();
        return Ok(wings);
    }

    [HttpPost]
    public async Task<ActionResult<WingDto>> Create(CreateWingRequest request)
    {
        var wing = new Wing { BlockId = request.BlockId, Name = request.Name, TotalFloors = request.TotalFloors };
        _db.Wings.Add(wing);
        await _db.SaveChangesAsync();
        return Ok(new WingDto(wing.Id, wing.BlockId, wing.Name, wing.TotalFloors));
    }
}

[ApiController]
[Route("api/flats")]
[Authorize]
public class FlatsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public FlatsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<FlatDto>>> GetByWing([FromQuery] Guid? wingId, [FromQuery] Guid? blockId, [FromQuery] Guid? societyId)
    {
        var query = _db.Flats.Include(f => f.Wing).ThenInclude(w => w.Block).AsQueryable();
        if (wingId.HasValue) query = query.Where(f => f.WingId == wingId);
        if (blockId.HasValue) query = query.Where(f => f.Wing.BlockId == blockId);

        // Callers scoped to a single society (e.g. SocietyAdmin) can never see other societies' flats,
        // regardless of what societyId they pass in.
        var callerSocietyId = User.GetSocietyId();
        var effectiveSocietyId = callerSocietyId ?? societyId;
        if (effectiveSocietyId.HasValue) query = query.Where(f => f.Wing.Block.SocietyId == effectiveSocietyId);

        var flats = await query
            .OrderBy(f => f.Wing.Block.Name).ThenBy(f => f.Wing.Name).ThenBy(f => f.FlatNumber)
            .Select(f => new FlatDto(f.Id, f.WingId, f.FlatNumber, f.Floor, f.AreaSqFt, f.OccupancyStatus, f.Wing.Name, f.Wing.Block.Name, f.Wing.Block.SocietyId))
            .ToListAsync();
        return Ok(flats);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<FlatDto>> Create(CreateFlatRequest request)
    {
        var wing = await _db.Wings.Include(w => w.Block).FirstOrDefaultAsync(w => w.Id == request.WingId);
        if (wing is null) return BadRequest(new { message = "Wing not found" });

        var callerSocietyId = User.GetSocietyId();
        if (callerSocietyId.HasValue && wing.Block.SocietyId != callerSocietyId.Value)
            return Forbid();

        var flat = new Flat
        {
            WingId = request.WingId,
            FlatNumber = request.FlatNumber,
            Floor = request.Floor,
            AreaSqFt = request.AreaSqFt
        };
        _db.Flats.Add(flat);
        await _db.SaveChangesAsync();
        return Ok(new FlatDto(flat.Id, flat.WingId, flat.FlatNumber, flat.Floor, flat.AreaSqFt, flat.OccupancyStatus, wing.Name, wing.Block.Name, wing.Block.SocietyId));
    }
}

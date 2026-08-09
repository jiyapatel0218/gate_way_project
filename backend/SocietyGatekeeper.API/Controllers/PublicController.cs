using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PublicController(ApplicationDbContext db) => _db = db;

    [HttpGet("societies")]
    public async Task<IActionResult> GetSocieties()
    {
        var societies = await _db.Societies.Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();
        return Ok(societies);
    }

    [HttpGet("flats")]
    public async Task<IActionResult> GetFlats([FromQuery] Guid societyId)
    {
        var flats = await _db.Flats
            .Where(f => f.Wing.Block.SocietyId == societyId)
            .OrderBy(f => f.Wing.Block.Name).ThenBy(f => f.Wing.Name).ThenBy(f => f.FlatNumber)
            .Select(f => new { f.Id, f.FlatNumber, WingName = f.Wing.Name, BlockName = f.Wing.Block.Name, f.OccupancyStatus })
            .ToListAsync();
        return Ok(flats);
    }
}

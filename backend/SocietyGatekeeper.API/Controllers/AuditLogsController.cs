using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "SuperAdmin,SocietyAdmin")]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AuditLogsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? entityName, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);

        var total = await query.CountAsync();
        var logs = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new { a.Id, a.Action, a.EntityName, a.EntityId, a.Details, UserName = a.User != null ? a.User.FullName : "System", a.IpAddress, a.CreatedAt })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items = logs });
    }
}

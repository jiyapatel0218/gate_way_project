using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

// Complaint Categories, Visitor Types, and Maintenance Types are global reference data shared by
// every society (deliberate choice) — only Super Admin manages them, so the dropdown options are
// always identical across all societies.

[ApiController]
[Route("api/masters/complaint-categories")]
[Authorize]
public class ComplaintCategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ComplaintCategoriesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MasterItemDto>>> GetAll()
    {
        var items = await _db.ComplaintCategories
            .Select(c => new MasterItemDto(c.Id, c.Name, c.IsActive))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<MasterItemDto>> Create(CreateMasterItemRequest request)
    {
        var category = new ComplaintCategory { Name = request.Name };
        _db.ComplaintCategories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(new MasterItemDto(category.Id, category.Name, category.IsActive));
    }

    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var category = await _db.ComplaintCategories.FindAsync(id);
        if (category is null) return NotFound();
        category.IsActive = !category.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { category.IsActive });
    }
}

[ApiController]
[Route("api/masters/visitor-types")]
[Authorize]
public class VisitorTypesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public VisitorTypesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MasterItemDto>>> GetAll()
    {
        var items = await _db.VisitorTypes
            .Select(v => new MasterItemDto(v.Id, v.Name, v.IsActive))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<MasterItemDto>> Create(CreateMasterItemRequest request)
    {
        var type = new VisitorType { Name = request.Name };
        _db.VisitorTypes.Add(type);
        await _db.SaveChangesAsync();
        return Ok(new MasterItemDto(type.Id, type.Name, type.IsActive));
    }
}

[ApiController]
[Route("api/masters/maintenance-types")]
[Authorize]
public class MaintenanceTypesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public MaintenanceTypesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAll()
    {
        var items = await _db.MaintenanceTypes
            .Select(m => new { m.Id, m.Name, m.DefaultAmount, m.IsActive })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceTypeRequest request)
    {
        var type = new MaintenanceType { Name = request.Name, DefaultAmount = request.DefaultAmount };
        _db.MaintenanceTypes.Add(type);
        await _db.SaveChangesAsync();
        return Ok(new { type.Id, type.Name, type.DefaultAmount, type.IsActive });
    }
}

public record CreateMaintenanceTypeRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [Range(0.01, 10000000)] decimal DefaultAmount
);

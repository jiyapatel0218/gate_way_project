using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.Validation;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/security-guards")]
[Authorize(Roles = "SuperAdmin,SocietyAdmin")]
public class SecurityGuardsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SecurityGuardsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private IQueryable<SecurityGuard> BaseQuery()
    {
        var societyId = User.GetSocietyId();
        var query = _db.SecurityGuards.Include(g => g.User).AsQueryable();
        if (societyId.HasValue) query = query.Where(g => g.SocietyId == societyId);
        return query;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var guards = await BaseQuery().Select(g => new
        {
            g.Id, g.UserId, g.User.FullName, g.User.Email, g.User.PhoneNumber,
            g.ShiftTiming, g.GuardCode, g.IsActive
        }).ToListAsync();

        return Ok(guards);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGuardRequest request)
    {
        var societyId = User.GetSocietyId() ?? request.SocietyId ?? throw new InvalidOperationException("Society context required");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName,
            Role = UserRole.SecurityGuard,
            SocietyId = societyId,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, nameof(UserRole.SecurityGuard));

        var guard = new SecurityGuard
        {
            UserId = user.Id,
            SocietyId = societyId,
            ShiftTiming = request.ShiftTiming,
            GuardCode = request.GuardCode
        };
        _db.SecurityGuards.Add(guard);
        await _db.SaveChangesAsync();

        return Ok(new { guard.Id, user.FullName, user.Email, guard.ShiftTiming, guard.GuardCode });
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var guard = await BaseQuery().FirstOrDefaultAsync(g => g.Id == id);
        if (guard is null) return NotFound();
        guard.IsActive = !guard.IsActive;
        guard.User.IsActive = guard.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { guard.IsActive });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var guard = await BaseQuery().FirstOrDefaultAsync(g => g.Id == id);
        if (guard is null) return NotFound();
        guard.IsDeleted = true;
        guard.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateGuardRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, Phone, StringLength(20)] string PhoneNumber,
    [Required, StrongPassword] string Password,
    [Required, StringLength(100, MinimumLength = 1)] string ShiftTiming,
    [StringLength(30)] string? GuardCode,
    Guid? SocietyId
);

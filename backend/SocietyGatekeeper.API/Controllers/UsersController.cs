using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Application.Validation;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserRole? role, [FromQuery] Guid? societyId)
    {
        var query = _db.Users.AsQueryable();
        if (role.HasValue) query = query.Where(u => u.Role == role);
        if (societyId.HasValue) query = query.Where(u => u.SocietyId == societyId);

        var users = await query.Select(u => new
        {
            u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role, u.SocietyId, u.IsActive, u.LastLoginAt, u.CreatedAt
        }).ToListAsync();

        return Ok(users);
    }

    [HttpPost("society-admin")]
    public async Task<IActionResult> CreateSocietyAdmin(CreateSocietyAdminRequest request)
    {
        var society = await _db.Societies.FindAsync(request.SocietyId);
        if (society is null) return BadRequest(new { message = "Society not found" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName,
            Role = UserRole.SocietyAdmin,
            SocietyId = request.SocietyId,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, nameof(UserRole.SocietyAdmin));
        return Ok(new { user.Id, user.FullName, user.Email, user.SocietyId });
    }

    [HttpPost("super-admin")]
    public async Task<IActionResult> CreateSuperAdmin(CreateSuperAdminRequest request)
    {
        // Step-up authentication: creating another Super Admin is highly sensitive, so require the
        // caller to re-confirm their own password even though they're already authenticated.
        var caller = await _userManager.GetUserAsync(User);
        if (caller is null) return Unauthorized();

        var passwordValid = await _userManager.CheckPasswordAsync(caller, request.CurrentAdminPassword);
        if (!passwordValid) return BadRequest(new { message = "Your current password is incorrect." });

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest(new { message = "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName,
            Role = UserRole.SuperAdmin,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, nameof(UserRole.SuperAdmin));
        return Ok(new { user.Id, user.FullName, user.Email });
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { user.IsActive });
    }

    [HttpGet("{id}/login-history")]
    public async Task<IActionResult> GetLoginHistory(Guid id)
    {
        var history = await _db.LoginHistories
            .Where(l => l.UserId == id)
            .OrderByDescending(l => l.LoginAt)
            .Take(50)
            .Select(l => new { l.LoginAt, l.IpAddress, l.UserAgent, l.Success })
            .ToListAsync();

        return Ok(history);
    }
}

public record CreateSocietyAdminRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, Phone, StringLength(20)] string PhoneNumber,
    [Required, StrongPassword] string Password,
    [Required] Guid SocietyId
);

public record CreateSuperAdminRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, Phone, StringLength(20)] string PhoneNumber,
    [Required, StrongPassword] string Password,
    [Required] string CurrentAdminPassword
);

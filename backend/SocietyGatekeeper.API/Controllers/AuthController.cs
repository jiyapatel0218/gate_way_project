using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Application.Interfaces;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext db,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var (response, error, statusCode) = await AuthenticateAsync(request, requireSuperAdmin: false, blockSuperAdmin: true);
        if (error is not null) return StatusCode(statusCode, new { message = error });
        return Ok(response);
    }

    [HttpPost("admin-login")]
    public async Task<ActionResult<LoginResponse>> AdminLogin(LoginRequest request)
    {
        var (response, error, statusCode) = await AuthenticateAsync(request, requireSuperAdmin: true, blockSuperAdmin: false);
        if (error is not null) return StatusCode(statusCode, new { message = error });
        return Ok(response);
    }

    private async Task<(LoginResponse? Response, string? Error, int StatusCode)> AuthenticateAsync(
        LoginRequest request, bool requireSuperAdmin, bool blockSuperAdmin)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (user is null || !user.IsActive)
        {
            return (null, "Invalid credentials", StatusCodes.Status401Unauthorized);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        _db.LoginHistories.Add(new LoginHistory
        {
            UserId = user.Id,
            IpAddress = ip,
            UserAgent = Request.Headers.UserAgent.ToString(),
            Success = result.Succeeded
        });
        await _db.SaveChangesAsync();

        if (!result.Succeeded)
        {
            return (null, result.IsLockedOut ? "Account locked. Try again later." : "Invalid credentials", StatusCodes.Status401Unauthorized);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isSuperAdmin = roles.Contains(nameof(UserRole.SuperAdmin));

        if (requireSuperAdmin && !isSuperAdmin)
            return (null, "This login is reserved for Super Admin accounts.", StatusCodes.Status403Forbidden);

        if (blockSuperAdmin && isSuperAdmin)
            return (null, "Super Admin accounts must sign in via the dedicated Super Admin login.", StatusCodes.Status403Forbidden);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var response = new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(expiryMinutes),
            user.Id,
            user.FullName,
            user.Email!,
            roles.FirstOrDefault() ?? string.Empty,
            user.SocietyId
        );

        return (response, null, StatusCodes.Status200OK);
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterResidentRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest(new { message = "An account with this email already exists." });

        var flat = await _db.Flats.Include(f => f.Wing).ThenInclude(w => w.Block)
            .FirstOrDefaultAsync(f => f.Id == request.FlatId);
        if (flat is null) return BadRequest(new { message = "Selected flat not found." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName,
            Role = UserRole.Resident,
            SocietyId = flat.Wing.Block.SocietyId,
            ProfileImageUrl = request.ProfileImageUrl,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, nameof(UserRole.Resident));

        var resident = new Resident { UserId = user.Id, FlatId = request.FlatId, IsOwner = request.IsOwner };
        _db.Residents.Add(resident);

        if (flat.OccupancyStatus == FlatOccupancyStatus.Vacant)
            flat.OccupancyStatus = request.IsOwner ? FlatOccupancyStatus.Owner : FlatOccupancyStatus.Tenant;

        await _db.SaveChangesAsync();

        return Ok(await BuildLoginResponseAsync(user));
    }

    // Society Secretary and Security Guard accounts are privileged (they grant access to the
    // gate/admin application, not just a resident's own flat) and must never be self-service.
    // They can only be created by an already-authenticated SuperAdmin/SocietyAdmin, via
    // UsersController.CreateSocietyAdmin and SecurityGuardsController.Create respectively.

    private async Task<LoginResponse> BuildLoginResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

        return new LoginResponse(
            accessToken, refreshToken, DateTime.UtcNow.AddMinutes(expiryMinutes),
            user.Id, user.FullName, user.Email!, roles.FirstOrDefault() ?? string.Empty, user.SocietyId
        );
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        // Always return OK to avoid user enumeration
        if (user is null) return Ok(new { message = "If the account exists, a reset link has been sent." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // In production this token would be emailed. Returned here only for local dev/testing.
        return Ok(new { message = "If the account exists, a reset link has been sent.", devToken = token });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return BadRequest(new { message = "Invalid request" });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await LogAuditAsync(user.Id, "PasswordResetCompleted", "User", user.Id.ToString(), null);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully" });
    }

    // ---------- OTP-based password reset ----------
    // Delivery integration (real SMTP / SMS provider) is intentionally not wired up yet — no
    // provider credentials are configured. Every generated OTP is returned in the response as
    // `devOtp` so the flow is fully testable, exactly like the existing devToken pattern above.
    // Swap in real email/SMS sending inside SendOtpAsync() once credentials are available; nothing
    // else in this flow (hashing, expiry, attempt/resend limits, audit logging) needs to change.

    private const string AccountNotFoundMessage = "No account found with the provided Email Address or Mobile Number.";

    [HttpPost("forgot-password/request")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> RequestResetOtp(ForgotPasswordOtpRequest request)
    {
        var user = await FindUserByIdentifierAsync(request.Identifier);
        // Deliberately reveals account existence (not the anti-enumeration-safe default) — explicitly
        // requested by the user, who was shown the enumeration trade-off before confirming.
        if (user is null) return NotFound(new { message = AccountNotFoundMessage });

        var deliveryTarget = request.DeliveryMethod == "Sms" ? "your registered Mobile Number" : "your registered Email Address";
        var sentMessage = $"A verification OTP has been sent to {deliveryTarget}.";

        // Invalidate any previous unused OTPs for this user before issuing a new one.
        var previous = await _db.PasswordResetOtps.Where(o => o.UserId == user.Id && !o.IsUsed).ToListAsync();
        foreach (var p in previous) p.IsUsed = true;

        var otp = GenerateOtp();
        _db.PasswordResetOtps.Add(new PasswordResetOtp
        {
            UserId = user.Id,
            OtpHash = HashOtp(otp, user.Id),
            DeliveryMethod = request.DeliveryMethod,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetIntSetting("Otp:ExpiryMinutes", 5)),
            LastSentAt = DateTime.UtcNow
        });
        await LogAuditAsync(user.Id, "PasswordResetOtpRequested", "User", user.Id.ToString(), $"via {request.DeliveryMethod}");
        await _db.SaveChangesAsync();

        return Ok(new { message = sentMessage, devOtp = otp });
    }

    [HttpPost("forgot-password/resend")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> ResendResetOtp(ForgotPasswordOtpRequest request)
    {
        var user = await FindUserByIdentifierAsync(request.Identifier);
        if (user is null) return NotFound(new { message = AccountNotFoundMessage });

        var deliveryTarget = request.DeliveryMethod == "Sms" ? "your registered Mobile Number" : "your registered Email Address";
        var sentMessage = $"A verification OTP has been sent to {deliveryTarget}.";

        var active = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        var maxResends = GetIntSetting("Otp:MaxResends", 3);
        var cooldownSeconds = GetIntSetting("Otp:ResendCooldownSeconds", 30);

        if (active is not null)
        {
            var secondsSinceLastSend = (DateTime.UtcNow - active.LastSentAt).TotalSeconds;
            if (secondsSinceLastSend < cooldownSeconds)
                return BadRequest(new { message = $"Please wait {(int)(cooldownSeconds - secondsSinceLastSend)}s before requesting another code." });

            if (active.ResendCount >= maxResends)
                return BadRequest(new { message = "Maximum resend attempts reached. Please start over." });

            active.IsUsed = true;
        }

        var otp = GenerateOtp();
        _db.PasswordResetOtps.Add(new PasswordResetOtp
        {
            UserId = user.Id,
            OtpHash = HashOtp(otp, user.Id),
            DeliveryMethod = request.DeliveryMethod,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetIntSetting("Otp:ExpiryMinutes", 5)),
            ResendCount = (active?.ResendCount ?? 0) + 1,
            LastSentAt = DateTime.UtcNow
        });
        await LogAuditAsync(user.Id, "PasswordResetOtpResent", "User", user.Id.ToString(), $"via {request.DeliveryMethod}");
        await _db.SaveChangesAsync();

        return Ok(new { message = sentMessage, devOtp = otp });
    }

    [HttpPost("forgot-password/verify-otp")]
    public async Task<ActionResult<VerifyResetOtpResponse>> VerifyResetOtp(VerifyResetOtpRequest request)
    {
        var user = await FindUserByIdentifierAsync(request.Identifier);
        if (user is null) return BadRequest(new { message = "Invalid or expired code." });

        var active = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (active is null || active.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "OTP has expired. Request a new OTP." });

        var maxAttempts = GetIntSetting("Otp:MaxAttempts", 5);
        if (active.AttemptCount >= maxAttempts)
        {
            active.IsUsed = true;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Maximum verification attempts exceeded. Request a new OTP." });
        }

        if (active.OtpHash != HashOtp(request.Otp, user.Id))
        {
            active.AttemptCount++;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Invalid OTP. Please try again." });
        }

        active.IsUsed = true;
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        await LogAuditAsync(user.Id, "PasswordResetOtpVerified", "User", user.Id.ToString(), null);
        await _db.SaveChangesAsync();

        return Ok(new VerifyResetOtpResponse(user.Email!, resetToken));
    }

    private async Task<ApplicationUser?> FindUserByIdentifierAsync(string identifier)
    {
        var byEmail = await _userManager.FindByEmailAsync(identifier);
        if (byEmail is not null) return byEmail;

        return await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
    }

    private static string GenerateOtp() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private string HashOtp(string otp, Guid userId)
    {
        var pepper = _config["Otp:Pepper"] ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes($"{otp}:{userId}:{pepper}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private int GetIntSetting(string key, int fallback) => int.TryParse(_config[key], out var value) ? value : fallback;

    private async Task LogAuditAsync(Guid? userId, string action, string entityName, string? entityId, string? details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await Task.CompletedTask;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.SocietyId,
            Role = roles.FirstOrDefault(),
            user.ProfileImageUrl
        });
    }
}

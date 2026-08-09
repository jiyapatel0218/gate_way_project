using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public NotificationsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMine([FromQuery] bool? unreadOnly)
    {
        var userId = User.GetUserId();
        var query = _db.Notifications.Where(n => n.RecipientUserId == userId);
        if (unreadOnly == true) query = query.Where(n => !n.IsRead);

        var notifications = await query.OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new NotificationDto(n.Id, n.Category, n.Title, n.Message, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = User.GetUserId();
        var count = await _db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = User.GetUserId();
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);
        if (notification is null) return NotFound();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetUserId();
        var notifications = await _db.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notifications)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

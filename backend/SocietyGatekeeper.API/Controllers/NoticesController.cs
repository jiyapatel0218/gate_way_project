using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.API.Common;
using SocietyGatekeeper.Application.DTOs;
using SocietyGatekeeper.Application.Interfaces;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Controllers;

[ApiController]
[Route("api/notices")]
[Authorize]
public class NoticesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public NoticesController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NoticeDto>>> GetAll([FromQuery] NoticeType? type)
    {
        var societyId = User.GetSocietyId();
        var query = _db.Notices.Include(n => n.PublishedByUser).Where(n => n.IsActive).AsQueryable();
        if (societyId.HasValue) query = query.Where(n => n.SocietyId == societyId);
        if (type.HasValue) query = query.Where(n => n.Type == type);

        var notices = await query.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoticeDto(n.Id, n.Title, n.Content, n.Type, n.EventDate, n.PublishedByUser.FullName, n.CreatedAt, n.AttachmentUrl))
            .ToListAsync();

        return Ok(notices);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<ActionResult<NoticeDto>> Create(CreateNoticeRequest request)
    {
        var societyId = User.GetSocietyId() ?? throw new InvalidOperationException("Society context required");
        var userId = User.GetUserId();

        var notice = new Notice
        {
            SocietyId = societyId,
            Title = request.Title,
            Content = request.Content,
            Type = request.Type,
            EventDate = request.EventDate,
            AttachmentUrl = request.AttachmentUrl,
            PublishedByUserId = userId
        };
        _db.Notices.Add(notice);
        await _db.SaveChangesAsync();

        var residentUserIds = await _db.Residents
            .Where(r => r.IsActive && r.Flat.Wing.Block.SocietyId == societyId)
            .Select(r => r.UserId).ToListAsync();

        var category = request.Type switch
        {
            NoticeType.Emergency => NotificationCategory.Emergency,
            NoticeType.Event => NotificationCategory.Event,
            _ => NotificationCategory.Notice
        };

        foreach (var residentUserId in residentUserIds)
            await _notificationService.NotifyUserAsync(residentUserId, category, notice.Title, request.Content, $"/notices/{notice.Id}");

        var user = await _db.Users.FindAsync(userId);
        return Ok(new NoticeDto(notice.Id, notice.Title, notice.Content, notice.Type, notice.EventDate, user?.FullName ?? "", notice.CreatedAt, notice.AttachmentUrl));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var notice = await _db.Notices.FindAsync(id);
        if (notice is null) return NotFound();
        notice.IsActive = false;
        notice.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

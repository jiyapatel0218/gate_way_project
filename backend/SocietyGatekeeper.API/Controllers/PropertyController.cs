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
[Route("api/property-listings")]
[Authorize]
public class PropertyController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public PropertyController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    private static PropertyListingDto ToDto(PropertyListing p) => new(
        p.Id, p.Title, p.Description, p.Type, p.Price, p.ContactPhone, p.ContactEmail,
        p.Status, p.Resident.User.FullName, p.Images.Select(i => i.ImageUrl).ToList(), p.CreatedAt
    );

    [HttpGet]
    public async Task<ActionResult<List<PropertyListingDto>>> GetAll([FromQuery] ListingStatus? status, [FromQuery] ListingType? type)
    {
        var societyId = User.GetSocietyId();
        var query = _db.PropertyListings
            .Include(p => p.Resident).ThenInclude(r => r.User)
            .Include(p => p.Images)
            .AsQueryable();

        if (societyId.HasValue) query = query.Where(p => p.SocietyId == societyId);

        // Residents only see approved listings by default (plus their own of any status)
        if (User.IsInRole("Resident") && !status.HasValue)
        {
            var userId = User.GetUserId();
            query = query.Where(p => p.Status == ListingStatus.Approved || p.Resident.UserId == userId);
        }
        else if (status.HasValue)
        {
            query = query.Where(p => p.Status == status);
        }

        if (type.HasValue) query = query.Where(p => p.Type == type);

        var listings = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(listings.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PropertyListingDto>> GetById(Guid id)
    {
        var listing = await _db.PropertyListings
            .Include(p => p.Resident).ThenInclude(r => r.User)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (listing is null) return NotFound();
        return Ok(ToDto(listing));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Resident")]
    public async Task<ActionResult<List<PropertyListingDto>>> GetMine()
    {
        var userId = User.GetUserId();
        var listings = await _db.PropertyListings
            .Include(p => p.Resident).ThenInclude(r => r.User)
            .Include(p => p.Images)
            .Where(p => p.Resident.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(listings.Select(ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Resident")]
    public async Task<ActionResult<PropertyListingDto>> Create(CreatePropertyListingRequest request)
    {
        var userId = User.GetUserId();
        var resident = await _db.Residents.Include(r => r.Flat).ThenInclude(f => f.Wing).ThenInclude(w => w.Block)
            .FirstOrDefaultAsync(r => r.UserId == userId);
        if (resident is null) return BadRequest(new { message = "Resident profile not found" });

        var listing = new PropertyListing
        {
            SocietyId = resident.Flat.Wing.Block.SocietyId,
            ResidentId = resident.Id,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Price = request.Price,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            Status = ListingStatus.PendingApproval
        };

        if (request.ImageUrls is not null)
            foreach (var url in request.ImageUrls)
                listing.Images.Add(new PropertyImage { ImageUrl = url });

        _db.PropertyListings.Add(listing);
        await _db.SaveChangesAsync();

        var full = await _db.PropertyListings.Include(p => p.Resident).ThenInclude(r => r.User).Include(p => p.Images).FirstAsync(p => p.Id == listing.Id);
        return Ok(ToDto(full));
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "SuperAdmin,SocietyAdmin")]
    public async Task<IActionResult> Approve(Guid id, ApprovePropertyListingRequest request)
    {
        var listing = await _db.PropertyListings.Include(p => p.Resident).FirstOrDefaultAsync(p => p.Id == id);
        if (listing is null) return NotFound();

        listing.Status = request.Approve ? ListingStatus.Approved : ListingStatus.Rejected;
        listing.RejectionReason = request.Approve ? null : request.RejectionReason;
        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notificationService.NotifyUserAsync(
            listing.Resident.UserId, NotificationCategory.Property,
            "Property Listing Update", $"Your listing '{listing.Title}' was {listing.Status}.", $"/property-listings/{listing.Id}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var listing = await _db.PropertyListings.Include(p => p.Resident).FirstOrDefaultAsync(p => p.Id == id);
        if (listing is null) return NotFound();

        if (User.IsInRole("Resident") && listing.Resident.UserId != userId) return Forbid();

        listing.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

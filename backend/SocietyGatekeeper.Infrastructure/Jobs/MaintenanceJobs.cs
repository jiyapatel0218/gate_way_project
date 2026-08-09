using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.Infrastructure.Jobs;

public class MaintenanceJobs
{
    private readonly ApplicationDbContext _db;

    public MaintenanceJobs(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task GenerateMonthlyMaintenanceForAllSocietiesAsync()
    {
        var month = DateTime.UtcNow.ToString("MMMM");
        var year = DateTime.UtcNow.Year;
        var dueDate = new DateTime(year, DateTime.UtcNow.Month, 10, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        // Maintenance Types are now global (shared by every society), so generate invoices for
        // every occupied flat across all societies for each active type.
        var maintenanceTypes = await _db.MaintenanceTypes.Where(m => m.IsActive).ToListAsync();
        var flats = await _db.Flats
            .Include(f => f.Wing).ThenInclude(w => w.Block)
            .Where(f => f.OccupancyStatus != FlatOccupancyStatus.Vacant)
            .ToListAsync();

        foreach (var type in maintenanceTypes)
        {
            foreach (var flat in flats)
            {
                var exists = await _db.MaintenanceInvoices.AnyAsync(m =>
                    m.FlatId == flat.Id && m.MaintenanceTypeId == type.Id && m.Month == month && m.Year == year);

                if (exists) continue;

                _db.MaintenanceInvoices.Add(new MaintenanceInvoice
                {
                    SocietyId = flat.Wing.Block.SocietyId,
                    FlatId = flat.Id,
                    MaintenanceTypeId = type.Id,
                    Month = month,
                    Year = year,
                    Amount = type.DefaultAmount,
                    DueDate = dueDate,
                    Status = PaymentStatus.Pending
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task MarkOverdueInvoicesAsync()
    {
        var overdue = await _db.MaintenanceInvoices
            .Where(m => m.Status == PaymentStatus.Pending && m.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invoice in overdue)
            invoice.Status = PaymentStatus.Overdue;

        await _db.SaveChangesAsync();
    }
}

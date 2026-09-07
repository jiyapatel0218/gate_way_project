using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Domain.Entities;
using SocietyGatekeeper.Domain.Enums;
using SocietyGatekeeper.Infrastructure.Data;

namespace SocietyGatekeeper.API.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Roles
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole { Name = role, Description = role });
        }

        // Super Admin
        var superAdminSection = config.GetSection("SuperAdmin");
        var superAdminEmail = superAdminSection["Email"]!;

        if (await userManager.FindByEmailAsync(superAdminEmail) is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FullName = superAdminSection["FullName"] ?? "Super Administrator",
                Role = UserRole.SuperAdmin,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(superAdmin, superAdminSection["Password"]!);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(superAdmin, nameof(UserRole.SuperAdmin));
        }

        // Demo society + structure (only if none exists) so the app isn't empty on first run
        if (!await db.Societies.AnyAsync())
        {
            var society = new Society
            {
                Name = "GreenGate Residency",
                Address = "123 Palm Avenue",
                City = "Ahmedabad",
                State = "Gujarat",
                PinCode = "380001",
                ContactEmail = "office@greengate.com",
                ContactPhone = "9999999999"
            };
            db.Societies.Add(society);

            var block = new Block { Society = society, Name = "Block A" };
            db.Blocks.Add(block);

            var wing = new Wing { Block = block, Name = "Wing 1", TotalFloors = 10 };
            db.Wings.Add(wing);

            for (int i = 1; i <= 10; i++)
            {
                db.Flats.Add(new Flat
                {
                    Wing = wing,
                    FlatNumber = $"10{i}",
                    Floor = i,
                    AreaSqFt = 1050,
                    OccupancyStatus = FlatOccupancyStatus.Vacant
                });
            }

            // Global master data (shared by every society) — only seeded once, on first run.
            if (!await db.ComplaintCategories.AnyAsync())
            {
                db.ComplaintCategories.AddRange(
                    new ComplaintCategory { Name = "Plumbing" },
                    new ComplaintCategory { Name = "Electrical" },
                    new ComplaintCategory { Name = "Housekeeping" },
                    new ComplaintCategory { Name = "Security" },
                    new ComplaintCategory { Name = "Other" }
                );
            }

            if (!await db.VisitorTypes.AnyAsync())
            {
                db.VisitorTypes.AddRange(
                    new VisitorType { Name = "Guest" },
                    new VisitorType { Name = "Delivery" },
                    new VisitorType { Name = "Cab/Auto" },
                    new VisitorType { Name = "Domestic Staff" },
                    new VisitorType { Name = "Vendor" }
                );
            }

            if (!await db.MaintenanceTypes.AnyAsync())
            {
                db.MaintenanceTypes.Add(new MaintenanceType { Name = "General Maintenance", DefaultAmount = 2500 });
            }

            await db.SaveChangesAsync();

            // Society Admin
            if (await userManager.FindByEmailAsync("admin@greengate.com") is null)
            {
                var societyAdmin = new ApplicationUser
                {
                    UserName = "admin@greengate.com",
                    Email = "admin@greengate.com",
                    FullName = "Society Admin",
                    Role = UserRole.SocietyAdmin,
                    SocietyId = society.Id,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var res = await userManager.CreateAsync(societyAdmin, "Admin@123");
                if (res.Succeeded)
                    await userManager.AddToRoleAsync(societyAdmin, nameof(UserRole.SocietyAdmin));
            }

            // Security Guard
            if (await userManager.FindByEmailAsync("guard@greengate.com") is null)
            {
                var guardUser = new ApplicationUser
                {
                    UserName = "guard@greengate.com",
                    Email = "guard@greengate.com",
                    FullName = "Ramesh Kumar",
                    Role = UserRole.SecurityGuard,
                    SocietyId = society.Id,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var res = await userManager.CreateAsync(guardUser, "Guard@123");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(guardUser, nameof(UserRole.SecurityGuard));
                    db.SecurityGuards.Add(new SecurityGuard
                    {
                        UserId = guardUser.Id,
                        SocietyId = society.Id,
                        ShiftTiming = "Day (8AM-8PM)",
                        GuardCode = "G-001"
                    });
                    await db.SaveChangesAsync();
                }
            }

            // Demo Resident in flat 101
            if (await userManager.FindByEmailAsync("resident@greengate.com") is null)
            {
                var flat101 = await db.Flats.FirstAsync(f => f.FlatNumber == "101");
                var residentUser = new ApplicationUser
                {
                    UserName = "resident@greengate.com",
                    Email = "resident@greengate.com",
                    FullName = "Anita Sharma",
                    Role = UserRole.Resident,
                    SocietyId = society.Id,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var res = await userManager.CreateAsync(residentUser, "Resident@123");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(residentUser, nameof(UserRole.Resident));
                    db.Residents.Add(new Resident
                    {
                        UserId = residentUser.Id,
                        FlatId = flat101.Id,
                        IsOwner = true
                    });
                    flat101.OccupancyStatus = FlatOccupancyStatus.Owner;
                    await db.SaveChangesAsync();
                }
            }
        }

        // Default payment QR assignment for the demo society, so online payments keep working
        // out of the box until an admin assigns/replaces it from the Payment QR screen. Runs on
        // every startup (not just first-run seeding above) so it also backfills existing databases.
        if (!await db.PaymentQrAssignments.AnyAsync())
        {
            var demoSociety = await db.Societies.FirstOrDefaultAsync(s => s.Name == "GreenGate Residency");
            var demoAdmin = await userManager.FindByEmailAsync("admin@greengate.com");
            if (demoSociety is not null && demoAdmin is not null)
            {
                db.PaymentQrAssignments.Add(new PaymentQrAssignment
                {
                    SocietyId = demoSociety.Id,
                    BlockId = null,
                    AssignedToUserId = demoAdmin.Id,
                    QrImageUrl = "/uploads/qr-codes/payment-qr.jpg",
                    PayeeName = "GreenGate Residency"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}

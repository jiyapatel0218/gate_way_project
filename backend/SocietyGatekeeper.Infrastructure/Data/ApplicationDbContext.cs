using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocietyGatekeeper.Domain.Entities;

namespace SocietyGatekeeper.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Society> Societies => Set<Society>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Wing> Wings => Set<Wing>();
    public DbSet<Flat> Flats => Set<Flat>();

    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<ResidentDocument> ResidentDocuments => Set<ResidentDocument>();
    public DbSet<SecurityGuard> SecurityGuards => Set<SecurityGuard>();

    public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintRemark> ComplaintRemarks => Set<ComplaintRemark>();

    public DbSet<VisitorType> VisitorTypes => Set<VisitorType>();
    public DbSet<Visitor> Visitors => Set<Visitor>();

    public DbSet<MaintenanceType> MaintenanceTypes => Set<MaintenanceType>();
    public DbSet<MaintenanceInvoice> MaintenanceInvoices => Set<MaintenanceInvoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<PropertyListing> PropertyListings => Set<PropertyListing>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();

    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PaymentQrAssignment> PaymentQrAssignments => Set<PaymentQrAssignment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables for clarity
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // Global query filters for soft delete
        builder.Entity<Society>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Block>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Wing>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Flat>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Resident>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Complaint>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Visitor>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PropertyListing>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Notice>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PaymentQrAssignment>().HasQueryFilter(e => !e.IsDeleted);

        // Society hierarchy
        builder.Entity<Block>()
            .HasOne(b => b.Society).WithMany(s => s.Blocks)
            .HasForeignKey(b => b.SocietyId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Wing>()
            .HasOne(w => w.Block).WithMany(b => b.Wings)
            .HasForeignKey(w => w.BlockId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Flat>()
            .HasOne(f => f.Wing).WithMany(w => w.Flats)
            .HasForeignKey(f => f.WingId).OnDelete(DeleteBehavior.Cascade);

        // Resident
        builder.Entity<Resident>()
            .HasOne(r => r.User).WithOne(u => u.ResidentProfile)
            .HasForeignKey<Resident>(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Resident>()
            .HasOne(r => r.Flat).WithMany(f => f.Residents)
            .HasForeignKey(r => r.FlatId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FamilyMember>()
            .HasOne(f => f.Resident).WithMany(r => r.FamilyMembers)
            .HasForeignKey(f => f.ResidentId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vehicle>()
            .HasOne(v => v.Resident).WithMany(r => r.Vehicles)
            .HasForeignKey(v => v.ResidentId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmergencyContact>()
            .HasOne(e => e.Resident).WithMany(r => r.EmergencyContacts)
            .HasForeignKey(e => e.ResidentId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResidentDocument>()
            .HasOne(d => d.Resident).WithMany(r => r.Documents)
            .HasForeignKey(d => d.ResidentId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SecurityGuard>()
            .HasOne(g => g.User).WithOne(u => u.SecurityGuardProfile)
            .HasForeignKey<SecurityGuard>(g => g.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SecurityGuard>()
            .HasOne(g => g.Society).WithMany()
            .HasForeignKey(g => g.SocietyId).OnDelete(DeleteBehavior.Restrict);

        // Complaints
        builder.Entity<Complaint>()
            .HasOne(c => c.Resident).WithMany()
            .HasForeignKey(c => c.ResidentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Complaint>()
            .HasOne(c => c.Category).WithMany()
            .HasForeignKey(c => c.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Complaint>()
            .HasOne(c => c.AssignedToUser).WithMany()
            .HasForeignKey(c => c.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ComplaintRemark>()
            .HasOne(r => r.Complaint).WithMany(c => c.Remarks)
            .HasForeignKey(r => r.ComplaintId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ComplaintRemark>()
            .HasOne(r => r.AddedByUser).WithMany()
            .HasForeignKey(r => r.AddedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Visitors
        builder.Entity<Visitor>()
            .HasOne(v => v.Flat).WithMany()
            .HasForeignKey(v => v.FlatId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Visitor>()
            .HasOne(v => v.CreatedByGuardUser).WithMany()
            .HasForeignKey(v => v.CreatedByGuardUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Visitor>()
            .HasOne(v => v.ApprovedByResident).WithMany()
            .HasForeignKey(v => v.ApprovedByResidentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Visitor>()
            .HasOne(v => v.VisitorTypeRef).WithMany()
            .HasForeignKey(v => v.VisitorTypeId).OnDelete(DeleteBehavior.Restrict);

        // Maintenance
        builder.Entity<MaintenanceInvoice>()
            .HasOne(m => m.Flat).WithMany(f => f.MaintenanceInvoices)
            .HasForeignKey(m => m.FlatId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MaintenanceInvoice>()
            .HasOne(m => m.MaintenanceType).WithMany()
            .HasForeignKey(m => m.MaintenanceTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.MaintenanceInvoice).WithMany(m => m.Payments)
            .HasForeignKey(p => p.MaintenanceInvoiceId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasOne(p => p.RecordedByUser).WithMany()
            .HasForeignKey(p => p.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentOrder>()
            .HasOne(o => o.MaintenanceInvoice).WithMany()
            .HasForeignKey(o => o.MaintenanceInvoiceId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentOrder>()
            .HasOne(o => o.CreatedByUser).WithMany()
            .HasForeignKey(o => o.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Payment).WithMany()
            .HasForeignKey(i => i.PaymentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.MaintenanceInvoice).WithMany()
            .HasForeignKey(i => i.MaintenanceInvoiceId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Resident).WithMany()
            .HasForeignKey(i => i.ResidentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>().Property(i => i.AmountPaid).HasColumnType("decimal(12,2)");
        builder.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.Entity<Invoice>().HasIndex(i => i.PaymentId).IsUnique();

        builder.Entity<PaymentOrder>()
            .Property(o => o.Amount).HasColumnType("decimal(12,2)");

        builder.Entity<PaymentOrder>().HasIndex(o => o.ProviderOrderId).IsUnique();

        builder.Entity<MaintenanceInvoice>()
            .Property(m => m.Amount).HasColumnType("decimal(12,2)");
        builder.Entity<MaintenanceInvoice>()
            .Property(m => m.PenaltyAmount).HasColumnType("decimal(12,2)");
        builder.Entity<MaintenanceInvoice>()
            .Property(m => m.PaidAmount).HasColumnType("decimal(12,2)");
        builder.Entity<Payment>()
            .Property(p => p.Amount).HasColumnType("decimal(12,2)");
        builder.Entity<MaintenanceType>()
            .Property(m => m.DefaultAmount).HasColumnType("decimal(12,2)");

        // Payment QR assignments
        builder.Entity<PaymentQrAssignment>()
            .HasOne(a => a.Society).WithMany()
            .HasForeignKey(a => a.SocietyId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentQrAssignment>()
            .HasOne(a => a.Block).WithMany()
            .HasForeignKey(a => a.BlockId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentQrAssignment>()
            .HasOne(a => a.AssignedToUser).WithMany()
            .HasForeignKey(a => a.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaymentQrAssignment>().HasIndex(a => new { a.SocietyId, a.BlockId });
        builder.Entity<PropertyListing>()
            .Property(p => p.Price).HasColumnType("decimal(14,2)");

        // Property
        builder.Entity<PropertyListing>()
            .HasOne(p => p.Resident).WithMany()
            .HasForeignKey(p => p.ResidentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PropertyImage>()
            .HasOne(i => i.PropertyListing).WithMany(p => p.Images)
            .HasForeignKey(i => i.PropertyListingId).OnDelete(DeleteBehavior.Cascade);

        // Notice
        builder.Entity<Notice>()
            .HasOne(n => n.PublishedByUser).WithMany()
            .HasForeignKey(n => n.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Notification
        builder.Entity<Notification>()
            .HasOne(n => n.RecipientUser).WithMany()
            .HasForeignKey(n => n.RecipientUserId).OnDelete(DeleteBehavior.Cascade);

        // Audit / Login history
        builder.Entity<AuditLog>()
            .HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LoginHistory>()
            .HasOne(l => l.User).WithMany()
            .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PasswordResetOtp>()
            .HasOne(o => o.User).WithMany()
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);

        // Unique constraints
        builder.Entity<Flat>().HasIndex(f => new { f.WingId, f.FlatNumber }).IsUnique();
        builder.Entity<MaintenanceInvoice>().HasIndex(m => new { m.FlatId, m.MaintenanceTypeId, m.Month, m.Year }).IsUnique();
    }
}

using SocietyGatekeeper.Domain.Common;

namespace SocietyGatekeeper.Domain.Entities;

public class Resident : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public bool IsOwner { get; set; }
    public string? AlternatePhone { get; set; }
    public DateTime MoveInDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<ResidentDocument> Documents { get; set; } = new List<ResidentDocument>();
}

public class FamilyMember : BaseEntity
{
    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Phone { get; set; }
}

public class Vehicle : BaseEntity
{
    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? Model { get; set; }
}

public class EmergencyContact : BaseEntity
{
    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
}

public class ResidentDocument : BaseEntity
{
    public Guid ResidentId { get; set; }
    public Resident Resident { get; set; } = null!;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
}

public class SecurityGuard : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string ShiftTiming { get; set; } = string.Empty;
    public string? GuardCode { get; set; }
    public bool IsActive { get; set; } = true;
}

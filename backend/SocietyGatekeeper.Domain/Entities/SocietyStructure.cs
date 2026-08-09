using SocietyGatekeeper.Domain.Common;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Domain.Entities;

public class Society : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Block> Blocks { get; set; } = new List<Block>();
}

public class Block : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Wing> Wings { get; set; } = new List<Wing>();
}

public class Wing : BaseEntity
{
    public Guid BlockId { get; set; }
    public Block Block { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int TotalFloors { get; set; }

    public ICollection<Flat> Flats { get; set; } = new List<Flat>();
}

public class Flat : BaseEntity
{
    public Guid WingId { get; set; }
    public Wing Wing { get; set; } = null!;
    public string FlatNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public double AreaSqFt { get; set; }
    public FlatOccupancyStatus OccupancyStatus { get; set; } = FlatOccupancyStatus.Vacant;

    public ICollection<Resident> Residents { get; set; } = new List<Resident>();
    public ICollection<MaintenanceInvoice> MaintenanceInvoices { get; set; } = new List<MaintenanceInvoice>();
}

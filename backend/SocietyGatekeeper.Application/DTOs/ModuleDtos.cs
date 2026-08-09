using System.ComponentModel.DataAnnotations;
using SocietyGatekeeper.Application.Validation;
using SocietyGatekeeper.Domain.Enums;

namespace SocietyGatekeeper.Application.DTOs;

// ---------- Society Structure ----------
public record SocietyDto(Guid Id, string Name, string Address, string City, string State, string PinCode, string? ContactEmail, string? ContactPhone, bool IsActive);

public record CreateSocietyRequest(
    [Required, StringLength(150, MinimumLength = 2)] string Name,
    [Required, StringLength(300)] string Address,
    [Required, StringLength(100)] string City,
    [Required, StringLength(100)] string State,
    [Required, RegularExpression(@"^\d{4,10}$", ErrorMessage = "PIN code must be 4-10 digits.")] string PinCode,
    [EmailAddress, StringLength(256)] string? ContactEmail,
    [Phone, StringLength(20)] string? ContactPhone
);

public record BlockDto(Guid Id, Guid SocietyId, string Name, string? Description);
public record CreateBlockRequest(
    [Required] Guid SocietyId,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(300)] string? Description
);

public record WingDto(Guid Id, Guid BlockId, string Name, int TotalFloors);
public record CreateWingRequest(
    [Required] Guid BlockId,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [Range(1, 200)] int TotalFloors
);

public record FlatDto(Guid Id, Guid WingId, string FlatNumber, int Floor, double AreaSqFt, FlatOccupancyStatus OccupancyStatus, string WingName, string BlockName, Guid SocietyId);
public record CreateFlatRequest(
    [Required] Guid WingId,
    [Required, StringLength(20, MinimumLength = 1)] string FlatNumber,
    [Range(0, 300)] int Floor,
    [Range(1, 100000)] double AreaSqFt
);

// ---------- Resident ----------
public record ResidentDto(
    Guid Id, Guid UserId, string FullName, string Email, string? PhoneNumber,
    Guid FlatId, string FlatNumber, string WingName, string BlockName,
    Guid SocietyId, string SocietyName,
    bool IsOwner, bool IsActive, DateTime MoveInDate
);

public record CreateResidentRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, Phone, StringLength(20)] string PhoneNumber,
    [Required, StrongPassword] string Password,
    [Required] Guid FlatId,
    bool IsOwner
);

public record UpdateResidentRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Phone, StringLength(20)] string? AlternatePhone,
    [Required] Guid FlatId,
    bool IsOwner,
    bool IsActive
);

public record FamilyMemberDto(Guid Id, string Name, string Relation, int? Age, string? Phone);
public record CreateFamilyMemberRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    [Required, StringLength(50, MinimumLength = 1)] string Relation,
    [Range(0, 130)] int? Age,
    [Phone, StringLength(20)] string? Phone
);

public record VehicleDto(Guid Id, string VehicleNumber, string VehicleType, string? Model);
public record CreateVehicleRequest(
    [Required, StringLength(20, MinimumLength = 1)] string VehicleNumber,
    [Required, StringLength(30, MinimumLength = 1)] string VehicleType,
    [StringLength(50)] string? Model
);

public record EmergencyContactDto(Guid Id, string Name, string Phone, string Relation);
public record CreateEmergencyContactRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    [Required, Phone, StringLength(20)] string Phone,
    [Required, StringLength(50, MinimumLength = 1)] string Relation
);

// ---------- Complaint ----------
public record ComplaintDto(
    Guid Id, string Title, string Description, ComplaintStatus Status,
    string CategoryName, Guid CategoryId, string ResidentName, string FlatNumber,
    string? AttachmentUrl, string? AssignedToName, DateTime CreatedAt, DateTime? ClosedAt
);

public record CreateComplaintRequest(
    [Required] Guid CategoryId,
    [Required, StringLength(150, MinimumLength = 3)] string Title,
    [Required, StringLength(2000, MinimumLength = 5)] string Description,
    [StringLength(500)] string? AttachmentUrl
);
public record AssignComplaintRequest([Required] Guid AssignedToUserId);
public record UpdateComplaintStatusRequest(
    [Required, EnumDataType(typeof(ComplaintStatus))] ComplaintStatus Status,
    [StringLength(1000)] string? Remark
);

public record ComplaintRemarkDto(Guid Id, string Remark, string AddedByName, ComplaintStatus StatusAtTime, DateTime CreatedAt);

// ---------- Visitor ----------
public record VisitorDto(
    Guid Id, string Name, string MobileNumber, string FlatNumber, Guid FlatId,
    string Purpose, VisitorEntryType EntryType, string? VehicleNumber, string? PhotoUrl,
    VisitorStatus Status, DateTime EntryTime, DateTime? ExitTime, string CreatedByGuardName
);

public record CreateVisitorRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    [Required, Phone, StringLength(20)] string MobileNumber,
    [Required] Guid FlatId,
    [Required, StringLength(200, MinimumLength = 1)] string Purpose,
    [Required, EnumDataType(typeof(VisitorEntryType))] VisitorEntryType EntryType,
    Guid? VisitorTypeId,
    [StringLength(20)] string? VehicleNumber,
    [StringLength(500)] string? PhotoUrl
);

public record VisitorActionRequest([Required, EnumDataType(typeof(VisitorStatus))] VisitorStatus Status);

// ---------- Maintenance ----------
public record MaintenanceInvoiceDto(
    Guid Id, string FlatNumber, string WingName, string BlockName, string MaintenanceTypeName,
    string Month, int Year, decimal Amount, decimal PenaltyAmount, decimal PaidAmount,
    DateTime DueDate, PaymentStatus Status
);

public record GenerateMaintenanceRequest(
    [Required] Guid MaintenanceTypeId,
    [Required, StringLength(20, MinimumLength = 3)] string Month,
    [Range(2000, 2100)] int Year,
    [Required] DateTime DueDate,
    Guid? BlockId
);

public record RecordPaymentRequest(
    [Range(0.01, 10000000)] decimal Amount,
    [Required, EnumDataType(typeof(PaymentMode))] PaymentMode Mode,
    [StringLength(100)] string? TransactionReference,
    [StringLength(500)] string? Notes
);
public record PaymentDto(Guid Id, decimal Amount, PaymentMode Mode, string? TransactionReference, DateTime PaidOn, string? ReceiptNumber, string RecordedByName, Guid? InvoiceId, string? InvoiceNumber);

// ---------- Invoice ----------
public record InvoiceDto(
    Guid Id, string InvoiceNumber, string SocietyName, string? SocietyLogoUrl, string FlatNumber, string BlockName, string WingName,
    string OwnerName, string? OwnerPhone, string? OwnerEmail, string BillingPeriod, decimal AmountPaid,
    string PaymentMode, string? TransactionReference, DateTime PaymentDateTime, DateTime GeneratedAt
);

// ---------- Online Payment Gateway ----------
public record CreatePaymentOrderResponse(Guid InternalOrderId, string ProviderOrderId, string Provider, string? CheckoutKeyId, decimal Amount, string Currency);
public record VerifyPaymentRequest(
    [Required] string ProviderOrderId,
    [Required] string ProviderPaymentId,
    string? Signature
);

// ---------- Property ----------
public record PropertyListingDto(
    Guid Id, string Title, string Description, ListingType Type, decimal Price,
    string ContactPhone, string? ContactEmail, ListingStatus Status, string ResidentName,
    List<string> ImageUrls, DateTime CreatedAt
);

public record CreatePropertyListingRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Title,
    [Required, StringLength(3000, MinimumLength = 5)] string Description,
    [Required, EnumDataType(typeof(ListingType))] ListingType Type,
    [Range(1, 1000000000)] decimal Price,
    [Required, Phone, StringLength(20)] string ContactPhone,
    [EmailAddress, StringLength(256)] string? ContactEmail,
    List<string>? ImageUrls
);
public record ApprovePropertyListingRequest(bool Approve, [StringLength(500)] string? RejectionReason);

// ---------- Notice ----------
public record NoticeDto(Guid Id, string Title, string Content, NoticeType Type, DateTime? EventDate, string PublishedByName, DateTime CreatedAt, string? AttachmentUrl);
public record CreateNoticeRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Title,
    [Required, StringLength(3000, MinimumLength = 3)] string Content,
    [Required, EnumDataType(typeof(NoticeType))] NoticeType Type,
    DateTime? EventDate,
    [StringLength(500)] string? AttachmentUrl
);

// ---------- Notification ----------
public record NotificationDto(Guid Id, NotificationCategory Category, string Title, string Message, string? LinkUrl, bool IsRead, DateTime CreatedAt);

// ---------- Masters ----------
public record MasterItemDto(Guid Id, string Name, bool IsActive);
public record CreateMasterItemRequest([Required, StringLength(100, MinimumLength = 1)] string Name);

// ---------- Dashboard ----------
public record DashboardSummaryDto(
    int TotalResidents, int VisitorsToday, int ActiveComplaints, int PendingComplaints,
    int ClosedComplaints, decimal MonthlyMaintenanceCollection, decimal PendingMaintenance,
    int PropertyListingsCount, int ActiveNoticesCount, List<RecentActivityDto> RecentActivities
);

public record RecentActivityDto(string Type, string Description, DateTime Timestamp);

namespace SocietyGatekeeper.Domain.Enums;

public enum UserRole
{
    SuperAdmin,
    SocietyAdmin,
    Resident,
    SecurityGuard
}

public enum ComplaintStatus
{
    Open,
    Assigned,
    InProgress,
    Completed,
    Closed
}

public enum VisitorStatus
{
    Pending,
    Approved,
    Rejected,
    Entered,
    Exited
}

public enum VisitorEntryType
{
    Visitor,
    Delivery,
    Staff,
    Cab
}

public enum PaymentMode
{
    Online,
    Cash,
    Cheque,
    BankTransfer,
    UPI
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Overdue,
    PartiallyPaid
}

public enum ListingType
{
    Rent,
    Sale
}

public enum ListingStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Closed
}

public enum NoticeType
{
    Notice,
    Event,
    Emergency
}

public enum FlatOccupancyStatus
{
    Owner,
    Tenant,
    Vacant
}

public enum NotificationCategory
{
    Visitor,
    Complaint,
    Maintenance,
    Payment,
    Property,
    Notice,
    Event,
    Emergency
}

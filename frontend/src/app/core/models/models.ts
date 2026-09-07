export type UserRole = 'SuperAdmin' | 'SocietyAdmin' | 'Resident' | 'SecurityGuard';

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  societyId?: string;
}

export interface CurrentUser {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  societyId?: string;
}

export interface Society {
  id: string;
  name: string;
  address: string;
  city: string;
  state: string;
  pinCode: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive: boolean;
}

export interface Block { id: string; societyId: string; name: string; description?: string; }
export interface Wing { id: string; blockId: string; name: string; totalFloors: number; }
export interface Flat { id: string; wingId: string; flatNumber: string; floor: number; areaSqFt: number; occupancyStatus: number; wingName: string; blockName: string; societyId: string; }

export interface PaymentQrAssignment {
  id: string; societyId: string; societyName: string; blockId?: string; blockName?: string;
  assignedToUserId: string; assignedToUserName: string; qrImageUrl: string; payeeName?: string; isActive: boolean;
}
export interface AssignableUser { id: string; fullName: string; email: string; role: UserRole; }
export interface MyPaymentQr { qrImageUrl: string; payeeName?: string; }

export interface Resident {
  id: string; userId: string; fullName: string; email: string; phoneNumber?: string;
  flatId: string; flatNumber: string; wingName: string; blockName: string;
  societyId: string; societyName: string;
  isOwner: boolean; isActive: boolean; moveInDate: string;
}

export interface FamilyMember { id: string; name: string; relation: string; age?: number; phone?: string; }
export interface Vehicle { id: string; vehicleNumber: string; vehicleType: string; model?: string; }
export interface EmergencyContact { id: string; name: string; phone: string; relation: string; }

export type ComplaintStatus = 'Open' | 'Assigned' | 'InProgress' | 'Completed' | 'Closed';

export interface Complaint {
  id: string; title: string; description: string; status: ComplaintStatus;
  categoryName: string; categoryId: string; residentName: string; flatNumber: string;
  attachmentUrl?: string; assignedToName?: string; createdAt: string; closedAt?: string;
}

export interface ComplaintRemark { id: string; remark: string; addedByName: string; statusAtTime: ComplaintStatus; createdAt: string; }

export type VisitorStatus = 'Pending' | 'Approved' | 'Rejected' | 'Entered' | 'Exited';
export type VisitorEntryType = 'Visitor' | 'Delivery' | 'Staff' | 'Cab';

export interface Visitor {
  id: string; name: string; mobileNumber: string; flatNumber: string; flatId: string;
  purpose: string; entryType: VisitorEntryType; vehicleNumber?: string; photoUrl?: string;
  status: VisitorStatus; entryTime: string; exitTime?: string; createdByGuardName: string;
}

export type PaymentStatus = 'Pending' | 'Paid' | 'Overdue' | 'PartiallyPaid';
export type PaymentMode = 'Online' | 'Cash' | 'Cheque' | 'BankTransfer' | 'UPI';

export interface MaintenanceInvoice {
  id: string; flatNumber: string; wingName: string; blockName: string; maintenanceTypeName: string;
  month: string; year: number; amount: number; penaltyAmount: number; paidAmount: number;
  dueDate: string; status: PaymentStatus;
}

export interface Payment {
  id: string; amount: number; mode: PaymentMode; transactionReference?: string;
  paidOn: string; receiptNumber?: string; recordedByName: string;
  invoiceId?: string; invoiceNumber?: string;
}

export interface Invoice {
  id: string; invoiceNumber: string; societyName: string; societyLogoUrl?: string;
  flatNumber: string; blockName: string; wingName: string;
  ownerName: string; ownerPhone?: string; ownerEmail?: string;
  billingPeriod: string; amountPaid: number; paymentMode: string;
  transactionReference?: string; paymentDateTime: string; generatedAt: string;
}

export type ListingType = 'Rent' | 'Sale';
export type ListingStatus = 'PendingApproval' | 'Approved' | 'Rejected' | 'Closed';

export interface PropertyListing {
  id: string; title: string; description: string; type: ListingType; price: number;
  contactPhone: string; contactEmail?: string; status: ListingStatus; residentName: string;
  imageUrls: string[]; createdAt: string;
}

export type NoticeType = 'Notice' | 'Event' | 'Emergency';

export interface Notice {
  id: string; title: string; content: string; type: NoticeType; eventDate?: string;
  publishedByName: string; createdAt: string; attachmentUrl?: string;
}

export type NotificationCategory = 'Visitor' | 'Complaint' | 'Maintenance' | 'Payment' | 'Property' | 'Notice' | 'Event' | 'Emergency';

export interface AppNotification {
  id: string; category: NotificationCategory; title: string; message: string;
  linkUrl?: string; isRead: boolean; createdAt: string;
}

export interface MasterItem { id: string; name: string; isActive: boolean; }

export interface RecentActivity { type: string; description: string; timestamp: string; }

export interface DashboardSummary {
  totalResidents: number; visitorsToday: number; activeComplaints: number; pendingComplaints: number;
  closedComplaints: number; monthlyMaintenanceCollection: number; pendingMaintenance: number;
  propertyListingsCount: number; activeNoticesCount: number; recentActivities: RecentActivity[];
}

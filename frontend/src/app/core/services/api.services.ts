import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssignableUser, Block, Complaint, ComplaintRemark, DashboardSummary, EmergencyContact, FamilyMember,
  Flat, Invoice, MaintenanceInvoice, MasterItem, MyPaymentQr, Notice, Payment, PaymentQrAssignment, PropertyListing, Resident,
  Society, Vehicle, Visitor, Wing
} from '../models/models';

const base = environment.apiUrl;

function cleanParams(params?: Record<string, unknown>): Record<string, string> {
  const result: Record<string, string> = {};
  if (!params) return result;
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      result[key] = String(value);
    }
  }
  return result;
}

@Injectable({ providedIn: 'root' })
export class PublicService {
  constructor(private http: HttpClient) {}
  getSocieties(): Observable<{ id: string; name: string }[]> {
    return this.http.get<{ id: string; name: string }[]>(`${base}/public/societies`);
  }
  getFlats(societyId: string): Observable<{ id: string; flatNumber: string; wingName: string; blockName: string; occupancyStatus: number }[]> {
    return this.http.get<any[]>(`${base}/public/flats`, { params: { societyId } });
  }
}

@Injectable({ providedIn: 'root' })
export class UploadsService {
  constructor(private http: HttpClient) {}
  uploadPhoto(file: File, folder: 'profile-photos' | 'qr-codes' = 'profile-photos'): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${base}/uploads/photo`, formData, { params: { folder } });
  }
}

/** Backend-uploaded files (e.g. /uploads/qr-codes/x.png) are served by the API, not the Angular dev server — resolve them to an absolute URL. Static frontend assets (already absolute, or served from Angular's own public/ folder) pass through unchanged. */
export function resolveMediaUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  if (/^https?:\/\//i.test(url)) return url;
  if (url.startsWith('/uploads/')) return `${base.replace(/\/api\/?$/, '')}${url}`;
  return url;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}
  getSummary(): Observable<DashboardSummary> { return this.http.get<DashboardSummary>(`${base}/dashboard/summary`); }
}

@Injectable({ providedIn: 'root' })
export class SocietyStructureService {
  constructor(private http: HttpClient) {}
  getSocieties(): Observable<Society[]> { return this.http.get<Society[]>(`${base}/societies`); }
  getMySociety(): Observable<Society> { return this.http.get<Society>(`${base}/societies/mine`); }
  createSociety(payload: Partial<Society>): Observable<Society> { return this.http.post<Society>(`${base}/societies`, payload); }
  toggleSocietyActive(id: string): Observable<unknown> { return this.http.patch(`${base}/societies/${id}/toggle-active`, {}); }

  getBlocks(societyId: string): Observable<Block[]> { return this.http.get<Block[]>(`${base}/blocks`, { params: { societyId } }); }
  createBlock(payload: { societyId: string; name: string; description?: string }): Observable<Block> { return this.http.post<Block>(`${base}/blocks`, payload); }
  updateBlock(id: string, payload: { name: string; description?: string }): Observable<Block> { return this.http.put<Block>(`${base}/blocks/${id}`, payload); }
  deleteBlock(id: string): Observable<unknown> { return this.http.delete(`${base}/blocks/${id}`); }

  getWings(blockId: string): Observable<Wing[]> { return this.http.get<Wing[]>(`${base}/wings`, { params: { blockId } }); }
  createWing(payload: { blockId: string; name: string; totalFloors: number }): Observable<Wing> { return this.http.post<Wing>(`${base}/wings`, payload); }

  getFlats(params: { wingId?: string; blockId?: string; societyId?: string }): Observable<Flat[]> {
    const query: Record<string, string> = {};
    if (params.wingId) query['wingId'] = params.wingId;
    if (params.blockId) query['blockId'] = params.blockId;
    if (params.societyId) query['societyId'] = params.societyId;
    return this.http.get<Flat[]>(`${base}/flats`, { params: query });
  }
  createFlat(payload: { wingId: string; flatNumber: string; floor: number; areaSqFt: number }): Observable<Flat> {
    return this.http.post<Flat>(`${base}/flats`, payload);
  }
}

@Injectable({ providedIn: 'root' })
export class MastersService {
  constructor(private http: HttpClient) {}
  getComplaintCategories(): Observable<MasterItem[]> { return this.http.get<MasterItem[]>(`${base}/masters/complaint-categories`); }
  createComplaintCategory(name: string): Observable<MasterItem> { return this.http.post<MasterItem>(`${base}/masters/complaint-categories`, { name }); }

  getVisitorTypes(): Observable<MasterItem[]> { return this.http.get<MasterItem[]>(`${base}/masters/visitor-types`); }
  createVisitorType(name: string): Observable<MasterItem> { return this.http.post<MasterItem>(`${base}/masters/visitor-types`, { name }); }

  getMaintenanceTypes(): Observable<any[]> { return this.http.get<any[]>(`${base}/masters/maintenance-types`); }
  createMaintenanceType(name: string, defaultAmount: number): Observable<any> { return this.http.post(`${base}/masters/maintenance-types`, { name, defaultAmount }); }
}

@Injectable({ providedIn: 'root' })
export class ResidentsService {
  constructor(private http: HttpClient) {}
  getAll(params?: { search?: string; blockId?: string; isActive?: boolean }): Observable<Resident[]> {
    return this.http.get<Resident[]>(`${base}/residents`, { params: cleanParams(params) });
  }
  getMyProfile(): Observable<Resident> { return this.http.get<Resident>(`${base}/residents/me`); }
  getById(id: string): Observable<Resident> { return this.http.get<Resident>(`${base}/residents/${id}`); }
  create(payload: any): Observable<Resident> { return this.http.post<Resident>(`${base}/residents`, payload); }
  update(id: string, payload: any): Observable<unknown> { return this.http.put(`${base}/residents/${id}`, payload); }
  toggleActive(id: string): Observable<unknown> { return this.http.patch(`${base}/residents/${id}/toggle-active`, {}); }
  delete(id: string): Observable<unknown> { return this.http.delete(`${base}/residents/${id}`); }

  getFamilyMembers(residentId: string): Observable<FamilyMember[]> { return this.http.get<FamilyMember[]>(`${base}/residents/${residentId}/family-members`); }
  addFamilyMember(residentId: string, payload: any): Observable<FamilyMember> { return this.http.post<FamilyMember>(`${base}/residents/${residentId}/family-members`, payload); }
  deleteFamilyMember(id: string): Observable<unknown> { return this.http.delete(`${base}/residents/family-members/${id}`); }

  getVehicles(residentId: string): Observable<Vehicle[]> { return this.http.get<Vehicle[]>(`${base}/residents/${residentId}/vehicles`); }
  addVehicle(residentId: string, payload: any): Observable<Vehicle> { return this.http.post<Vehicle>(`${base}/residents/${residentId}/vehicles`, payload); }
  deleteVehicle(id: string): Observable<unknown> { return this.http.delete(`${base}/residents/vehicles/${id}`); }

  getEmergencyContacts(residentId: string): Observable<EmergencyContact[]> { return this.http.get<EmergencyContact[]>(`${base}/residents/${residentId}/emergency-contacts`); }
  addEmergencyContact(residentId: string, payload: any): Observable<EmergencyContact> { return this.http.post<EmergencyContact>(`${base}/residents/${residentId}/emergency-contacts`, payload); }
  deleteEmergencyContact(id: string): Observable<unknown> { return this.http.delete(`${base}/residents/emergency-contacts/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class ComplaintsService {
  constructor(private http: HttpClient) {}
  getAll(params?: { status?: string; categoryId?: string }): Observable<Complaint[]> {
    return this.http.get<Complaint[]>(`${base}/complaints`, { params: cleanParams(params) });
  }
  getById(id: string): Observable<Complaint> { return this.http.get<Complaint>(`${base}/complaints/${id}`); }
  getRemarks(id: string): Observable<ComplaintRemark[]> { return this.http.get<ComplaintRemark[]>(`${base}/complaints/${id}/remarks`); }
  create(payload: any): Observable<Complaint> { return this.http.post<Complaint>(`${base}/complaints`, payload); }
  assign(id: string, assignedToUserId: string): Observable<unknown> { return this.http.post(`${base}/complaints/${id}/assign`, { assignedToUserId }); }
  updateStatus(id: string, status: string, remark?: string): Observable<unknown> { return this.http.post(`${base}/complaints/${id}/status`, { status, remark }); }
}

@Injectable({ providedIn: 'root' })
export class VisitorsService {
  constructor(private http: HttpClient) {}
  getAll(params?: any): Observable<Visitor[]> { return this.http.get<Visitor[]>(`${base}/visitors`, { params: cleanParams(params) }); }
  getToday(): Observable<Visitor[]> { return this.http.get<Visitor[]>(`${base}/visitors/today`); }
  create(payload: any): Observable<Visitor> { return this.http.post<Visitor>(`${base}/visitors`, payload); }
  respond(id: string, status: 'Approved' | 'Rejected'): Observable<unknown> { return this.http.post(`${base}/visitors/${id}/respond`, { status }); }
  markEntered(id: string): Observable<unknown> { return this.http.post(`${base}/visitors/${id}/entry`, {}); }
  markExited(id: string): Observable<unknown> { return this.http.post(`${base}/visitors/${id}/exit`, {}); }
}

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  constructor(private http: HttpClient) {}
  getAll(params?: any): Observable<MaintenanceInvoice[]> { return this.http.get<MaintenanceInvoice[]>(`${base}/maintenance`, { params: cleanParams(params) }); }
  getMine(): Observable<MaintenanceInvoice[]> { return this.http.get<MaintenanceInvoice[]>(`${base}/maintenance/my`); }
  generate(payload: any): Observable<unknown> { return this.http.post(`${base}/maintenance/generate`, payload); }
  recordPayment(id: string, payload: any): Observable<Payment> { return this.http.post<Payment>(`${base}/maintenance/${id}/payments`, payload); }
  getPayments(id: string): Observable<Payment[]> { return this.http.get<Payment[]>(`${base}/maintenance/${id}/payments`); }
  getSummary(params?: any): Observable<any> { return this.http.get(`${base}/maintenance/summary`, { params: cleanParams(params) }); }
  exportExcel(): Observable<Blob> { return this.http.get(`${base}/maintenance/export/excel`, { responseType: 'blob' }); }
  exportPdf(): Observable<Blob> { return this.http.get(`${base}/maintenance/export/pdf`, { responseType: 'blob' }); }

  createOnlineOrder(invoiceId: string): Observable<PaymentOrderResponse> {
    return this.http.post<PaymentOrderResponse>(`${base}/maintenance/${invoiceId}/payments/online/create-order`, {});
  }
  verifyOnlinePayment(payload: { providerOrderId: string; providerPaymentId: string; signature?: string | null }): Observable<Payment> {
    return this.http.post<Payment>(`${base}/maintenance/payments/online/verify`, payload);
  }

  getMyPaymentInvoices(): Observable<Invoice[]> { return this.http.get<Invoice[]>(`${base}/maintenance/invoices/mine`); }
  getInvoiceByPayment(paymentId: string): Observable<Invoice> { return this.http.get<Invoice>(`${base}/maintenance/invoices/by-payment/${paymentId}`); }
  getInvoicePdfBlob(invoiceId: string, mode: 'view' | 'download'): Observable<Blob> {
    return this.http.get(`${base}/maintenance/invoices/${invoiceId}/pdf`, { params: { mode }, responseType: 'blob' });
  }
}

export interface PaymentOrderResponse {
  internalOrderId: string;
  providerOrderId: string;
  provider: string;
  checkoutKeyId: string | null;
  amount: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentQrCodesService {
  constructor(private http: HttpClient) {}
  getAll(societyId?: string): Observable<PaymentQrAssignment[]> {
    return this.http.get<PaymentQrAssignment[]>(`${base}/payment-qr-codes`, { params: cleanParams({ societyId }) });
  }
  getAssignableUsers(societyId: string): Observable<AssignableUser[]> {
    return this.http.get<AssignableUser[]>(`${base}/payment-qr-codes/assignable-users`, { params: { societyId } });
  }
  create(payload: { societyId: string; blockId?: string; assignedToUserId: string; qrImageUrl: string; payeeName?: string }): Observable<PaymentQrAssignment> {
    return this.http.post<PaymentQrAssignment>(`${base}/payment-qr-codes`, payload);
  }
  update(id: string, payload: { blockId?: string; assignedToUserId: string; qrImageUrl: string; payeeName?: string }): Observable<PaymentQrAssignment> {
    return this.http.put<PaymentQrAssignment>(`${base}/payment-qr-codes/${id}`, payload);
  }
  toggleActive(id: string): Observable<unknown> { return this.http.patch(`${base}/payment-qr-codes/${id}/toggle-active`, {}); }
  delete(id: string): Observable<unknown> { return this.http.delete(`${base}/payment-qr-codes/${id}`); }
  getMine(): Observable<MyPaymentQr> { return this.http.get<MyPaymentQr>(`${base}/payment-qr-codes/mine`); }
}

@Injectable({ providedIn: 'root' })
export class PropertyService {
  constructor(private http: HttpClient) {}
  getAll(params?: any): Observable<PropertyListing[]> { return this.http.get<PropertyListing[]>(`${base}/property-listings`, { params: cleanParams(params) }); }
  getMine(): Observable<PropertyListing[]> { return this.http.get<PropertyListing[]>(`${base}/property-listings/my`); }
  getById(id: string): Observable<PropertyListing> { return this.http.get<PropertyListing>(`${base}/property-listings/${id}`); }
  create(payload: any): Observable<PropertyListing> { return this.http.post<PropertyListing>(`${base}/property-listings`, payload); }
  approve(id: string, approve: boolean, rejectionReason?: string): Observable<unknown> {
    return this.http.post(`${base}/property-listings/${id}/approve`, { approve, rejectionReason });
  }
  delete(id: string): Observable<unknown> { return this.http.delete(`${base}/property-listings/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class NoticesService {
  constructor(private http: HttpClient) {}
  getAll(type?: string): Observable<Notice[]> { return this.http.get<Notice[]>(`${base}/notices`, { params: type ? { type } : {} }); }
  create(payload: any): Observable<Notice> { return this.http.post<Notice>(`${base}/notices`, payload); }
  delete(id: string): Observable<unknown> { return this.http.delete(`${base}/notices/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class SecurityGuardsService {
  constructor(private http: HttpClient) {}
  getAll(): Observable<any[]> { return this.http.get<any[]>(`${base}/security-guards`); }
  create(payload: any): Observable<any> { return this.http.post(`${base}/security-guards`, payload); }
  toggleActive(id: string): Observable<unknown> { return this.http.patch(`${base}/security-guards/${id}/toggle-active`, {}); }
  delete(id: string): Observable<unknown> { return this.http.delete(`${base}/security-guards/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private http: HttpClient) {}
  getAll(params?: any): Observable<any[]> { return this.http.get<any[]>(`${base}/users`, { params: cleanParams(params) }); }
  createSocietyAdmin(payload: any): Observable<any> { return this.http.post(`${base}/users/society-admin`, payload); }
  createSuperAdmin(payload: { fullName: string; email: string; phoneNumber: string; password: string; currentAdminPassword: string }): Observable<any> {
    return this.http.post(`${base}/users/super-admin`, payload);
  }
  toggleActive(id: string): Observable<unknown> { return this.http.patch(`${base}/users/${id}/toggle-active`, {}); }
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  constructor(private http: HttpClient) {}
  download(report: string, format: 'excel' | 'pdf'): Observable<Blob> {
    return this.http.get(`${base}/reports/${report}`, { params: { format }, responseType: 'blob' });
  }
}

export function triggerBlobDownload(blob: Blob, filename: string): void {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  window.URL.revokeObjectURL(url);
}

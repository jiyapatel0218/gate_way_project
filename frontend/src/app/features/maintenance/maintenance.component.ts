import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { AuthService } from '../../core/services/auth.service';
import { MaintenanceService, MastersService, triggerBlobDownload } from '../../core/services/api.services';
import { MaintenanceInvoice } from '../../core/models/models';
import { OnlinePaymentDialogComponent } from './online-payment-dialog.component';

function openBlobInNewTab(blob: Blob): void {
  const url = window.URL.createObjectURL(blob);
  window.open(url, '_blank');
  setTimeout(() => window.URL.revokeObjectURL(url), 60000);
}

@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule, MatDialogModule, MatTooltipModule, MatDatepickerModule, MatNativeDateModule],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.scss'
})
export class MaintenanceComponent implements OnInit {
  invoices = signal<MaintenanceInvoice[]>([]);
  maintenanceTypes = signal<any[]>([]);
  showGenerateForm = signal(false);
  isAdmin = () => this.authService.role() === 'SuperAdmin' || this.authService.role() === 'SocietyAdmin';
  displayedColumns = ['flat', 'type', 'period', 'amount', 'paid', 'status', 'actions', 'invoice'];

  generateForm: { maintenanceTypeId: string; month: string; year: number; dueDate: Date | null } =
    { maintenanceTypeId: '', month: '', year: new Date().getFullYear(), dueDate: null };
  paymentDrafts: Record<string, { amount: number; mode: string; reference: string }> = {};

  months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  paymentModes = ['Cash', 'Cheque', 'BankTransfer', 'UPI', 'Online'];

  constructor(
    private maintenanceService: MaintenanceService,
    private mastersService: MastersService,
    private authService: AuthService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.load();
    if (this.isAdmin()) {
      this.mastersService.getMaintenanceTypes().subscribe((types) => this.maintenanceTypes.set(types));
    }
  }

  load(): void {
    const obs = this.isAdmin() ? this.maintenanceService.getAll() : this.maintenanceService.getMine();
    obs.subscribe((data) => {
      this.invoices.set(data);
      data.forEach((inv) => {
        if (!this.paymentDrafts[inv.id]) {
          this.paymentDrafts[inv.id] = { amount: inv.amount - inv.paidAmount, mode: 'Cash', reference: '' };
        }
      });
    });
  }

  generate(): void {
    if (!this.generateForm.maintenanceTypeId || !this.generateForm.month || !this.generateForm.dueDate) return;
    this.maintenanceService.generate(this.generateForm).subscribe(() => {
      this.showGenerateForm.set(false);
      this.load();
    });
  }

  pay(invoice: MaintenanceInvoice): void {
    const draft = this.paymentDrafts[invoice.id];
    if (!draft.amount) return;
    this.maintenanceService.recordPayment(invoice.id, { amount: draft.amount, mode: draft.mode, transactionReference: draft.reference }).subscribe(() => this.load());
  }

  payOnline(invoice: MaintenanceInvoice): void {
    const dialogRef = this.dialog.open(OnlinePaymentDialogComponent, {
      width: '420px',
      disableClose: true,
      data: { invoiceId: invoice.id, label: `${invoice.month} ${invoice.year} maintenance` }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) this.load();
    });
  }

  private getLatestInvoiceId(maintenanceInvoiceId: string, onFound: (invoiceId: string) => void): void {
    this.maintenanceService.getPayments(maintenanceInvoiceId).subscribe((payments) => {
      const withInvoice = payments.find((p) => !!p.invoiceId);
      if (!withInvoice?.invoiceId) {
        alert('No invoice has been generated for this record yet.');
        return;
      }
      onFound(withInvoice.invoiceId);
    });
  }

  viewInvoice(invoice: MaintenanceInvoice): void {
    this.getLatestInvoiceId(invoice.id, (invoiceId) => {
      this.maintenanceService.getInvoicePdfBlob(invoiceId, 'view').subscribe((blob) => openBlobInNewTab(blob));
    });
  }

  downloadInvoice(invoice: MaintenanceInvoice): void {
    this.getLatestInvoiceId(invoice.id, (invoiceId) => {
      this.maintenanceService.getInvoicePdfBlob(invoiceId, 'download').subscribe((blob) => triggerBlobDownload(blob, `${invoiceId}.pdf`));
    });
  }

  exportExcel(): void {
    this.maintenanceService.exportExcel().subscribe((blob) => triggerBlobDownload(blob, 'maintenance-report.xlsx'));
  }

  exportPdf(): void {
    this.maintenanceService.exportPdf().subscribe((blob) => triggerBlobDownload(blob, 'maintenance-report.pdf'));
  }
}

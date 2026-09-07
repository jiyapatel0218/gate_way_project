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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PaymentQrCodesService, SocietyStructureService, UploadsService, resolveMediaUrl } from '../../core/services/api.services';
import { AuthService } from '../../core/services/auth.service';
import { AssignableUser, Block, PaymentQrAssignment, Society } from '../../core/models/models';

interface AssignmentForm {
  blockId: string;
  assignedToUserId: string;
  qrImageUrl: string;
  payeeName: string;
}

const EMPTY_FORM: AssignmentForm = { blockId: '', assignedToUserId: '', qrImageUrl: '', payeeName: '' };

@Component({
  selector: 'app-payment-qr-codes',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule, MatProgressSpinnerModule
  ],
  templateUrl: './payment-qr-codes.component.html',
  styleUrl: './payment-qr-codes.component.scss'
})
export class PaymentQrCodesComponent implements OnInit {
  isSuperAdmin = () => this.authService.role() === 'SuperAdmin';

  societies = signal<Society[]>([]);
  selectedSocietyId = signal<string>('');
  blocks = signal<Block[]>([]);
  assignableUsers = signal<AssignableUser[]>([]);
  assignments = signal<PaymentQrAssignment[]>([]);

  showForm = signal(false);
  editingId = signal<string | null>(null);
  uploading = signal(false);
  qrPreviewUrl = signal<string | null>(null);
  form: AssignmentForm = { ...EMPTY_FORM };

  displayedColumns = ['scope', 'assignedTo', 'qr', 'payeeName', 'status', 'actions'];
  resolveMediaUrl = resolveMediaUrl;

  constructor(
    private qrService: PaymentQrCodesService,
    private structureService: SocietyStructureService,
    private uploadsService: UploadsService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    if (this.isSuperAdmin()) {
      this.structureService.getSocieties().subscribe((data) => this.societies.set(data));
    } else {
      this.structureService.getMySociety().subscribe((society) => {
        this.societies.set([society]);
        this.selectedSocietyId.set(society.id);
        this.onSocietyChange(society.id);
      });
    }
  }

  onSocietyChange(societyId: string): void {
    this.selectedSocietyId.set(societyId);
    this.resetForm();
    if (!societyId) {
      this.blocks.set([]);
      this.assignableUsers.set([]);
      this.assignments.set([]);
      return;
    }
    this.structureService.getBlocks(societyId).subscribe((data) => this.blocks.set(data));
    this.qrService.getAssignableUsers(societyId).subscribe((data) => this.assignableUsers.set(data));
    this.loadAssignments();
  }

  private loadAssignments(): void {
    const societyId = this.selectedSocietyId();
    if (!societyId) return;
    this.qrService.getAll(societyId).subscribe((data) => this.assignments.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
    if (!this.showForm()) this.resetForm();
  }

  onQrFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.qrPreviewUrl.set(URL.createObjectURL(file));
    this.uploading.set(true);
    this.uploadsService.uploadPhoto(file, 'qr-codes').subscribe({
      next: (res) => {
        this.form.qrImageUrl = res.url;
        this.uploading.set(false);
      },
      error: () => {
        alert('Could not upload the QR image. Please try again.');
        this.uploading.set(false);
      }
    });
  }

  startEdit(a: PaymentQrAssignment): void {
    this.editingId.set(a.id);
    this.form = { blockId: a.blockId ?? '', assignedToUserId: a.assignedToUserId, qrImageUrl: a.qrImageUrl, payeeName: a.payeeName ?? '' };
    this.qrPreviewUrl.set(resolveMediaUrl(a.qrImageUrl));
    this.showForm.set(true);
  }

  resetForm(): void {
    this.editingId.set(null);
    this.form = { ...EMPTY_FORM };
    this.qrPreviewUrl.set(null);
  }

  save(): void {
    if (!this.form.assignedToUserId || !this.form.qrImageUrl) return;
    const societyId = this.selectedSocietyId();
    const payload = {
      blockId: this.form.blockId || undefined,
      assignedToUserId: this.form.assignedToUserId,
      qrImageUrl: this.form.qrImageUrl,
      payeeName: this.form.payeeName || undefined
    };

    const editingId = this.editingId();
    const req = editingId
      ? this.qrService.update(editingId, payload)
      : this.qrService.create({ societyId, ...payload });

    req.subscribe({
      next: () => {
        this.showForm.set(false);
        this.resetForm();
        this.loadAssignments();
      },
      error: (err) => alert(err?.error?.message || 'Could not save the QR assignment.')
    });
  }

  toggleActive(a: PaymentQrAssignment): void {
    this.qrService.toggleActive(a.id).subscribe({
      next: () => this.loadAssignments(),
      error: (err) => alert(err?.error?.message || 'Could not update the QR assignment.')
    });
  }

  remove(a: PaymentQrAssignment): void {
    if (!confirm(`Remove the QR assigned to ${a.assignedToUserName}?`)) return;
    this.qrService.delete(a.id).subscribe(() => this.loadAssignments());
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ResidentsService, SocietyStructureService } from '../../core/services/api.services';
import { AuthService } from '../../core/services/auth.service';
import { Resident, Flat, Society } from '../../core/models/models';

const PHONE_PATTERN = /^[0-9+()\-\s]{7,20}$/;
const PASSWORD_PATTERN = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

@Component({
  selector: 'app-residents',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule, MatChipsModule, MatProgressSpinnerModule
  ],
  templateUrl: './residents.component.html',
  styleUrl: './residents.component.scss'
})
export class ResidentsComponent implements OnInit {
  residents = signal<Resident[]>([]);
  flats = signal<Flat[]>([]);
  societies = signal<Society[]>([]);
  showForm = signal(false);
  editingResidentId = signal<string | null>(null);
  loadingSociety = signal(false);
  saving = signal(false);
  searchTerm = '';
  displayedColumns = ['name', 'society', 'flat', 'contact', 'ownership', 'status', 'actions'];

  private fb = inject(FormBuilder);
  private residentsService = inject(ResidentsService);
  private structureService = inject(SocietyStructureService);
  private authService = inject(AuthService);

  isSuperAdmin = () => this.authService.role() === 'SuperAdmin';
  isEditMode = () => this.editingResidentId() !== null;

  form: FormGroup = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
    societyId: ['', Validators.required],
    flatId: ['', Validators.required],
    isOwner: [true]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.residentsService.getAll({ search: this.searchTerm || undefined }).subscribe((data) => this.residents.set(data));
  }

  get societyControl() {
    return this.form.get('societyId')!;
  }

  openCreateForm(): void {
    this.editingResidentId.set(null);
    this.form.reset({ isOwner: true });
    this.form.get('email')?.enable();
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]);
    this.form.get('password')?.updateValueAndValidity();
    this.flats.set([]);
    this.showForm.set(true);
    this.initSocietyControl();
  }

  openEditForm(resident: Resident): void {
    this.editingResidentId.set(resident.id);
    this.form.reset({
      fullName: resident.fullName,
      email: resident.email,
      phoneNumber: resident.phoneNumber ?? '',
      isOwner: resident.isOwner
    });
    // Email can't be changed after account creation; password isn't needed for edits.
    this.form.get('email')?.disable();
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    this.flats.set([]);
    this.showForm.set(true);
    this.initSocietyControl(resident.societyId, resident.flatId);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingResidentId.set(null);
  }

  /** Loads the society list for the current role and wires up the societyId control's enabled state. */
  private initSocietyControl(preselectSocietyId?: string, preselectFlatId?: string): void {
    this.loadingSociety.set(true);

    if (this.isSuperAdmin()) {
      this.societyControl.enable();
      this.structureService.getSocieties().subscribe((data) => {
        this.societies.set(data);
        this.loadingSociety.set(false);
        const societyId = preselectSocietyId ?? '';
        this.form.patchValue({ societyId });
        if (societyId) this.onSocietyChange(preselectFlatId);
      });
    } else {
      // Society Admins can only ever manage their own society: auto-select it and lock the
      // control, but keep it enabled in the FormGroup (via .disable() + getRawValue() on submit)
      // so the correct societyId is still sent to the API.
      this.structureService.getMySociety().subscribe((society) => {
        this.societies.set([society]);
        this.loadingSociety.set(false);
        this.form.patchValue({ societyId: society.id });
        this.societyControl.disable();
        this.onSocietyChange(preselectFlatId);
      });
    }
  }

  onSocietyChange(preselectFlatId?: string): void {
    const societyId = this.societyControl.value;
    this.flats.set([]);
    if (!preselectFlatId) this.form.patchValue({ flatId: '' });
    if (!societyId) return;

    this.structureService.getFlats({ societyId }).subscribe((flats) => {
      this.flats.set(flats);
      if (preselectFlatId) this.form.patchValue({ flatId: preselectFlatId });
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // getRawValue() includes the societyId even when the control is disabled (Society Admin case).
    const raw = this.form.getRawValue();
    this.saving.set(true);

    if (this.isEditMode()) {
      const payload = {
        fullName: raw.fullName,
        alternatePhone: raw.phoneNumber,
        flatId: raw.flatId,
        isOwner: raw.isOwner,
        isActive: true
      };
      this.residentsService.update(this.editingResidentId()!, payload).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => this.saving.set(false)
      });
    } else {
      const payload = {
        fullName: raw.fullName,
        email: raw.email,
        phoneNumber: raw.phoneNumber,
        password: raw.password,
        flatId: raw.flatId,
        isOwner: raw.isOwner
      };
      this.residentsService.create(payload).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => this.saving.set(false)
      });
    }
  }

  toggleActive(resident: Resident): void {
    this.residentsService.toggleActive(resident.id).subscribe(() => this.load());
  }

  deleteResident(resident: Resident): void {
    if (!confirm(`Remove ${resident.fullName}?`)) return;
    this.residentsService.delete(resident.id).subscribe(() => this.load());
  }
}

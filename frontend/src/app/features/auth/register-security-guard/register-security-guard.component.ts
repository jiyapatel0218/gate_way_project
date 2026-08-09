import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { PublicService, UploadsService } from '../../../core/services/api.services';
import { getHomeRoute } from '../../../core/utils/role-home';

@Component({
  selector: 'app-register-security-guard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './register-security-guard.component.html',
  styleUrl: './register-security-guard.component.scss'
})
export class RegisterSecurityGuardComponent implements OnInit {
  loading = signal(false);
  errorMessage = signal('');

  photoPreviewUrl = signal<string | null>(null);
  uploadedPhotoUrl = signal<string | null>(null);
  uploadingPhoto = signal(false);

  hidePassword = true;
  hideConfirmPassword = true;
  confirmPassword = '';

  societies = signal<{ id: string; name: string }[]>([]);
  shiftOptions = ['Morning (6 AM - 2 PM)', 'Afternoon (2 PM - 10 PM)', 'Night (10 PM - 6 AM)'];

  form = { fullName: '', email: '', phoneNumber: '', password: '', societyId: '', shiftTiming: '', guardCode: '' };

  constructor(private authService: AuthService, private publicService: PublicService, private uploadsService: UploadsService, private router: Router) {}

  ngOnInit(): void {
    this.publicService.getSocieties().subscribe((data) => this.societies.set(data));
  }

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.photoPreviewUrl.set(URL.createObjectURL(file));
    this.uploadingPhoto.set(true);
    this.uploadsService.uploadPhoto(file).subscribe({
      next: (res) => { this.uploadedPhotoUrl.set(res.url); this.uploadingPhoto.set(false); },
      error: () => { this.uploadingPhoto.set(false); this.errorMessage.set('Photo upload failed. You can still continue without a photo.'); }
    });
  }

  submit(): void {
    if (!this.form.fullName || !this.form.email || !this.form.phoneNumber || !this.form.password || !this.form.societyId || !this.form.shiftTiming) return;
    if (this.form.password !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.registerSecurityGuard({
      ...this.form,
      guardCode: this.form.guardCode || undefined,
      profileImageUrl: this.uploadedPhotoUrl() ?? undefined
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate([getHomeRoute(this.authService.role())]);
      },
      error: (err) => {
        this.loading.set(false);
        const errors = err?.error?.errors;
        this.errorMessage.set(Array.isArray(errors) ? errors.join(' ') : (err?.error?.message ?? 'Registration failed.'));
      }
    });
  }
}

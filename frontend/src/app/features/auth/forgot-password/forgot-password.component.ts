import { Component, ElementRef, OnDestroy, QueryList, ViewChildren, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

type Step = 'identify' | 'otp' | 'reset' | 'success';

function passwordScore(pw: string): number {
  let score = 0;
  if (pw.length >= 8) score++;
  if (/[A-Z]/.test(pw)) score++;
  if (/[a-z]/.test(pw)) score++;
  if (/\d/.test(pw)) score++;
  if (/[^A-Za-z0-9]/.test(pw)) score++;
  return score;
}

const STRENGTH_LABELS = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong', 'Strong'];

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatRadioModule, MatProgressSpinnerModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);

  @ViewChildren('otpBox') otpBoxes!: QueryList<ElementRef<HTMLInputElement>>;

  step = signal<Step>('identify');
  loading = signal(false);
  errorMessage = signal('');
  backLink = ['/welcome'];

  identifier = '';
  deliveryMethod: 'Email' | 'Sms' = 'Email';
  devOtp = signal<string | null>(null);

  otpDigits = signal<string[]>(['', '', '', '', '', '']);
  expirySeconds = signal(300);
  resendCooldown = signal(0);
  resendCount = signal(0);
  private timer: ReturnType<typeof setInterval> | null = null;

  resetToken = '';
  resetEmail = '';
  newPassword = '';
  confirmPassword = '';
  hideNewPassword = true;
  hideConfirmPassword = true;

  // Plain methods, not computed() signals: newPassword is a plain ngModel-bound field, not a
  // signal, so a computed() here would never track it as a dependency and would freeze after its
  // first read (this app is zoneless, so ngModel input events still drive change detection and
  // re-invoke these on every check — computed() has no equivalent mechanism for non-signal reads).
  getStrength(): number {
    return passwordScore(this.newPassword);
  }

  getStrengthLabel(): string {
    return STRENGTH_LABELS[this.getStrength()];
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  private startTimers(): void {
    this.expirySeconds.set(300);
    this.resendCooldown.set(30);
    if (this.timer) clearInterval(this.timer);
    this.timer = setInterval(() => {
      this.expirySeconds.update((v) => Math.max(0, v - 1));
      this.resendCooldown.update((v) => Math.max(0, v - 1));
    }, 1000);
  }

  submitIdentify(): void {
    if (!this.identifier) return;
    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.requestResetOtp(this.identifier, this.deliveryMethod).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.devOtp.set(res.devOtp ?? null);
        this.otpDigits.set(['', '', '', '', '', '']);
        this.resendCount.set(0);
        this.startTimers();
        this.step.set('otp');
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Something went wrong. Please try again.');
      }
    });
  }

  onOtpInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const digit = input.value.replace(/\D/g, '').slice(-1);
    const digits = [...this.otpDigits()];
    digits[index] = digit;
    this.otpDigits.set(digits);

    if (digit && index < 5) {
      this.otpBoxes.get(index + 1)?.nativeElement.focus();
    }
  }

  onOtpKeydown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.otpDigits()[index] && index > 0) {
      this.otpBoxes.get(index - 1)?.nativeElement.focus();
    }
  }

  onOtpPaste(event: ClipboardEvent): void {
    const pasted = event.clipboardData?.getData('text').replace(/\D/g, '').slice(0, 6) ?? '';
    if (!pasted) return;
    event.preventDefault();
    const digits = pasted.split('');
    while (digits.length < 6) digits.push('');
    this.otpDigits.set(digits);
    const lastIndex = Math.min(pasted.length, 6) - 1;
    if (lastIndex >= 0) this.otpBoxes.get(lastIndex)?.nativeElement.focus();
  }

  get otpValue(): string {
    return this.otpDigits().join('');
  }

  resendOtp(): void {
    if (this.resendCooldown() > 0 || this.resendCount() >= 3) return;
    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.resendResetOtp(this.identifier, this.deliveryMethod).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.devOtp.set(res.devOtp ?? null);
        this.otpDigits.set(['', '', '', '', '', '']);
        this.resendCount.update((v) => v + 1);
        this.startTimers();
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Could not resend code.');
      }
    });
  }

  verifyOtp(): void {
    if (this.otpValue.length !== 6) return;
    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.verifyResetOtp(this.identifier, this.otpValue).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.resetToken = res.resetToken;
        this.resetEmail = res.email;
        this.step.set('reset');
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Invalid OTP. Please try again.');
      }
    });
  }

  formatTime(totalSeconds: number): string {
    const m = Math.floor(totalSeconds / 60);
    const s = totalSeconds % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  submitReset(): void {
    if (!this.newPassword || this.newPassword !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }
    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.resetPassword(this.resetEmail, this.resetToken, this.newPassword).subscribe({
      next: () => {
        this.loading.set(false);
        this.step.set('success');
        setTimeout(() => this.router.navigate(['/welcome']), 4000);
      },
      error: (err) => {
        this.loading.set(false);
        const errors = err?.error?.errors;
        this.errorMessage.set(Array.isArray(errors) ? errors.join(' ') : (err?.error?.message ?? 'Failed to reset password.'));
      }
    });
  }
}

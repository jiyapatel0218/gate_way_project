import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUser, LoginResponse, UserRole } from '../models/models';

const STORAGE_KEY = 'sgk_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSignal = signal<CurrentUser | null>(this.loadFromStorage());

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => !!this.currentUserSignal());
  readonly role = computed<UserRole | null>(() => this.currentUserSignal()?.role ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  private loadFromStorage(): CurrentUser | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw);
      return parsed.user ?? null;
    } catch {
      return null;
    }
  }

  get accessToken(): string | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw).accessToken ?? null;
    } catch {
      return null;
    }
  }

  private storeSession(res: LoginResponse): void {
    const user: CurrentUser = {
      userId: res.userId,
      fullName: res.fullName,
      email: res.email,
      role: res.role,
      societyId: res.societyId
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ accessToken: res.accessToken, refreshToken: res.refreshToken, user }));
    this.currentUserSignal.set(user);
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
      tap((res) => this.storeSession(res))
    );
  }

  adminLogin(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/admin-login`, { email, password }).pipe(
      tap((res) => this.storeSession(res))
    );
  }

  register(payload: { fullName: string; email: string; phoneNumber: string; password: string; flatId: string; isOwner: boolean; profileImageUrl?: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/register`, payload).pipe(
      tap((res) => this.storeSession(res))
    );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.currentUserSignal.set(null);
    this.router.navigate(['/welcome']);
  }

  changePassword(currentPassword: string, newPassword: string): Observable<unknown> {
    return this.http.post(`${environment.apiUrl}/auth/change-password`, { currentPassword, newPassword });
  }

  forgotPassword(email: string): Observable<{ message: string; devToken?: string }> {
    return this.http.post<{ message: string; devToken?: string }>(`${environment.apiUrl}/auth/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<unknown> {
    return this.http.post(`${environment.apiUrl}/auth/reset-password`, { email, token, newPassword });
  }

  requestResetOtp(identifier: string, deliveryMethod: 'Email' | 'Sms'): Observable<{ message: string; devOtp?: string }> {
    return this.http.post<{ message: string; devOtp?: string }>(`${environment.apiUrl}/auth/forgot-password/request`, { identifier, deliveryMethod });
  }

  resendResetOtp(identifier: string, deliveryMethod: 'Email' | 'Sms'): Observable<{ message: string; devOtp?: string }> {
    return this.http.post<{ message: string; devOtp?: string }>(`${environment.apiUrl}/auth/forgot-password/resend`, { identifier, deliveryMethod });
  }

  verifyResetOtp(identifier: string, otp: string): Observable<{ email: string; resetToken: string }> {
    return this.http.post<{ email: string; resetToken: string }>(`${environment.apiUrl}/auth/forgot-password/verify-otp`, { identifier, otp });
  }
}

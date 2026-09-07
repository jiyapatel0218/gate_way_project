import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { getHomeRoute } from '../../../core/utils/role-home';
import { UserRole } from '../../../core/models/models';

interface RolePresentation {
  emoji: string;
  title: string;
  registerRoute: string | null;
}

// Society Secretary and Security Guard accounts are privileged and provisioned only by an
// existing admin from within the app — never self-service, so these two have no registerRoute.
const ROLE_PRESENTATION: Record<string, RolePresentation> = {
  SocietyAdmin: { emoji: '🏢', title: 'Society Secretary Portal', registerRoute: null },
  Resident: { emoji: '🏠', title: 'Resident Portal', registerRoute: '/register/resident' },
  SecurityGuard: { emoji: '🛡️', title: 'Security Guard Portal', registerRoute: null }
};
const DEFAULT_PRESENTATION: RolePresentation = { emoji: '🏘️', title: 'Welcome back', registerRoute: '/register/resident' };

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  presentation = signal<RolePresentation>(DEFAULT_PRESENTATION);

  email = '';
  password = '';
  hidePassword = true;
  loading = signal(false);
  errorMessage = signal('');

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const roleParam = this.route.snapshot.queryParamMap.get('role') as UserRole | null;
    this.presentation.set((roleParam && ROLE_PRESENTATION[roleParam]) || DEFAULT_PRESENTATION);
  }

  submit(): void {
    if (!this.email || !this.password) return;
    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate([getHomeRoute(this.authService.role())]);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Login failed. Please check your credentials.');
      }
    });
  }
}

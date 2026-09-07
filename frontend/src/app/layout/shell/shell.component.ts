import { Component, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserRole } from '../../core/models/models';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  roles: UserRole[];
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', icon: 'dashboard', route: '/dashboard', roles: ['SuperAdmin', 'SocietyAdmin'] },
  { label: 'My Profile', icon: 'person', route: '/profile', roles: ['Resident'] },
  { label: 'Societies', icon: 'domain', route: '/societies', roles: ['SuperAdmin'] },
  { label: 'Residents', icon: 'groups', route: '/residents', roles: ['SuperAdmin', 'SocietyAdmin'] },
  { label: 'Security Guards', icon: 'security', route: '/security-guards', roles: ['SuperAdmin', 'SocietyAdmin'] },
  { label: 'Society Admin', icon: 'manage_accounts', route: '/society-admins', roles: ['SuperAdmin'] },
  { label: 'Super Admins', icon: 'admin_panel_settings', route: '/super-admins', roles: ['SuperAdmin'] },
  { label: 'Complaints', icon: 'report_problem', route: '/complaints', roles: ['SuperAdmin', 'SocietyAdmin', 'Resident'] },
  { label: 'Visitors', icon: 'how_to_reg', route: '/visitors', roles: ['SuperAdmin', 'SocietyAdmin', 'Resident'] },
  { label: 'Gate Entry', icon: 'meeting_room', route: '/gate-entry', roles: ['SecurityGuard'] },
  { label: 'Maintenance', icon: 'payments', route: '/maintenance', roles: ['SuperAdmin', 'SocietyAdmin', 'Resident'] },
  { label: 'Property Listings', icon: 'real_estate_agent', route: '/property', roles: ['SuperAdmin', 'SocietyAdmin', 'Resident'] },
  { label: 'Notices', icon: 'campaign', route: '/notices', roles: ['SuperAdmin', 'SocietyAdmin', 'Resident', 'SecurityGuard'] },
  { label: 'Masters', icon: 'tune', route: '/masters', roles: ['SuperAdmin', 'SocietyAdmin'] },
  { label: 'Payment QR Codes', icon: 'qr_code_2', route: '/payment-qr-codes', roles: ['SuperAdmin', 'SocietyAdmin'] },
  { label: 'Reports', icon: 'summarize', route: '/reports', roles: ['SuperAdmin', 'SocietyAdmin'] },
  { label: 'Audit Logs', icon: 'history', route: '/audit-logs', roles: ['SuperAdmin', 'SocietyAdmin'] }
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, MatSidenavModule, MatToolbarModule, MatIconModule, MatListModule, MatButtonModule, MatMenuModule, MatBadgeModule, MatDividerModule],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  readonly currentUser = this.authService.currentUser;
  readonly navItems = computed(() => {
    const role = this.authService.role();
    return NAV_ITEMS.filter((item) => role && item.roles.includes(role));
  });
  readonly notifications = this.notificationService.notifications;
  readonly unreadCount = this.notificationService.unreadCount;

  ngOnInit(): void {
    this.notificationService.connect();
    this.notificationService.loadRecent();
  }

  logout(): void {
    this.notificationService.disconnect();
    this.authService.logout();
  }

  markAllRead(): void {
    this.notificationService.markAllRead();
  }

  openNotification(id: string): void {
    this.notificationService.markRead(id);
  }
}

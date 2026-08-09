import { Routes } from '@angular/router';
import { authGuard, roleGuard, homeRedirectGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'welcome', loadComponent: () => import('./features/auth/role-select/role-select.component').then((m) => m.RoleSelectComponent) },
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent) },
  { path: 'register/resident', loadComponent: () => import('./features/auth/register-resident/register-resident.component').then((m) => m.RegisterResidentComponent) },
  { path: 'register/society-secretary', loadComponent: () => import('./features/auth/register-society-admin/register-society-admin.component').then((m) => m.RegisterSocietyAdminComponent) },
  { path: 'register/security-guard', loadComponent: () => import('./features/auth/register-security-guard/register-security-guard.component').then((m) => m.RegisterSecurityGuardComponent) },
  { path: 'admin-login', loadComponent: () => import('./features/auth/admin-login/admin-login.component').then((m) => m.AdminLoginComponent) },
  { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password.component').then((m) => m.ResetPasswordComponent) },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent) },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', canActivate: [homeRedirectGuard], loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent) },
      {
        path: 'dashboard',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin'])],
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'profile',
        canActivate: [roleGuard(['Resident'])],
        loadComponent: () => import('./features/profile/profile.component').then((m) => m.ProfileComponent)
      },
      {
        path: 'societies',
        canActivate: [roleGuard(['SuperAdmin'])],
        loadComponent: () => import('./features/societies/societies.component').then((m) => m.SocietiesComponent)
      },
      {
        path: 'residents',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin'])],
        loadComponent: () => import('./features/residents/residents.component').then((m) => m.ResidentsComponent)
      },
      {
        path: 'security-guards',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin'])],
        loadComponent: () => import('./features/security-guards/security-guards.component').then((m) => m.SecurityGuardsComponent)
      },
      {
        path: 'society-admins',
        canActivate: [roleGuard(['SuperAdmin'])],
        loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent)
      },
      {
        path: 'super-admins',
        canActivate: [roleGuard(['SuperAdmin'])],
        loadComponent: () => import('./features/super-admins/super-admins.component').then((m) => m.SuperAdminsComponent)
      },
      {
        path: 'complaints',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin', 'Resident'])],
        loadComponent: () => import('./features/complaints/complaints.component').then((m) => m.ComplaintsComponent)
      },
      {
        path: 'visitors',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin', 'Resident'])],
        loadComponent: () => import('./features/visitors/visitors.component').then((m) => m.VisitorsComponent)
      },
      {
        path: 'gate-entry',
        canActivate: [roleGuard(['SecurityGuard'])],
        loadComponent: () => import('./features/gate-entry/gate-entry.component').then((m) => m.GateEntryComponent)
      },
      {
        path: 'maintenance',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin', 'Resident'])],
        loadComponent: () => import('./features/maintenance/maintenance.component').then((m) => m.MaintenanceComponent)
      },
      {
        path: 'property',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin', 'Resident'])],
        loadComponent: () => import('./features/property/property.component').then((m) => m.PropertyComponent)
      },
      {
        path: 'notices',
        loadComponent: () => import('./features/notices/notices.component').then((m) => m.NoticesComponent)
      },
      {
        path: 'masters',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin'])],
        loadComponent: () => import('./features/masters/masters.component').then((m) => m.MastersComponent)
      },
      {
        path: 'reports',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin'])],
        loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent)
      },
      {
        path: 'audit-logs',
        canActivate: [roleGuard(['SuperAdmin', 'SocietyAdmin'])],
        loadComponent: () => import('./features/audit-logs/audit-logs.component').then((m) => m.AuditLogsComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];

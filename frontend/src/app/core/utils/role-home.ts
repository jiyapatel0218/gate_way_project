import { UserRole } from '../models/models';

export function getHomeRoute(role: UserRole | null): string {
  switch (role) {
    case 'SuperAdmin':
    case 'SocietyAdmin':
      return '/dashboard';
    case 'Resident':
      return '/profile';
    case 'SecurityGuard':
      return '/gate-entry';
    default:
      return '/login';
  }
}

import { AfterViewInit, Component, ElementRef, QueryList, ViewChildren, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatRippleModule } from '@angular/material/core';
import gsap from 'gsap';
import { UserRole } from '../../../core/models/models';

interface RoleCard {
  role: UserRole;
  emoji: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-role-select',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatRippleModule],
  templateUrl: './role-select.component.html',
  styleUrl: './role-select.component.scss'
})
export class RoleSelectComponent implements AfterViewInit {
  private router = inject(Router);

  @ViewChildren('cardEl') cardEls!: QueryList<ElementRef<HTMLElement>>;

  roles: RoleCard[] = [
    { role: 'SuperAdmin', emoji: '👑', title: 'Super Admin', description: 'Full system control across every society' },
    { role: 'SocietyAdmin', emoji: '🏢', title: 'Society Secretary', description: 'Manage residents, staff, and society operations' },
    { role: 'Resident', emoji: '🏠', title: 'Resident', description: 'Pay maintenance, raise complaints, invite guests' },
    { role: 'SecurityGuard', emoji: '🛡️', title: 'Security Guard', description: 'Log visitors and manage gate entries' }
  ];

  ngAfterViewInit(): void {
    gsap.from(
      this.cardEls.map((c) => c.nativeElement),
      { opacity: 0, y: 40, duration: 0.7, stagger: 0.12, ease: 'power3.out' }
    );
  }

  onCardMouseMove(event: MouseEvent, card: HTMLElement): void {
    const rect = card.getBoundingClientRect();
    const px = (event.clientX - rect.left) / rect.width;
    const py = (event.clientY - rect.top) / rect.height;
    const rotateY = (px - 0.5) * 16;
    const rotateX = (py - 0.5) * -16;
    gsap.to(card, { rotateX, rotateY, scale: 1.04, y: -6, duration: 0.4, ease: 'power2.out', transformPerspective: 800 });
  }

  onCardMouseLeave(card: HTMLElement): void {
    gsap.to(card, { rotateX: 0, rotateY: 0, scale: 1, y: 0, duration: 0.6, ease: 'power3.out' });
  }

  selectRole(role: UserRole): void {
    if (role === 'SuperAdmin') {
      this.router.navigate(['/admin-login']);
    } else {
      this.router.navigate(['/login'], { queryParams: { role } });
    }
  }
}

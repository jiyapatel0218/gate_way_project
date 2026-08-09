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
import { SecurityGuardsService, SocietyStructureService } from '../../core/services/api.services';
import { AuthService } from '../../core/services/auth.service';
import { Society } from '../../core/models/models';

@Component({
  selector: 'app-security-guards',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule],
  templateUrl: './security-guards.component.html',
  styleUrl: './security-guards.component.scss'
})
export class SecurityGuardsComponent implements OnInit {
  guards = signal<any[]>([]);
  societies = signal<Society[]>([]);
  showForm = signal(false);
  displayedColumns = ['name', 'contact', 'shift', 'code', 'status', 'actions'];

  isSuperAdmin = () => this.authService.role() === 'SuperAdmin';

  newGuard = { fullName: '', email: '', phoneNumber: '', password: '', societyId: '', shiftTiming: '', guardCode: '' };

  constructor(
    private guardsService: SecurityGuardsService,
    private structureService: SocietyStructureService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.guardsService.getAll().subscribe((data) => this.guards.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
    if (!this.showForm()) return;

    if (this.isSuperAdmin()) {
      this.structureService.getSocieties().subscribe((data) => this.societies.set(data));
    } else {
      this.structureService.getMySociety().subscribe((society) => {
        this.societies.set([society]);
        this.newGuard.societyId = society.id;
      });
    }
  }

  create(): void {
    if (!this.newGuard.fullName || !this.newGuard.email || !this.newGuard.password) return;
    this.guardsService.create(this.newGuard).subscribe(() => {
      this.showForm.set(false);
      this.newGuard = { fullName: '', email: '', phoneNumber: '', password: '', societyId: '', shiftTiming: '', guardCode: '' };
      this.load();
    });
  }

  toggleActive(guard: any): void {
    this.guardsService.toggleActive(guard.id).subscribe(() => this.load());
  }

  remove(guard: any): void {
    if (!confirm(`Remove ${guard.fullName}?`)) return;
    this.guardsService.delete(guard.id).subscribe(() => this.load());
  }
}

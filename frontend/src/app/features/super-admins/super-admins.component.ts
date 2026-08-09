import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { UsersService } from '../../core/services/api.services';

@Component({
  selector: 'app-super-admins',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSlideToggleModule],
  templateUrl: './super-admins.component.html',
  styleUrl: './super-admins.component.scss'
})
export class SuperAdminsComponent implements OnInit {
  admins = signal<any[]>([]);
  showForm = signal(false);
  saving = signal(false);
  errorMessage = signal('');
  displayedColumns = ['name', 'contact', 'status', 'lastLogin'];

  newAdmin = { fullName: '', email: '', phoneNumber: '', password: '', currentAdminPassword: '' };

  constructor(private usersService: UsersService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.usersService.getAll({ role: 'SuperAdmin' }).subscribe((data) => this.admins.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
    this.errorMessage.set('');
  }

  createSuperAdmin(): void {
    if (!this.newAdmin.fullName || !this.newAdmin.email || !this.newAdmin.password || !this.newAdmin.currentAdminPassword) return;

    this.saving.set(true);
    this.errorMessage.set('');

    this.usersService.createSuperAdmin(this.newAdmin).subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.newAdmin = { fullName: '', email: '', phoneNumber: '', password: '', currentAdminPassword: '' };
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Failed to create Super Admin.');
      }
    });
  }

  toggleActive(admin: any): void {
    this.usersService.toggleActive(admin.id).subscribe(() => this.load());
  }
}

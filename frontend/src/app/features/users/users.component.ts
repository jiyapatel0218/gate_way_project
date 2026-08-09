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
import { MatChipsModule } from '@angular/material/chips';
import { UsersService, SocietyStructureService } from '../../core/services/api.services';
import { Society } from '../../core/models/models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule, MatChipsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  users = signal<any[]>([]);
  societies = signal<Society[]>([]);
  showForm = signal(false);
  displayedColumns = ['name', 'contact', 'society', 'status', 'lastLogin'];

  newAdmin = { fullName: '', email: '', phoneNumber: '', password: '', societyId: '' };

  constructor(private usersService: UsersService, private structureService: SocietyStructureService) {}

  ngOnInit(): void {
    this.load();
    this.structureService.getSocieties().subscribe((data) => this.societies.set(data));
  }

  load(): void {
    this.usersService.getAll({ role: 'SocietyAdmin' }).subscribe((data) => this.users.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
  }

  createSocietyAdmin(): void {
    if (!this.newAdmin.fullName || !this.newAdmin.email || !this.newAdmin.password || !this.newAdmin.societyId) return;
    this.usersService.createSocietyAdmin(this.newAdmin).subscribe(() => {
      this.showForm.set(false);
      this.newAdmin = { fullName: '', email: '', phoneNumber: '', password: '', societyId: '' };
      this.load();
    });
  }

  toggleActive(user: any): void {
    this.usersService.toggleActive(user.id).subscribe(() => this.load());
  }

  getSocietyName(societyId: string): string {
    return this.societies().find((s) => s.id === societyId)?.name ?? '-';
  }
}

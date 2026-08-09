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
import { MatChipsModule } from '@angular/material/chips';
import { AuthService } from '../../core/services/auth.service';
import { ComplaintsService, MastersService } from '../../core/services/api.services';
import { Complaint, ComplaintStatus, MasterItem } from '../../core/models/models';

const STATUS_OPTIONS: ComplaintStatus[] = ['Open', 'Assigned', 'InProgress', 'Completed', 'Closed'];

@Component({
  selector: 'app-complaints',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule],
  templateUrl: './complaints.component.html',
  styleUrl: './complaints.component.scss'
})
export class ComplaintsComponent implements OnInit {
  complaints = signal<Complaint[]>([]);
  categories = signal<MasterItem[]>([]);
  showForm = signal(false);
  statusFilter = '';
  statusOptions = STATUS_OPTIONS;
  isResident = () => this.authService.role() === 'Resident';
  isAdmin = () => this.authService.role() === 'SuperAdmin' || this.authService.role() === 'SocietyAdmin';
  displayedColumns = ['title', 'category', 'resident', 'status', 'created', 'actions'];

  newComplaint = { categoryId: '', title: '', description: '' };
  remarkDrafts: Record<string, string> = {};

  constructor(
    private complaintsService: ComplaintsService,
    private mastersService: MastersService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
    this.mastersService.getComplaintCategories().subscribe((data) => this.categories.set(data));
  }

  load(): void {
    this.complaintsService.getAll({ status: this.statusFilter || undefined }).subscribe((data) => this.complaints.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
  }

  submitComplaint(): void {
    if (!this.newComplaint.categoryId || !this.newComplaint.title || !this.newComplaint.description) return;
    this.complaintsService.create(this.newComplaint).subscribe(() => {
      this.showForm.set(false);
      this.newComplaint = { categoryId: '', title: '', description: '' };
      this.load();
    });
  }

  updateStatus(complaint: Complaint, status: string): void {
    const remark = this.remarkDrafts[complaint.id];
    this.complaintsService.updateStatus(complaint.id, status, remark).subscribe(() => {
      this.remarkDrafts[complaint.id] = '';
      this.load();
    });
  }
}

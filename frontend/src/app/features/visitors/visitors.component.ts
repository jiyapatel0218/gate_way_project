import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { AuthService } from '../../core/services/auth.service';
import { VisitorsService } from '../../core/services/api.services';
import { Visitor } from '../../core/models/models';

@Component({
  selector: 'app-visitors',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatChipsModule],
  templateUrl: './visitors.component.html',
  styleUrl: './visitors.component.scss'
})
export class VisitorsComponent implements OnInit {
  visitors = signal<Visitor[]>([]);
  searchTerm = '';
  isResident = () => this.authService.role() === 'Resident';
  displayedColumns = ['name', 'flat', 'purpose', 'type', 'status', 'time', 'actions'];

  constructor(private visitorsService: VisitorsService, private authService: AuthService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.visitorsService.getAll({ search: this.searchTerm || undefined }).subscribe((data) => this.visitors.set(data));
  }

  respond(visitor: Visitor, status: 'Approved' | 'Rejected'): void {
    this.visitorsService.respond(visitor.id, status).subscribe(() => this.load());
  }
}

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { VisitorsService, SocietyStructureService, MastersService } from '../../core/services/api.services';
import { Visitor, Flat, MasterItem, VisitorEntryType } from '../../core/models/models';

@Component({
  selector: 'app-gate-entry',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatButtonToggleModule, MatIconModule, MatTableModule, MatChipsModule],
  templateUrl: './gate-entry.component.html',
  styleUrl: './gate-entry.component.scss'
})
export class GateEntryComponent implements OnInit {
  flats = signal<Flat[]>([]);
  visitorTypes = signal<MasterItem[]>([]);
  todayVisitors = signal<Visitor[]>([]);
  displayedColumns = ['name', 'flat', 'type', 'status', 'time', 'actions'];

  entry = {
    name: '', mobileNumber: '', flatId: '', purpose: '', entryType: 'Visitor' as VisitorEntryType,
    visitorTypeId: null as string | null, vehicleNumber: ''
  };

  entryTypes: VisitorEntryType[] = ['Visitor', 'Delivery', 'Staff', 'Cab'];

  constructor(
    private visitorsService: VisitorsService,
    private structureService: SocietyStructureService,
    private mastersService: MastersService
  ) {}

  ngOnInit(): void {
    this.structureService.getFlats({}).subscribe((flats) => this.flats.set(flats));
    this.mastersService.getVisitorTypes().subscribe((types) => this.visitorTypes.set(types));
    this.loadToday();
  }

  loadToday(): void {
    this.visitorsService.getToday().subscribe((data) => this.todayVisitors.set(data));
  }

  submitEntry(): void {
    if (!this.entry.name || !this.entry.mobileNumber || !this.entry.flatId || !this.entry.purpose) return;

    const payload = { ...this.entry, vehicleNumber: this.entry.vehicleNumber || undefined };
    this.visitorsService.create(payload).subscribe(() => {
      Object.assign(this.entry, { name: '', mobileNumber: '', flatId: '', purpose: '', entryType: 'Visitor', visitorTypeId: null, vehicleNumber: '' });
      this.loadToday();
    });
  }

  markEntered(visitor: Visitor): void {
    this.visitorsService.markEntered(visitor.id).subscribe(() => this.loadToday());
  }

  markExited(visitor: Visitor): void {
    this.visitorsService.markExited(visitor.id).subscribe(() => this.loadToday());
  }
}

import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MastersService } from '../../core/services/api.services';
import { AuthService } from '../../core/services/auth.service';
import { MasterItem } from '../../core/models/models';

@Component({
  selector: 'app-masters',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatTabsModule, MatChipsModule],
  templateUrl: './masters.component.html',
  styleUrl: './masters.component.scss'
})
export class MastersComponent implements OnInit {
  complaintCategories = signal<MasterItem[]>([]);
  visitorTypes = signal<MasterItem[]>([]);
  maintenanceTypes = signal<any[]>([]);

  newCategoryName = '';
  newVisitorTypeName = '';
  newMaintenanceType = { name: '', defaultAmount: 0 };

  isSuperAdmin = () => this.authService.role() === 'SuperAdmin';

  constructor(private mastersService: MastersService, private authService: AuthService) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.mastersService.getComplaintCategories().subscribe((data) => this.complaintCategories.set(data));
    this.mastersService.getVisitorTypes().subscribe((data) => this.visitorTypes.set(data));
    this.mastersService.getMaintenanceTypes().subscribe((data) => this.maintenanceTypes.set(data));
  }

  addCategory(): void {
    if (!this.newCategoryName) return;
    this.mastersService.createComplaintCategory(this.newCategoryName).subscribe((item) => {
      this.complaintCategories.update((list) => [...list, item]);
      this.newCategoryName = '';
    });
  }

  addVisitorType(): void {
    if (!this.newVisitorTypeName) return;
    this.mastersService.createVisitorType(this.newVisitorTypeName).subscribe((item) => {
      this.visitorTypes.update((list) => [...list, item]);
      this.newVisitorTypeName = '';
    });
  }

  addMaintenanceType(): void {
    if (!this.newMaintenanceType.name || !this.newMaintenanceType.defaultAmount) return;
    this.mastersService.createMaintenanceType(this.newMaintenanceType.name, this.newMaintenanceType.defaultAmount).subscribe((item) => {
      this.maintenanceTypes.update((list) => [...list, item]);
      this.newMaintenanceType = { name: '', defaultAmount: 0 };
    });
  }
}

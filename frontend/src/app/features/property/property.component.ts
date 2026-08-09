import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { AuthService } from '../../core/services/auth.service';
import { PropertyService } from '../../core/services/api.services';
import { PropertyListing } from '../../core/models/models';

@Component({
  selector: 'app-property',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule],
  templateUrl: './property.component.html',
  styleUrl: './property.component.scss'
})
export class PropertyComponent implements OnInit {
  listings = signal<PropertyListing[]>([]);
  showForm = signal(false);
  isResident = () => this.authService.role() === 'Resident';
  isAdmin = () => this.authService.role() === 'SuperAdmin' || this.authService.role() === 'SocietyAdmin';

  newListing = { title: '', description: '', type: 'Rent', price: 0, contactPhone: '', contactEmail: '' };

  constructor(private propertyService: PropertyService, private authService: AuthService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.propertyService.getAll().subscribe((data) => this.listings.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
  }

  submit(): void {
    if (!this.newListing.title || !this.newListing.description || !this.newListing.price || !this.newListing.contactPhone) return;
    this.propertyService.create(this.newListing).subscribe(() => {
      this.showForm.set(false);
      this.newListing = { title: '', description: '', type: 'Rent', price: 0, contactPhone: '', contactEmail: '' };
      this.load();
    });
  }

  approve(listing: PropertyListing): void {
    this.propertyService.approve(listing.id, true).subscribe(() => this.load());
  }

  reject(listing: PropertyListing): void {
    const reason = prompt('Reason for rejection (optional):') ?? undefined;
    this.propertyService.approve(listing.id, false, reason).subscribe(() => this.load());
  }
}

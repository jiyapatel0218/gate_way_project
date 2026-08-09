import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatTabsModule } from '@angular/material/tabs';
import { ResidentsService } from '../../core/services/api.services';
import { Resident, FamilyMember, Vehicle, EmergencyContact } from '../../core/models/models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatListModule, MatTabsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  resident = signal<Resident | null>(null);
  familyMembers = signal<FamilyMember[]>([]);
  vehicles = signal<Vehicle[]>([]);
  emergencyContacts = signal<EmergencyContact[]>([]);

  newFamilyMember = { name: '', relation: '', age: null as number | null, phone: '' };
  newVehicle = { vehicleNumber: '', vehicleType: '', model: '' };
  newContact = { name: '', phone: '', relation: '' };

  constructor(private residentsService: ResidentsService) {}

  ngOnInit(): void {
    this.residentsService.getMyProfile().subscribe((profile) => {
      this.resident.set(profile);
      this.residentsService.getFamilyMembers(profile.id).subscribe((data) => this.familyMembers.set(data));
      this.residentsService.getVehicles(profile.id).subscribe((data) => this.vehicles.set(data));
      this.residentsService.getEmergencyContacts(profile.id).subscribe((data) => this.emergencyContacts.set(data));
    });
  }

  addFamilyMember(): void {
    const r = this.resident();
    if (!r || !this.newFamilyMember.name || !this.newFamilyMember.relation) return;
    this.residentsService.addFamilyMember(r.id, this.newFamilyMember).subscribe((member) => {
      this.familyMembers.update((list) => [...list, member]);
      this.newFamilyMember = { name: '', relation: '', age: null, phone: '' };
    });
  }

  removeFamilyMember(id: string): void {
    this.residentsService.deleteFamilyMember(id).subscribe(() => this.familyMembers.update((list) => list.filter((m) => m.id !== id)));
  }

  addVehicle(): void {
    const r = this.resident();
    if (!r || !this.newVehicle.vehicleNumber || !this.newVehicle.vehicleType) return;
    this.residentsService.addVehicle(r.id, this.newVehicle).subscribe((v) => {
      this.vehicles.update((list) => [...list, v]);
      this.newVehicle = { vehicleNumber: '', vehicleType: '', model: '' };
    });
  }

  removeVehicle(id: string): void {
    this.residentsService.deleteVehicle(id).subscribe(() => this.vehicles.update((list) => list.filter((v) => v.id !== id)));
  }

  addContact(): void {
    const r = this.resident();
    if (!r || !this.newContact.name || !this.newContact.phone) return;
    this.residentsService.addEmergencyContact(r.id, this.newContact).subscribe((c) => {
      this.emergencyContacts.update((list) => [...list, c]);
      this.newContact = { name: '', phone: '', relation: '' };
    });
  }

  removeContact(id: string): void {
    this.residentsService.deleteEmergencyContact(id).subscribe(() => this.emergencyContacts.update((list) => list.filter((c) => c.id !== id)));
  }
}

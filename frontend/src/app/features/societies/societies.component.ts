import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { SocietyStructureService } from '../../core/services/api.services';
import { Society, Block, Flat } from '../../core/models/models';

@Component({
  selector: 'app-societies',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule, MatExpansionModule],
  templateUrl: './societies.component.html',
  styleUrl: './societies.component.scss'
})
export class SocietiesComponent implements OnInit {
  societies = signal<Society[]>([]);
  showForm = signal(false);
  expandedSocietyId = signal<string | null>(null);

  // These must be signals, not plain objects: this app runs zoneless (no zone.js), so
  // mutating a plain Record after an async HTTP response never triggers a re-render —
  // only signal writes (or DOM-event-driven ngModel bindings) do.
  blocksBySociety = signal<Record<string, Block[]>>({});
  unitsByBlock = signal<Record<string, Flat[]>>({});

  newSociety = { name: '', address: '', city: '', state: '', pinCode: '', contactEmail: '', contactPhone: '' };
  newBlockName: Record<string, string> = {};
  newUnit: Record<string, { unitNumber: string; floor: number }> = {};

  editingBlockId = signal<string | null>(null);
  editBlockName: Record<string, string> = {};

  constructor(private structureService: SocietyStructureService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.structureService.getSocieties().subscribe((data) => this.societies.set(data));
  }

  createSociety(): void {
    if (!this.newSociety.name || !this.newSociety.address) return;
    this.structureService.createSociety(this.newSociety).subscribe(() => {
      this.showForm.set(false);
      this.newSociety = { name: '', address: '', city: '', state: '', pinCode: '', contactEmail: '', contactPhone: '' };
      this.load();
    });
  }

  toggleSocietyActive(society: Society): void {
    this.structureService.toggleSocietyActive(society.id).subscribe(() => this.load());
  }

  expandSociety(society: Society): void {
    const isExpanding = this.expandedSocietyId() !== society.id;
    this.expandedSocietyId.set(isExpanding ? society.id : null);
    if (isExpanding && !this.blocksBySociety()[society.id]) {
      this.structureService.getBlocks(society.id).subscribe((blocks) => {
        this.blocksBySociety.update((m) => ({ ...m, [society.id]: blocks }));
        blocks.forEach((b) => (this.newUnit[b.id] ??= { unitNumber: '', floor: 1 }));
      });
    }
  }

  addBlock(societyId: string): void {
    const name = this.newBlockName[societyId];
    if (!name) return;
    this.structureService.createBlock({ societyId, name }).subscribe((block) => {
      this.blocksBySociety.update((m) => ({ ...m, [societyId]: [...(m[societyId] ?? []), block] }));
      this.newUnit[block.id] ??= { unitNumber: '', floor: 1 };
      this.newBlockName[societyId] = '';
    });
  }

  startEditBlock(block: Block): void {
    this.editingBlockId.set(block.id);
    this.editBlockName[block.id] = block.name;
  }

  cancelEditBlock(): void {
    this.editingBlockId.set(null);
  }

  saveBlockEdit(societyId: string, block: Block): void {
    const name = this.editBlockName[block.id]?.trim();
    if (!name) return;
    this.structureService.updateBlock(block.id, { name }).subscribe((updated) => {
      this.blocksBySociety.update((m) => ({
        ...m,
        [societyId]: (m[societyId] ?? []).map((b) => (b.id === block.id ? updated : b))
      }));
      this.editingBlockId.set(null);
    });
  }

  deleteBlock(societyId: string, block: Block): void {
    if (!confirm(`Delete block "${block.name}"? This cannot be undone.`)) return;
    this.structureService.deleteBlock(block.id).subscribe(() => {
      this.blocksBySociety.update((m) => ({
        ...m,
        [societyId]: (m[societyId] ?? []).filter((b) => b.id !== block.id)
      }));
    });
  }

  loadUnits(block: Block): void {
    if (!this.unitsByBlock()[block.id]) {
      this.structureService.getFlats({ blockId: block.id }).subscribe((flats) => {
        this.unitsByBlock.update((m) => ({ ...m, [block.id]: flats }));
      });
    }
  }

  /** Adds a unit directly under a block: creates its (hidden) Wing and Flat record in one step,
   *  so the user only ever deals with a unit number + floor, not a separate Wing step. */
  addUnit(block: Block): void {
    const draft = this.newUnit[block.id];
    if (!draft?.unitNumber) return;
    this.structureService.createWing({ blockId: block.id, name: draft.unitNumber, totalFloors: draft.floor ?? 1 }).subscribe((wing) => {
      this.structureService.createFlat({ wingId: wing.id, flatNumber: draft.unitNumber, floor: draft.floor ?? 1, areaSqFt: 1000 }).subscribe((flat) => {
        this.unitsByBlock.update((m) => ({ ...m, [block.id]: [...(m[block.id] ?? []), flat] }));
        this.newUnit[block.id] = { unitNumber: '', floor: 1 };
      });
    });
  }
}

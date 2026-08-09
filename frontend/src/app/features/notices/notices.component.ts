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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { AuthService } from '../../core/services/auth.service';
import { NoticesService } from '../../core/services/api.services';
import { Notice, NoticeType } from '../../core/models/models';

@Component({
  selector: 'app-notices',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule, MatDatepickerModule, MatNativeDateModule],
  templateUrl: './notices.component.html',
  styleUrl: './notices.component.scss'
})
export class NoticesComponent implements OnInit {
  notices = signal<Notice[]>([]);
  showForm = signal(false);
  isAdmin = () => this.authService.role() === 'SuperAdmin' || this.authService.role() === 'SocietyAdmin';

  newNotice: { title: string; content: string; type: NoticeType; eventDate: Date | null } =
    { title: '', content: '', type: 'Notice', eventDate: null };

  constructor(private noticesService: NoticesService, private authService: AuthService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.noticesService.getAll().subscribe((data) => this.notices.set(data));
  }

  toggleForm(): void {
    this.showForm.update((v) => !v);
  }

  submit(): void {
    if (!this.newNotice.title || !this.newNotice.content) return;
    const payload = { ...this.newNotice, eventDate: this.newNotice.eventDate || undefined };
    this.noticesService.create(payload).subscribe(() => {
      this.showForm.set(false);
      this.newNotice = { title: '', content: '', type: 'Notice', eventDate: null };
      this.load();
    });
  }

  delete(notice: Notice): void {
    if (!confirm('Delete this notice?')) return;
    this.noticesService.delete(notice.id).subscribe(() => this.load());
  }
}

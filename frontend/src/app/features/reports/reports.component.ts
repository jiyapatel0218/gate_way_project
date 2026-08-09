import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ReportsService, triggerBlobDownload } from '../../core/services/api.services';

interface ReportDef {
  key: string;
  label: string;
  description: string;
  icon: string;
}

const REPORTS: ReportDef[] = [
  { key: 'visitors', label: 'Visitor Report', description: 'All visitor entries and exits', icon: 'how_to_reg' },
  { key: 'complaints', label: 'Complaint Report', description: 'Complaint status and resolution details', icon: 'report_problem' },
  { key: 'payments', label: 'Payment Report', description: 'Maintenance payment records', icon: 'payments' },
  { key: 'property-listings', label: 'Property Listing Report', description: 'Rent and sale listings', icon: 'real_estate_agent' },
  { key: 'residents', label: 'Resident Report', description: 'Resident directory by flat', icon: 'groups' }
];

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent {
  reports = REPORTS;

  constructor(private reportsService: ReportsService) {}

  download(report: ReportDef, format: 'excel' | 'pdf'): void {
    this.reportsService.download(report.key, format).subscribe((blob) => {
      const ext = format === 'pdf' ? 'pdf' : 'xlsx';
      triggerBlobDownload(blob, `${report.key}-report.${ext}`);
    });
  }
}

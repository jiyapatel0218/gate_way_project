import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { DashboardService } from '../../core/services/api.services';
import { DashboardSummary } from '../../core/models/models';

interface StatTile {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  summary = signal<DashboardSummary | null>(null);
  tiles = signal<StatTile[]>([]);

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.dashboardService.getSummary().subscribe((data) => {
      this.summary.set(data);
      this.tiles.set([
        { label: 'Total Residents', value: `${data.totalResidents}`, icon: 'groups', color: '#0e5e4c' },
        { label: 'Visitors Today', value: `${data.visitorsToday}`, icon: 'how_to_reg', color: '#2c5fa8' },
        { label: 'Active Complaints', value: `${data.activeComplaints}`, icon: 'report_problem', color: '#e8a93d' },
        { label: 'Pending Complaints', value: `${data.pendingComplaints}`, icon: 'hourglass_empty', color: '#c4432e' },
        { label: 'Closed Complaints', value: `${data.closedComplaints}`, icon: 'task_alt', color: '#1e7a46' },
        { label: 'Monthly Collection', value: `₹${data.monthlyMaintenanceCollection.toLocaleString()}`, icon: 'payments', color: '#0e5e4c' },
        { label: 'Pending Maintenance', value: `₹${data.pendingMaintenance.toLocaleString()}`, icon: 'account_balance_wallet', color: '#c4432e' },
        { label: 'Property Listings', value: `${data.propertyListingsCount}`, icon: 'real_estate_agent', color: '#2c5fa8' },
        { label: 'Active Notices', value: `${data.activeNoticesCount}`, icon: 'campaign', color: '#e8a93d' }
      ]);
    });
  }
}

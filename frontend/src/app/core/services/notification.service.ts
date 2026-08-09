import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AppNotification } from '../models/models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection?: signalR.HubConnection;
  readonly notifications = signal<AppNotification[]>([]);
  readonly unreadCount = signal(0);

  constructor(private http: HttpClient, private authService: AuthService) {}

  connect(): void {
    if (this.hubConnection || !this.authService.accessToken) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.authService.accessToken ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (payload: AppNotification) => {
      this.notifications.update((list) => [payload, ...list]);
      this.unreadCount.update((c) => c + 1);
    });

    this.hubConnection.start().catch((err) => console.error('SignalR connection error', err));
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  loadRecent(): void {
    this.http.get<AppNotification[]>(`${environment.apiUrl}/notifications`).subscribe((list) => {
      this.notifications.set(list);
      this.unreadCount.set(list.filter((n) => !n.isRead).length);
    });
  }

  markRead(id: string): void {
    this.http.post(`${environment.apiUrl}/notifications/${id}/read`, {}).subscribe(() => {
      this.notifications.update((list) => list.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
      this.unreadCount.update((c) => Math.max(0, c - 1));
    });
  }

  markAllRead(): void {
    this.http.post(`${environment.apiUrl}/notifications/read-all`, {}).subscribe(() => {
      this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
      this.unreadCount.set(0);
    });
  }
}

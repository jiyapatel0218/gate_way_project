import { Component, Inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MaintenanceService, PaymentOrderResponse } from '../../core/services/api.services';

declare global {
  interface Window {
    Razorpay?: new (options: Record<string, unknown>) => { open(): void };
  }
}

export interface OnlinePaymentDialogData {
  invoiceId: string;
  label: string;
}

type DialogStage = 'creating-order' | 'mock-checkout' | 'processing' | 'success' | 'error';

@Component({
  selector: 'app-online-payment-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './online-payment-dialog.component.html',
  styleUrl: './online-payment-dialog.component.scss'
})
export class OnlinePaymentDialogComponent implements OnInit {
  stage = signal<DialogStage>('creating-order');
  amount = signal(0);
  provider = signal('');
  errorMessage = signal('');
  private order?: PaymentOrderResponse;

  constructor(
    private dialogRef: MatDialogRef<OnlinePaymentDialogComponent>,
    private maintenanceService: MaintenanceService,
    @Inject(MAT_DIALOG_DATA) public data: OnlinePaymentDialogData
  ) {}

  ngOnInit(): void {
    this.maintenanceService.createOnlineOrder(this.data.invoiceId).subscribe({
      next: (order) => {
        this.order = order;
        this.amount.set(order.amount);
        this.provider.set(order.provider);

        if (order.provider === 'Razorpay' && order.checkoutKeyId) {
          this.openRazorpayCheckout(order);
        } else {
          this.stage.set('mock-checkout');
        }
      },
      error: () => {
        this.errorMessage.set('Could not start the payment. Please try again.');
        this.stage.set('error');
      }
    });
  }

  private loadRazorpayScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (window.Razorpay) return resolve();
      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Failed to load Razorpay checkout script'));
      document.body.appendChild(script);
    });
  }

  private async openRazorpayCheckout(order: { providerOrderId: string; checkoutKeyId: string | null; amount: number; currency: string }): Promise<void> {
    try {
      await this.loadRazorpayScript();
    } catch {
      this.errorMessage.set('Could not load the payment checkout. Please try again.');
      this.stage.set('error');
      return;
    }

    this.stage.set('processing');

    const razorpay = new window.Razorpay!({
      key: order.checkoutKeyId,
      amount: Math.round(order.amount * 100),
      currency: order.currency,
      order_id: order.providerOrderId,
      name: 'GreenGate Residency',
      description: this.data.label,
      handler: (response: { razorpay_order_id: string; razorpay_payment_id: string; razorpay_signature: string }) => {
        this.verify(response.razorpay_order_id, response.razorpay_payment_id, response.razorpay_signature);
      },
      modal: {
        ondismiss: () => this.dialogRef.close(null)
      }
    });

    razorpay.open();
  }

  simulateSuccessfulPayment(): void {
    if (!this.order) return;
    this.stage.set('processing');
    const fakePaymentId = `pay_mock_${Math.random().toString(36).slice(2, 12)}`;
    this.verify(this.order.providerOrderId, fakePaymentId, null);
  }

  private verify(providerOrderId: string, providerPaymentId: string, signature: string | null): void {
    this.maintenanceService.verifyOnlinePayment({ providerOrderId, providerPaymentId, signature }).subscribe({
      next: (payment) => {
        this.stage.set('success');
        setTimeout(() => this.dialogRef.close(payment), 1200);
      },
      error: () => {
        this.errorMessage.set('Payment verification failed. Please try again.');
        this.stage.set('error');
      }
    });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}

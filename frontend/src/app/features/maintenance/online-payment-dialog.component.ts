import { Component, Inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MaintenanceService, PaymentOrderResponse, PaymentQrCodesService, resolveMediaUrl } from '../../core/services/api.services';
import { Payment } from '../../core/models/models';

declare global {
  interface Window {
    Razorpay?: new (options: Record<string, unknown>) => { open(): void };
  }
}

export interface OnlinePaymentDialogData {
  invoiceId: string;
  label: string;
}

type DialogStage = 'creating-order' | 'qr-scan' | 'processing' | 'success' | 'error';

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
  errorTitle = signal('Payment Failed');
  errorMessage = signal('');
  paymentResult = signal<Payment | null>(null);
  qrImageUrl = signal<string | null>(null);
  payeeName = signal<string | undefined>(undefined);

  private order?: PaymentOrderResponse;
  private verifying = false;

  constructor(
    private dialogRef: MatDialogRef<OnlinePaymentDialogComponent>,
    private maintenanceService: MaintenanceService,
    private paymentQrCodesService: PaymentQrCodesService,
    private router: Router,
    @Inject(MAT_DIALOG_DATA) public data: OnlinePaymentDialogData
  ) {}

  ngOnInit(): void {
    this.startOrder();
  }

  private startOrder(): void {
    this.stage.set('creating-order');
    this.errorMessage.set('');

    this.maintenanceService.createOnlineOrder(this.data.invoiceId).subscribe({
      next: (order) => {
        this.order = order;
        this.amount.set(order.amount);
        this.provider.set(order.provider);

        if (order.provider === 'Razorpay' && order.checkoutKeyId) {
          this.openRazorpayCheckout(order);
        } else {
          this.loadQrAndShow();
        }
      },
      error: (err) => {
        this.errorTitle.set('Payment Failed');
        this.errorMessage.set(err?.error?.message || 'Could not start the payment. Please try again.');
        this.stage.set('error');
      }
    });
  }

  /** Fetches the QR the caller is allowed to see (resolved server-side to their own flat's Society/Block) before showing the scan screen. */
  private loadQrAndShow(): void {
    this.paymentQrCodesService.getMine().subscribe({
      next: (qr) => {
        this.qrImageUrl.set(resolveMediaUrl(qr.qrImageUrl));
        this.payeeName.set(qr.payeeName ?? undefined);
        this.stage.set('qr-scan');
      },
      error: (err) => {
        this.errorTitle.set('Payment Failed');
        this.errorMessage.set(err?.error?.message || 'No payment QR has been configured for your society yet. Please contact your admin.');
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
      this.errorTitle.set('Payment Failed');
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
        ondismiss: () => {
          this.errorTitle.set('Payment Cancelled');
          this.errorMessage.set('You closed the payment window before it completed.');
          this.stage.set('error');
        }
      }
    });

    razorpay.open();
  }

  /** User has scanned the QR and completed the UPI transfer in their own app; confirm it here. */
  confirmManualPayment(): void {
    if (!this.order || this.verifying) return;
    this.stage.set('processing');
    const reference = `upi_${Math.random().toString(36).slice(2, 12)}`;
    this.verify(this.order.providerOrderId, reference, null);
  }

  /** User cancelled from the QR screen without paying. */
  cancelPayment(): void {
    this.errorTitle.set('Payment Cancelled');
    this.errorMessage.set('You cancelled the payment. No amount was deducted.');
    this.stage.set('error');
  }

  private verify(providerOrderId: string, providerPaymentId: string, signature: string | null): void {
    // Guards against a double-click firing two /verify calls for the same order; the backend
    // itself is the real guard (rejects an order that's already Paid) — this just avoids the
    // redundant request in the common case.
    if (this.verifying) return;
    this.verifying = true;

    this.maintenanceService.verifyOnlinePayment({ providerOrderId, providerPaymentId, signature }).subscribe({
      next: (payment) => {
        this.verifying = false;
        this.paymentResult.set(payment);
        this.stage.set('success');
      },
      error: (err) => {
        this.verifying = false;
        this.errorTitle.set('Payment Failed');
        this.errorMessage.set(err?.error?.message || 'Payment verification failed. Please try again.');
        this.stage.set('error');
      }
    });
  }

  retry(): void {
    this.startOrder();
  }

  done(): void {
    const payment = this.paymentResult();
    this.dialogRef.close(payment);
    this.router.navigate(['/maintenance']);
  }

  close(): void {
    this.dialogRef.close(null);
  }
}

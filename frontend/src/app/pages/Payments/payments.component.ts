import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// Components
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { IconComponent } from '../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { PaymentDetailModalComponent } from './components/payment-detail/payment-detail-modal.component';

// Services
import { PaymentService, BillingDto, CreatePaymentDto } from '../../services/payment.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-payments',
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HeaderComponent,
    ButtonComponent,
    IconComponent,
    SmallIconComponent,
    PaymentDetailModalComponent
  ]
})
export class PaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private toastService = inject(ToastService);

  // State
  billings: BillingDto[] = [];
  selectedBilling: BillingDto | null = null;
  showDetailModal = false;
  isLoading = false;
  isProcessingPayment = false;

  // Computed properties
  get totalPending(): number {
    return this.billings
      .filter(b => b.status === 'PENDING')
      .reduce((sum, b) => sum + b.amount, 0);
  }

  get totalPaid(): number {
    return this.billings
      .filter(b => b.status === 'PAID')
      .reduce((sum, b) => sum + b.amount, 0);
  }

  get pendingBillings(): BillingDto[] {
    return this.billings.filter(b => b.status === 'PENDING');
  }

  get paidBillings(): BillingDto[] {
    return this.billings.filter(b => b.status === 'PAID');
  }

  ngOnInit(): void {
    this.loadBillings();
  }

  loadBillings(): void {
    this.isLoading = true;
    this.paymentService.getBillings().subscribe({
      next: (billings) => {
        this.billings = billings;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading billings:', error);
        this.toastService.error('Erreur lors du chargement des factures');
        this.isLoading = false;
      }
    });
  }

  openDetailModal(billing: BillingDto): void {
    this.selectedBilling = billing;
    this.showDetailModal = true;
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedBilling = null;
  }

  downloadInvoice(billing: BillingDto, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }

    this.paymentService.downloadInvoicePdf(billing.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Facture_${billing.invoiceNumber?.replace('/', '-') || billing.id}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.toastService.success('Facture téléchargée');
      },
      error: (error) => {
        console.error('Error downloading invoice:', error);
        this.toastService.error('Erreur lors du téléchargement de la facture');
      }
    });
  }

  processPayment(): void {
    if (!this.selectedBilling || this.selectedBilling.status === 'PAID') {
      return;
    }

    this.isProcessingPayment = true;

    const paymentDto: CreatePaymentDto = {
      invoiceId: this.selectedBilling.id,
      method: 'CARD'
    };

    this.paymentService.processPayment(paymentDto).subscribe({
      next: (payment) => {
        this.toastService.success('Paiement effectué avec succès');
        this.isProcessingPayment = false;
        this.closeDetailModal();
        this.loadBillings(); // Reload to reflect payment status
      },
      error: (error) => {
        console.error('Error processing payment:', error);
        this.toastService.error('Erreur lors du traitement du paiement');
        this.isProcessingPayment = false;
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  getStatusLabel(status: string): string {
    return status === 'PAID' ? 'Payé' : 'En attente';
  }

  getStatusClass(status: string): string {
    return status === 'PAID' ? 'status-paid' : 'status-pending';
  }

  onModalDownload(billing: BillingDto): void {
    this.downloadInvoice(billing);
  }
}

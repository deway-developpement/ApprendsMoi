import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

// Components
import { ButtonComponent } from '../../../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../../../components/shared/SmallIcon/small-icon.component';

// Services
import { BillingDto } from '../../../../services/payment.service';

@Component({
  selector: 'app-payment-detail-modal',
  templateUrl: './payment-detail-modal.component.html',
  styleUrls: ['./payment-detail-modal.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ButtonComponent,
    SmallIconComponent
  ]
})
export class PaymentDetailModalComponent {
  @Input() billing: BillingDto | null = null;
  @Input() isProcessing = false;
  
  @Output() close = new EventEmitter<void>();
  @Output() download = new EventEmitter<BillingDto>();
  @Output() pay = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  onDownload(): void {
    if (this.billing) {
      this.download.emit(this.billing);
    }
  }

  onPay(): void {
    this.pay.emit();
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
}

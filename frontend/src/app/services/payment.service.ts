import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface BillingDto {
  id: string;
  courseId: string;
  subjectName: string;
  childName: string;
  teacherName: string;
  amount: number;
  amountHT: number;
  vatAmount: number;
  status: string; // Changed from literal union to string to match backend
  issuedAt: string;
  paidAt?: string;
  invoiceNumber?: string;
}

export interface PaymentDto {
  id: string;
  invoiceId: string;
  parentId: string;
  amount: number;
  method: string;
  status: string;
  stripePaymentIntentId?: string;
  errorMessage?: string;
  createdAt: string;
  processedAt?: string;
}

export interface CreatePaymentDto {
  invoiceId: string;
  method: string;
  stripePaymentIntentId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/api/Payments`;

  constructor(private http: HttpClient) {}

  /**
   * Get all billings for the current user
   */
  getBillings(): Observable<BillingDto[]> {
    return this.http.get<BillingDto[]>(`${this.apiUrl}/user`);
  }

  /**
   * Get a specific billing by ID
   */
  getBillingById(id: string): Observable<BillingDto> {
    return this.http.get<BillingDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Download invoice PDF
   */
  downloadInvoicePdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/invoice/${id}/pdf`, {
      responseType: 'blob'
    });
  }

  /**
   * Process a payment for an invoice
   */
  processPayment(dto: CreatePaymentDto): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(`${this.apiUrl}/process`, dto);
  }
}

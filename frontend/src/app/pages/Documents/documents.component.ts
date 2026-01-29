import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HeaderComponent } from '../../components/Header/header.component';
import { environment } from '../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

interface DocumentUploadResult {
  fileName: string;
  documentType: string;
  success: boolean;
  message: string;
  documentId?: string;
}

interface BatchUploadResponse {
  results: DocumentUploadResult[];
  successCount: number;
  failureCount: number;
}

interface TeacherDocumentDto {
  id: string;
  teacherId: string;
  teacherFirstName?: string;
  teacherLastName?: string;
  documentType: number | string;
  fileName: string;
  status: number | string;
  rejectionReason?: string;
  uploadedAt: string;
  reviewedAt?: string;
  reviewedBy?: string;
}

interface PendingDocument {
  id: string;
  teacherId: string;
  teacherFirstName?: string;
  teacherLastName?: string;
  documentType: number | string;
  fileName: string;
  status: number | string;
  uploadedAt: string;
}

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, HeaderComponent],
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.scss']
})
export class DocumentsComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private fb = inject(FormBuilder);

  myDocuments: TeacherDocumentDto[] = [];
  pendingDocuments: PendingDocument[] = [];
  isTeacher = false;
  isAdmin = false;
  uploading = false;
  loadingDocuments = false;
  loadingPending = false;

  uploadForm!: FormGroup;
  selectedIdPaper: File | null = null;
  selectedDiplomas: File[] = [];
  validateForm!: FormGroup;

  DocumentType = DocumentType;
  DocumentStatus = DocumentStatus;

  ngOnInit(): void {
    this.checkUserRole();
    this.initializeForms();
    this.loadMyDocuments();
    if (this.isAdmin) {
      this.loadPendingDocuments();
    }
  }

  initializeForms(): void {
    this.uploadForm = this.fb.group({
      files: [null, Validators.required]
    });

    this.validateForm = this.fb.group({
      documentId: ['', Validators.required],
      approve: [false, Validators.required],
      rejectionReason: ['']
    });
  }

  checkUserRole(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.isTeacher = user.profileType === 1; // ProfileType.Teacher
        this.isAdmin = user.profileType === 0; // ProfileType.Admin
      }
    });
  }

  onFileSelected(event: Event, documentType: DocumentType): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      if (documentType === DocumentType.ID_PAPER) {
        this.selectedIdPaper = input.files[0];
      } else if (documentType === DocumentType.DIPLOMA) {
        this.selectedDiplomas = Array.from(input.files);
      }
    }
  }

  async uploadDocuments(): Promise<void> {
    if (!this.selectedIdPaper && this.selectedDiplomas.length === 0) {
      this.toastService.error('Veuillez sélectionner au moins un fichier');
      return;
    }

    this.uploading = true;
    const formData = new FormData();
    let fileIndex = 0;

    // Add ID Paper
    if (this.selectedIdPaper) {
      formData.append('files', this.selectedIdPaper);
      formData.append(`documentTypes[${fileIndex}]`, String(DocumentType.ID_PAPER));
      fileIndex++;
    }

    // Add Diplomas
    for (const diploma of this.selectedDiplomas) {
      formData.append('files', diploma);
      formData.append(`documentTypes[${fileIndex}]`, String(DocumentType.DIPLOMA));
      fileIndex++;
    }

    try {
      const response = await this.http
        .post<BatchUploadResponse>(`${environment.apiUrl}/api/documents/upload`, formData)
        .toPromise();

      if (response) {
        this.toastService.success(`Téléchargement de ${response.successCount} document(s) réussi(s)`);
        if (response.failureCount > 0) {
          this.toastService.warning(`Échec du téléchargement de ${response.failureCount} document(s)`);
        }
        this.selectedIdPaper = null;
        this.selectedDiplomas = [];
        // Reset file inputs
        const idPaperInput = document.getElementById('file-ID_PAPER') as HTMLInputElement;
        const diplomaInput = document.getElementById('file-DIPLOMA') as HTMLInputElement;
        if (idPaperInput) idPaperInput.value = '';
        if (diplomaInput) diplomaInput.value = '';
        await this.loadMyDocuments();
      }
    } catch (error: any) {
      console.error('Upload error:', error);
      this.toastService.error(error.error?.message || 'Échec du téléchargement des documents');
    } finally {
      this.uploading = false;
    }
  }

  async loadMyDocuments(): Promise<void> {
    if (!this.isTeacher) return;

    this.loadingDocuments = true;
    try {
      const response = await this.http
        .get<TeacherDocumentDto[]>(`${environment.apiUrl}/api/documents/my-documents`)
        .toPromise();

      this.myDocuments = response || [];
    } catch (error: any) {
      console.error('Error loading documents:', error);
      this.toastService.error('Impossible de charger les documents');
    } finally {
      this.loadingDocuments = false;
    }
  }

  async loadPendingDocuments(): Promise<void> {
    if (!this.isAdmin) return;

    this.loadingPending = true;
    try {
      const response = await this.http
        .get<PendingDocument[]>(`${environment.apiUrl}/api/documents/pending`)
        .toPromise();

      this.pendingDocuments = response || [];
    } catch (error: any) {
      console.error('Error loading pending documents:', error);
      this.toastService.error('Impossible de charger les documents en attente');
    } finally {
      this.loadingPending = false;
    }
  }

  async validateDocuments(documentIds: string[], approve: boolean): Promise<void> {
    // Ask for confirmation when batch approving multiple documents
    if (approve && documentIds.length > 1) {
      if (!confirm(`Êtes-vous sûr de vouloir approuver ${documentIds.length} document(s) ?`)) {
        return;
      }
    }

    const validationItems = documentIds.map(id => ({
      documentId: id,
      approve: approve,
      rejectionReason: approve ? null : prompt(`Raison du rejet de ${id} ?`)
    }));

    try {
      const response = await this.http
        .post(`${environment.apiUrl}/api/documents/validate`, { documents: validationItems })
        .toPromise();

      this.toastService.success(`${approve ? 'Approuvé' : 'Rejeté'} documents avec succès`);
      await this.loadPendingDocuments();
    } catch (error: any) {
      console.error('Validation error:', error);
      this.toastService.error(error.error?.message || 'Impossible de valider les documents');
    }
  }

  async downloadDocument(documentId: string, fileName: string): Promise<void> {
    try {
      const response = await this.http
        .get(`${environment.apiUrl}/api/documents/${documentId}/download`, {
          responseType: 'blob'
        })
        .toPromise();

      if (response) {
        const url = window.URL.createObjectURL(response);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        window.URL.revokeObjectURL(url);
      }
    } catch (error: any) {
      console.error('Download error:', error);
      this.toastService.error('Impossible de télécharger le document');
    }
  }

  async deleteDocument(documentId: string): Promise<void> {
    if (confirm('Êtes-vous sûr de vouloir supprimer ce document ?')) {
      try {
        await this.http
          .delete(`${environment.apiUrl}/api/documents/${documentId}`)
          .toPromise();

        this.toastService.success('Document supprimé avec succès');
        await this.loadMyDocuments();
      } catch (error: any) {
        console.error('Delete error:', error);
        this.toastService.error('Impossible de supprimer le document');
      }
    }
  }

  getPendingDocumentIds(): string[] {
    return this.pendingDocuments.map(d => d.id);
  }

  getTeacherName(firstN: any, lastN: any): string {
    const firstName = firstN || '';
    const lastName = lastN || '';
    const fullName = `${firstName} ${lastName}`.trim();
    return fullName || 'Nom inconnu';
  }

  getDocumentTypeLabel(documentType: any): string {
    if (typeof documentType === 'string') {
      return documentType === 'ID_PAPER' ? 'Pièce d\'identité' : 'Diplôme';
    }
    // Handle numeric enum values
    if (documentType === 0 || documentType === DocumentType.ID_PAPER) {
      return 'Pièce d\'identité';
    }
    if (documentType === 1 || documentType === DocumentType.DIPLOMA) {
      return 'Diplôme';
    }
    return 'Document';
  }

  getDocumentStatusClass(status: any): string {
    // Handle numeric values
    if (status === 0 || status === DocumentStatus.PENDING) {
      return 'status-pending';
    }
    if (status === 1 || status === DocumentStatus.APPROVED) {
      return 'status-approved';
    }
    if (status === 2 || status === DocumentStatus.REJECTED) {
      return 'status-rejected';
    }
    // Handle string values
    switch (status) {
      case 'PENDING':
      case DocumentStatus.PENDING:
        return 'status-pending';
      case 'APPROVED':
      case DocumentStatus.APPROVED:
        return 'status-approved';
      case 'REJECTED':
      case DocumentStatus.REJECTED:
        return 'status-rejected';
      default:
        return '';
    }
  }

  getDocumentStatusLabel(status: any): string {
    // Convert to number if string
    const numStatus = typeof status === 'string' ? parseInt(status, 10) : status;
    
    switch (numStatus) {
      case 0:
      case DocumentStatus.PENDING:
        return 'En attente';
      case 1:
      case DocumentStatus.APPROVED:
        return 'Approuvé';
      case 2:
      case DocumentStatus.REJECTED:
        return 'Rejeté';
      default:
        return status?.toString() || 'Inconnu';
    }
  }
}

enum DocumentType {
  ID_PAPER = 0,
  DIPLOMA = 1
}

enum DocumentStatus {
  PENDING = 0,
  APPROVED = 1,
  REJECTED = 2
}

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { HeaderComponent } from '../../components/Header/header.component';
import { environment } from '../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ModalService } from '../../services/modal.service';
import {
  DocumentUploadResult,
  BatchUploadResponse,
  TeacherDocumentDto,
  PendingDocument,
  DocumentType,
  DocumentStatus
} from './documents.models';
import { DocumentUtils } from './documents.utils';
import { SubjectSelectorComponent } from './components/subject-selector/subject-selector.component';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, HeaderComponent, SubjectSelectorComponent],
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.scss']
})
export class DocumentsComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private modalService = inject(ModalService);
  private location = inject(Location);
  private destroy$ = new Subject<void>();

  myDocuments: TeacherDocumentDto[] = [];
  pendingDocuments: PendingDocument[] = [];
  isTeacher = false;
  isAdmin = false;
  uploading = false;
  loadingDocuments = false;
  loadingPending = false;
  confirmingDeleteId: string | null = null;

  selectedIdPaper: File | null = null;
  selectedDiplomas: File[] = [];

  DocumentType = DocumentType;
  DocumentStatus = DocumentStatus;

  ngOnInit(): void {
    this.checkUserRole();
    this.loadMyDocuments();
    if (this.isAdmin) {
      this.loadPendingDocuments();
    }
  }

  checkUserRole(): void {
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (user) {
          this.isTeacher = user.profileType === 1; // ProfileType.Teacher
          this.isAdmin = user.profileType === 0; // ProfileType.Admin
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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

  async validateDocuments(documents: PendingDocument[] | string[], approve: boolean): Promise<void> {
    // Convert string IDs to documents if needed
    const docsToValidate = documents.map(doc => 
      typeof doc === 'string' 
        ? this.pendingDocuments.find(d => d.id === doc)
        : doc
    ).filter((doc): doc is PendingDocument => doc !== undefined);

    // Ask for confirmation when batch approving multiple documents
    if (approve && docsToValidate.length > 1) {
      const confirmed = await this.modalService.confirm(
        `Êtes-vous sûr de vouloir approuver ${docsToValidate.length} document(s) ?`,
        'Confirmation'
      );
      if (!confirmed) {
        return;
      }
    }

    const validationItems = [];
    
    for (const doc of docsToValidate) {
      let rejectionReason = null;
      
      if (!approve) {
        rejectionReason = await this.modalService.prompt(
          `Raison du rejet de ${doc.fileName}`,
          'Raison du rejet',
          'Entrez la raison du rejet...'
        );
        
        // User cancelled the prompt
        if (rejectionReason === null) {
          continue;
        }
      }
      
      validationItems.push({
        documentId: doc.id,
        approve: approve,
        rejectionReason: rejectionReason
      });
    }
    
    if (validationItems.length === 0) {
      return;
    }

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

  confirmDeleteDocument(documentId: string): void {
    this.confirmingDeleteId = documentId;
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }

  async deleteDocument(documentId: string): Promise<void> {
    try {
      await this.http
        .delete(`${environment.apiUrl}/api/documents/${documentId}`)
        .toPromise();

      this.toastService.success('Document supprimé avec succès');
      this.confirmingDeleteId = null;
      await this.loadMyDocuments();
    } catch (error: any) {
      console.error('Delete error:', error);
      this.toastService.error('Impossible de supprimer le document');
      this.confirmingDeleteId = null;
    }
  }

  getPendingDocuments(): PendingDocument[] {
    return this.pendingDocuments;
  }

  getTeacherName(firstN: any, lastN: any): string {
    return DocumentUtils.getTeacherName(firstN, lastN);
  }

  getDocumentTypeLabel(documentType: any): string {
    return DocumentUtils.getDocumentTypeLabel(documentType);
  }

  getDocumentStatusClass(status: any): string {
    return DocumentUtils.getDocumentStatusClass(status);
  }

  getDocumentStatusLabel(status: any): string {
    return DocumentUtils.getDocumentStatusLabel(status);
  }

  goBack(): void {
    this.location.back();
  }
}


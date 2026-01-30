export interface DocumentUploadResult {
  fileName: string;
  documentType: string;
  success: boolean;
  message: string;
  documentId?: string;
}

export interface BatchUploadResponse {
  results: DocumentUploadResult[];
  successCount: number;
  failureCount: number;
}

export interface TeacherDocumentDto {
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

export interface PendingDocument {
  id: string;
  teacherId: string;
  teacherFirstName?: string;
  teacherLastName?: string;
  documentType: number | string;
  fileName: string;
  status: number | string;
  uploadedAt: string;
}

export enum DocumentType {
  ID_PAPER = 0,
  DIPLOMA = 1
}

export enum DocumentStatus {
  PENDING = 0,
  APPROVED = 1,
  REJECTED = 2
}

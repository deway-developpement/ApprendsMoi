import { DocumentType, DocumentStatus } from './documents.models';

export class DocumentUtils {
  static getTeacherName(firstN: any, lastN: any): string {
    const firstName = firstN || '';
    const lastName = lastN || '';
    const fullName = `${firstName} ${lastName}`.trim();
    return fullName || 'Nom inconnu';
  }

  static getDocumentTypeLabel(documentType: any): string {
    if (documentType === 0 || documentType === DocumentType.ID_PAPER) {
      return 'Pièce d\'identité';
    }
    if (documentType === 1 || documentType === DocumentType.DIPLOMA) {
      return 'Diplôme';
    }
    return 'Document';
  }

  static getDocumentStatusClass(status: any): string {
    switch (status) {
      case 0:
      case DocumentStatus.PENDING:
        return 'status-pending';
      case 1:
      case DocumentStatus.APPROVED:
        return 'status-approved';
      case 2:
      case DocumentStatus.REJECTED:
        return 'status-rejected';
      default:
        return '';
    }
  }

  static getDocumentStatusLabel(status: any): string {
    switch (status) {
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

using backend.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Documents;

public class TeacherDocumentDto {
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
}

public class UploadDocumentRequest {
    [Required]
    public DocumentType DocumentType { get; set; }
    
    [Required]
    public IFormFile File { get; set; } = null!;
}

public class ValidateDocumentRequest {
    [Required]
    public Guid DocumentId { get; set; }
    
    [Required]
    public bool Approve { get; set; }
    
    public string? RejectionReason { get; set; }
}

public class BatchValidateDocumentsRequest {
    [Required]
    public List<DocumentValidationItem> Documents { get; set; } = new();
}

public class DocumentValidationItem {
    [Required]
    public Guid DocumentId { get; set; }
    
    [Required]
    public bool Approve { get; set; }
    
    public string? RejectionReason { get; set; }
}

public class DocumentUploadResponse {
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BatchUploadDocumentsRequest {
    [Required]
    public List<BatchDocumentItem> Documents { get; set; } = new();
}

public class BatchDocumentItem {
    [Required]
    public DocumentType DocumentType { get; set; }
    
    [Required]
    public IFormFile File { get; set; } = null!;
}

public class BatchUploadResponse {
    public List<DocumentUploadResult> Results { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class DocumentUploadResult {
    public string FileName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
}

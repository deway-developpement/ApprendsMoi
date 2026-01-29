using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Documents;

public class DocumentService(AppDbContext db, IWebHostEnvironment environment) {
    private readonly AppDbContext _db = db;
    private readonly IWebHostEnvironment _environment = environment;
    private const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
    private const int MAX_DIPLOMAS = 5;

    public async Task<(bool Success, string Message, Guid? DocumentId)> UploadDocumentAsync(
        Guid teacherId, 
        DocumentType documentType, 
        IFormFile file, 
        CancellationToken ct = default) {
        
        // Validate file type
        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) {
            return (false, "Only PDF files are allowed", null);
        }

        // Validate file size
        if (file.Length > MAX_FILE_SIZE) {
            return (false, $"File size cannot exceed {MAX_FILE_SIZE / 1024 / 1024}MB", null);
        }

        // Check if teacher exists
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherId, ct);
        if (teacher == null) {
            return (false, "Teacher not found", null);
        }

        // Validate document type constraints
        if (documentType == DocumentType.ID_PAPER) {
            var existingIdPaper = await _db.TeacherDocuments
                .Where(d => d.TeacherId == teacherId && d.DocumentType == DocumentType.ID_PAPER)
                .ToListAsync(ct);
            
            if (existingIdPaper.Any()) {
                return (false, "You can only upload one ID paper. Please delete the existing one first.", null);
            }
        } else if (documentType == DocumentType.DIPLOMA) {
            var diplomaCount = await _db.TeacherDocuments
                .CountAsync(d => d.TeacherId == teacherId && d.DocumentType == DocumentType.DIPLOMA, ct);
            
            if (diplomaCount >= MAX_DIPLOMAS) {
                return (false, $"Maximum {MAX_DIPLOMAS} diplomas allowed", null);
            }
        }

        // Read file content into memory
        using (var memoryStream = new MemoryStream()) {
            await file.CopyToAsync(memoryStream, ct);
            var fileContent = memoryStream.ToArray();

            // Create database record with file content
            var document = new TeacherDocument {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                DocumentType = documentType,
                FileName = file.FileName,
                FileContent = fileContent,
                Status = DocumentStatus.PENDING,
                UploadedAt = DateTime.UtcNow
            };

            _db.TeacherDocuments.Add(document);
            await _db.SaveChangesAsync(ct);

            return (true, "Document uploaded successfully", document.Id);
        }
    }

    public async Task<BatchUploadResponse> BatchUploadDocumentsAsync(
        Guid teacherId, 
        List<BatchDocumentItem> documents, 
        CancellationToken ct = default) {
        
        var response = new BatchUploadResponse();

        // Check if teacher exists
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherId, ct);
        if (teacher == null) {
            response.Results.Add(new DocumentUploadResult {
                Success = false,
                Message = "Teacher not found"
            });
            response.FailureCount = documents.Count;
            return response;
        }

        foreach (var doc in documents) {
            var (success, message, documentId) = await UploadDocumentAsync(teacherId, doc.DocumentType, doc.File, ct);
            
            response.Results.Add(new DocumentUploadResult {
                FileName = doc.File.FileName,
                DocumentType = doc.DocumentType,
                Success = success,
                Message = message,
                DocumentId = documentId
            });

            if (success) {
                response.SuccessCount++;
            } else {
                response.FailureCount++;
            }
        }

        return response;
    }

    public async Task<List<TeacherDocumentDto>> GetTeacherDocumentsAsync(Guid teacherId, CancellationToken ct = default) {
        var documents = await _db.TeacherDocuments
            .AsNoTracking()
            .Include(d => d.Teacher)
            .ThenInclude(t => t.User)
            .Where(d => d.TeacherId == teacherId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        return documents.Select(MapToDto).ToList();
    }

    public async Task<List<TeacherDocumentDto>> GetPendingDocumentsAsync(CancellationToken ct = default) {
        var documents = await _db.TeacherDocuments
            .AsNoTracking()
            .Include(d => d.Teacher)
            .ThenInclude(t => t.User)
            .Where(d => d.Status == DocumentStatus.PENDING)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync(ct);

        return documents.Select(MapToDto).ToList();
    }

    public async Task<(bool Success, string Message)> ValidateDocumentAsync(
        Guid documentId, 
        bool approve, 
        string? rejectionReason, 
        Guid adminId, 
        CancellationToken ct = default) {
        
        var document = await _db.TeacherDocuments
            .Include(d => d.Teacher)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null) {
            return (false, "Document not found");
        }

        if (document.Status != DocumentStatus.PENDING) {
            return (false, "Document has already been reviewed");
        }

        document.Status = approve ? DocumentStatus.APPROVED : DocumentStatus.REJECTED;
        document.RejectionReason = approve ? null : rejectionReason;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewedBy = adminId;

        // Update teacher verification status based on documents
        if (document.DocumentType == DocumentType.ID_PAPER && approve) {
            // Check if teacher has at least one approved diploma
            var hasApprovedDiploma = await _db.TeacherDocuments
                .AnyAsync(d => d.TeacherId == document.TeacherId 
                    && d.DocumentType == DocumentType.DIPLOMA 
                    && d.Status == DocumentStatus.APPROVED, ct);

            if (hasApprovedDiploma) {
                document.Teacher.VerificationStatus = VerificationStatus.DIPLOMA_VERIFIED;
            } else {
                document.Teacher.VerificationStatus = VerificationStatus.VERIFIED;
            }
        } else if (document.DocumentType == DocumentType.DIPLOMA && approve) {
            // Check if ID paper is approved
            var hasApprovedIdPaper = await _db.TeacherDocuments
                .AnyAsync(d => d.TeacherId == document.TeacherId 
                    && d.DocumentType == DocumentType.ID_PAPER 
                    && d.Status == DocumentStatus.APPROVED, ct);

            if (hasApprovedIdPaper) {
                document.Teacher.VerificationStatus = VerificationStatus.DIPLOMA_VERIFIED;
            }
        }

        await _db.SaveChangesAsync(ct);

        return (true, $"Document {(approve ? "approved" : "rejected")} successfully");
    }

    public async Task<(bool Success, string Message, int ProcessedCount)> BatchValidateDocumentsAsync(
        List<DocumentValidationItem> validations, 
        Guid adminId, 
        CancellationToken ct = default) {
        
        int processedCount = 0;

        foreach (var validation in validations) {
            var result = await ValidateDocumentAsync(
                validation.DocumentId, 
                validation.Approve, 
                validation.RejectionReason, 
                adminId, 
                ct);
            
            if (result.Success) {
                processedCount++;
            }
        }

        return (true, $"{processedCount} documents processed successfully", processedCount);
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid teacherId, CancellationToken ct = default) {
        var document = await _db.TeacherDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TeacherId == teacherId, ct);

        if (document == null) {
            return false;
        }

        _db.TeacherDocuments.Remove(document);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<(byte[]? FileData, string? ContentType, string? FileName)> DownloadDocumentAsync(
        Guid documentId, 
        Guid? requesterId = null, 
        bool isAdmin = false, 
        CancellationToken ct = default) {
        
        var document = await _db.TeacherDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null) {
            return (null, null, null);
        }

        // Check permissions: only the teacher or an admin can download
        if (!isAdmin && requesterId != document.TeacherId) {
            return (null, null, null);
        }

        return (document.FileContent, "application/pdf", document.FileName);
    }

    private static TeacherDocumentDto MapToDto(TeacherDocument document) {
        return new TeacherDocumentDto {
            Id = document.Id,
            TeacherId = document.TeacherId,
            TeacherFirstName = document.Teacher?.User?.FirstName,
            TeacherLastName = document.Teacher?.User?.LastName,
            DocumentType = document.DocumentType,
            FileName = document.FileName,
            Status = document.Status,
            RejectionReason = document.RejectionReason,
            UploadedAt = document.UploadedAt,
            ReviewedAt = document.ReviewedAt,
            ReviewedBy = document.ReviewedBy
        };
    }
}

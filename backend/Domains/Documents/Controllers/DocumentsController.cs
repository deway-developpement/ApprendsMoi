using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using backend.Database.Models;

namespace backend.Domains.Documents;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(DocumentService documentService) : ControllerBase {
    
    private readonly DocumentService _documentService = documentService;

    /// <summary>
    /// Batch upload multiple documents (or single document) - Teacher only
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    [TeacherOnly]
    [ProducesResponseType(typeof(BatchUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BatchUploadResponse>> UploadDocuments(
        [FromForm] IFormCollection formData,
        CancellationToken ct) {
        
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var documents = new List<BatchDocumentItem>();

        // Extract files and their corresponding document types
        var files = formData.Files;
        for (int i = 0; i < files.Count; i++) {
            var file = files[i];
            var documentTypeStr = formData[$"documentTypes[{i}]"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(documentTypeStr) || !Enum.TryParse<DocumentType>(documentTypeStr, out var documentType)) {
                continue;
            }

            documents.Add(new BatchDocumentItem {
                DocumentType = documentType,
                File = file
            });
        }

        if (documents.Count == 0) {
            return BadRequest(new { message = "No documents provided" });
        }

        var response = await _documentService.BatchUploadDocumentsAsync(userId.Value, documents, ct);
        return Ok(response);
    }

    /// <summary>
    /// Get all documents for the authenticated teacher
    /// </summary>
    [HttpGet("my-documents")]
    [Authorize]
    [TeacherOnly]
    [ProducesResponseType(typeof(List<TeacherDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TeacherDocumentDto>>> GetMyDocuments(CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var documents = await _documentService.GetTeacherDocumentsAsync(userId.Value, ct);
        return Ok(documents);
    }

    /// <summary>
    /// Get all pending documents - Admin only
    /// </summary>
    [HttpGet("pending")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(List<TeacherDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TeacherDocumentDto>>> GetPendingDocuments(CancellationToken ct) {
        var documents = await _documentService.GetPendingDocumentsAsync(ct);
        return Ok(documents);
    }

    /// <summary>
    /// Batch validate multiple documents (or single document) - Admin only
    /// </summary>
    [HttpPost("validate")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ValidateDocuments(
        [FromBody] BatchValidateDocumentsRequest request, 
        CancellationToken ct) {
        
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        // Validate rejection reasons
        foreach (var doc in request.Documents.Where(d => !d.Approve)) {
            if (string.IsNullOrWhiteSpace(doc.RejectionReason)) {
                return BadRequest(new { message = "Rejection reason is required for all rejected documents" });
            }
        }

        var (success, message, processedCount) = await _documentService.BatchValidateDocumentsAsync(
            request.Documents, 
            userId.Value, 
            ct);

        return Ok(new { message, processedCount });
    }

    /// <summary>
    /// Delete a document - Teacher only (can only delete their own documents)
    /// </summary>
    [HttpDelete("{documentId:guid}")]
    [Authorize]
    [TeacherOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteDocument(Guid documentId, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var success = await _documentService.DeleteDocumentAsync(documentId, userId.Value, ct);
        
        if (!success) {
            return NotFound(new { message = "Document not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Download a document - Teacher (own documents) or Admin
    /// </summary>
    [HttpGet("{documentId:guid}/download")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var isAdmin = JwtHelper.GetUserProfileFromClaims(User) == ProfileType.Admin;

        var (fileData, contentType, fileName) = await _documentService.DownloadDocumentAsync(
            documentId, 
            userId, 
            isAdmin, 
            ct);

        if (fileData == null) {
            return NotFound(new { message = "Document not found or access denied" });
        }

        return File(fileData, contentType!, fileName);
    }

    /// <summary>
    /// Get documents for a specific teacher - Admin only
    /// </summary>
    [HttpGet("teacher/{teacherId:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(List<TeacherDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TeacherDocumentDto>>> GetTeacherDocuments(
        Guid teacherId, 
        CancellationToken ct) {
        
        var documents = await _documentService.GetTeacherDocumentsAsync(teacherId, ct);
        return Ok(documents);
    }
}

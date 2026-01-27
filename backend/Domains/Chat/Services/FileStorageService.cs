namespace backend.Domains.Chat;

public interface IFileStorageService {
    /// <summary>
    /// Upload a file and return the URL
    /// </summary>
    Task<(string Url, string FileName, long FileSize, string FileType)> UploadFileAsync(
        IFormFile file,
        Guid chatId,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a file
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken ct = default);

    /// <summary>
    /// Get file URL
    /// </summary>
    string GetFileUrl(string fileName, Guid chatId);
}

public class LocalFileStorageService : IFileStorageService {
    private readonly string _uploadBasePath;
    private readonly long _maxFileSizeMb;
    private readonly string[] _allowedExtensions;
    private const long MB_TO_BYTES = 1024 * 1024;

    public LocalFileStorageService(IWebHostEnvironment env) {
        var config = DotNetEnv.Env.Load();
        
        _uploadBasePath = Environment.GetEnvironmentVariable("FILE_STORAGE_LOCAL_PATH") ?? "uploads/chats";
        _maxFileSizeMb = long.Parse(Environment.GetEnvironmentVariable("FILE_STORAGE_MAX_SIZE_MB") ?? "50");
        
        var extensionsStr = Environment.GetEnvironmentVariable("FILE_STORAGE_ALLOWED_EXTENSIONS") 
            ?? ".pdf,.doc,.docx,.jpg,.png,.xlsx,.txt,.zip";
        _allowedExtensions = extensionsStr.Split(',');

        // Ensure upload directory exists
        var fullPath = Path.Combine(env.WebRootPath ?? Directory.GetCurrentDirectory(), _uploadBasePath);
        if (!Directory.Exists(fullPath)) {
            Directory.CreateDirectory(fullPath);
        }
    }

    public async Task<(string Url, string FileName, long FileSize, string FileType)> UploadFileAsync(
        IFormFile file,
        Guid chatId,
        CancellationToken ct = default) {
        
        // Validate file
        ValidateFile(file);

        // Create chat-specific directory
        var chatPath = Path.Combine(_uploadBasePath, chatId.ToString());
        var fullChatPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            chatPath
        );

        if (!Directory.Exists(fullChatPath)) {
            Directory.CreateDirectory(fullChatPath);
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
        var uniqueFileName = $"{sanitizedFileName}_{Guid.NewGuid()}{fileExtension}";

        var filePath = Path.Combine(fullChatPath, uniqueFileName);

        // Save file
        await using (var stream = new FileStream(filePath, FileMode.Create)) {
            await file.CopyToAsync(stream, ct);
        }

        // Return URL and metadata
        var fileUrl = $"/{chatPath.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
        return (fileUrl, file.FileName, file.Length, file.ContentType ?? "application/octet-stream");
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken ct = default) {
        try {
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                fileUrl.TrimStart('/')
            );

            if (File.Exists(filePath)) {
                File.Delete(filePath);
                return await Task.FromResult(true);
            }

            return false;
        } catch {
            return false;
        }
    }

    public string GetFileUrl(string fileName, Guid chatId) {
        return $"/{Path.Combine(_uploadBasePath, chatId.ToString(), fileName).Replace(Path.DirectorySeparatorChar, '/')}";
    }

    private void ValidateFile(IFormFile file) {
        if (file.Length == 0) {
            throw new InvalidOperationException("File is empty");
        }

        if (file.Length > _maxFileSizeMb * MB_TO_BYTES) {
            throw new InvalidOperationException($"File exceeds maximum size of {_maxFileSizeMb}MB");
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(fileExtension)) {
            throw new InvalidOperationException($"File type {fileExtension} is not allowed");
        }
    }

    private string SanitizeFileName(string fileName) {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        
        // Limit length
        if (sanitized.Length > 100) {
            sanitized = sanitized.Substring(0, 100);
        }

        return sanitized.Trim();
    }
}

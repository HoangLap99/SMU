using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Infrastructure.Storage.Configuration;

namespace SMU.Infrastructure.Storage;

/// <summary>
/// Hiện thực hóa dịch vụ lưu trữ tệp tin trên hệ thống tệp cục bộ (Local Storage).
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IOptions<FileStorageSettings> options,
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<FileUploadResponse> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file hợp lệ để tải lên.", nameof(file));
        }

        var originalFileName = Path.GetFileName(file.FileName);
        using var stream = file.OpenReadStream();
        var response = await SaveFileAsync(stream, originalFileName, file.ContentType, cancellationToken);
        response.FileSizeBytes = file.Length;
        return response;
    }

    public async Task<FileUploadResponse> SaveFileAsync(
        Stream stream, 
        string originalFileName, 
        string? contentType = null, 
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream), "Luồng dữ liệu không được rỗng.");
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Tên tệp tin không được để trống.", nameof(originalFileName));
        }

        // 1. Tạo chuỗi GUID chuẩn 36 ký tự (format "D": 8-4-4-4-12)
        var fileGuid = Guid.NewGuid().ToString("D");
        var extension = Path.GetExtension(originalFileName);

        // 2. Tên file lưu trên đĩa: GUID kèm đuôi mở rộng (hoặc thuần GUID tùy cấu hình)
        var storedFileName = _settings.PreserveExtension && !string.IsNullOrWhiteSpace(extension)
            ? $"{fileGuid}{extension}"
            : fileGuid;

        // 3. Xác định thư mục lưu trữ (tuyệt đối hoặc tương đối theo ContentRoot)
        var configuredFolder = string.IsNullOrWhiteSpace(_settings.StorageFolder) ? "Uploads" : _settings.StorageFolder;
        var destinationFolder = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
            _logger.LogInformation("Đã khởi tạo thư mục lưu trữ: {Directory}", destinationFolder);
        }

        var targetFilePath = Path.Combine(destinationFolder, storedFileName);

        // 4. Ghi file ra ổ đĩa bất đồng bộ
        using (var destinationStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await stream.CopyToAsync(destinationStream, cancellationToken);
        }

        _logger.LogInformation("Đã lưu tệp '{OriginalFileName}' thành '{StoredFileName}' tại '{Path}'", 
            originalFileName, storedFileName, targetFilePath);

        var relativePath = Path.Combine(configuredFolder, storedFileName).Replace('\\', '/');

        return new FileUploadResponse
        {
            OriginalFileName = originalFileName,
            FileGuid = fileGuid,
            StoredFileName = storedFileName,
            StorageFolder = configuredFolder,
            RelativePath = relativePath,
            FileSizeBytes = stream.CanSeek ? stream.Length : 0,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };
    }
}

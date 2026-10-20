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
        var downloadUrl = GenerateDownloadUrl(originalFileName, targetFilePath);

        return new FileUploadResponse
        {
            OriginalFileName = originalFileName,
            FileGuid = fileGuid,
            StoredFileName = storedFileName,
            StorageFolder = configuredFolder,
            RelativePath = relativePath,
            FileSizeBytes = stream.CanSeek ? stream.Length : 0,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            DownloadUrl = downloadUrl
        };
    }

    /// <summary>
    /// Phân giải tệp tin cần tải về từ tham số filename và path (hỗ trợ cả chuỗi mã hóa Base64 và chuỗi thường).
    /// Đường dẫn path được phân giải linh hoạt dựa theo thư mục StorageFolder cấu hình trong appsettings.
    /// </summary>
    public ResolvedDownloadFileResult? ResolveFileForDownload(string? encodedFileName, string? encodedPath)
    {
        if (string.IsNullOrWhiteSpace(encodedPath))
        {
            return null;
        }

        // 1. Giải mã tên file và đường dẫn
        var decodedFileName = DecodeBase64OrRaw(encodedFileName);
        var decodedPath = DecodeBase64OrRaw(encodedPath);

        _logger.LogInformation("Yêu cầu tải file - Raw: filename='{RawFileName}', path='{RawPath}' | Decoded: filename='{DecodedFileName}', path='{DecodedPath}'",
            encodedFileName, encodedPath, decodedFileName, decodedPath);

        // 2. Xác định thư mục StorageFolder từ cấu hình
        var configuredFolder = string.IsNullOrWhiteSpace(_settings.StorageFolder) ? "Uploads" : _settings.StorageFolder;
        var storageDir = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        // 3. Tìm kiếm file theo các ứng viên
        string? targetFilePath = null;

        // Candidate 1: Đường dẫn tuyệt đối trực tiếp trên ổ đĩa
        if (!string.IsNullOrWhiteSpace(decodedPath) && File.Exists(decodedPath))
        {
            targetFilePath = Path.GetFullPath(decodedPath);
        }

        // Candidate 2: Trích xuất tên file (Path.GetFileName) và tìm trong StorageFolder
        if (targetFilePath == null && !string.IsNullOrWhiteSpace(decodedPath))
        {
            var fileNameOnly = Path.GetFileName(decodedPath.Replace('\\', '/'));
            if (!string.IsNullOrWhiteSpace(fileNameOnly))
            {
                var candidateInStorage = Path.Combine(storageDir, fileNameOnly);
                if (File.Exists(candidateInStorage))
                {
                    targetFilePath = Path.GetFullPath(candidateInStorage);
                }
            }
        }

        // Candidate 3: Đường dẫn tương đối ghép với ContentRootPath
        if (targetFilePath == null && !string.IsNullOrWhiteSpace(decodedPath))
        {
            var candidateRelativeRoot = Path.Combine(_environment.ContentRootPath, decodedPath.TrimStart('\\', '/'));
            if (File.Exists(candidateRelativeRoot))
            {
                targetFilePath = Path.GetFullPath(candidateRelativeRoot);
            }
        }

        // Candidate 4: Đường dẫn tương đối ghép với StorageFolder
        if (targetFilePath == null && !string.IsNullOrWhiteSpace(decodedPath))
        {
            var candidateRelativeStorage = Path.Combine(storageDir, decodedPath.TrimStart('\\', '/'));
            if (File.Exists(candidateRelativeStorage))
            {
                targetFilePath = Path.GetFullPath(candidateRelativeStorage);
            }
        }

        // Candidate 5: Tìm kiếm đệ quy trong thư mục StorageFolder (nếu có thư mục con)
        if (targetFilePath == null && !string.IsNullOrWhiteSpace(decodedPath) && Directory.Exists(storageDir))
        {
            var fileNameOnly = Path.GetFileName(decodedPath.Replace('\\', '/'));
            if (!string.IsNullOrWhiteSpace(fileNameOnly))
            {
                var found = Directory.EnumerateFiles(storageDir, fileNameOnly, SearchOption.AllDirectories).FirstOrDefault();
                if (found != null)
                {
                    targetFilePath = Path.GetFullPath(found);
                }
            }
        }

        // Candidate 6: Kiểm tra cả encodedPath gốc (phòng khi không phải Base64)
        if (targetFilePath == null && !string.IsNullOrWhiteSpace(encodedPath))
        {
            var rawFileName = Path.GetFileName(encodedPath.Trim().Replace('\\', '/'));
            var rawCandidate = Path.Combine(storageDir, rawFileName);
            if (File.Exists(rawCandidate))
            {
                targetFilePath = Path.GetFullPath(rawCandidate);
            }
        }

        // Candidate 7: Kiểm tra trong thư mục MauBaoCao nếu có
        if (targetFilePath == null && !string.IsNullOrWhiteSpace(decodedPath))
        {
            var fileNameOnly = Path.GetFileName(decodedPath.Replace('\\', '/'));
            var mauBaoCaoPath = Path.Combine(_environment.ContentRootPath, "MauBaoCao", fileNameOnly);
            if (File.Exists(mauBaoCaoPath))
            {
                targetFilePath = Path.GetFullPath(mauBaoCaoPath);
            }
        }

        if (targetFilePath == null || !File.Exists(targetFilePath))
        {
            _logger.LogWarning("Không tìm thấy tệp tin: decodedPath='{DecodedPath}' trong StorageFolder='{StorageDir}'", decodedPath, storageDir);
            return null;
        }

        // 4. Xác định tên tải về
        var finalDownloadName = !string.IsNullOrWhiteSpace(decodedFileName)
            ? Path.GetFileName(decodedFileName)
            : Path.GetFileName(targetFilePath);

        // Bổ sung extension nếu finalDownloadName bị thiếu extension
        var ext = Path.GetExtension(targetFilePath);
        if (string.IsNullOrEmpty(Path.GetExtension(finalDownloadName)) && !string.IsNullOrEmpty(ext))
        {
            finalDownloadName += ext;
        }

        // 5. Xác định ContentType
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(finalDownloadName, out var contentType) &&
            !provider.TryGetContentType(targetFilePath, out contentType))
        {
            contentType = "application/octet-stream";
        }

        return new ResolvedDownloadFileResult
        {
            PhysicalFilePath = targetFilePath,
            ContentType = contentType,
            DownloadFileName = finalDownloadName
        };
    }

    /// <summary>
    /// Sinh URL tải file với filename và path được mã hóa Base64 theo đúng chuẩn yêu cầu.
    /// </summary>
    public string GenerateDownloadUrl(string originalFileName, string physicalOrRelativePath)
    {
        var encodedFileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalFileName ?? string.Empty));
        var encodedPath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(physicalOrRelativePath ?? string.Empty));
        return $"/api/FileUpload/download?filename={Uri.EscapeDataString(encodedFileName)}&path={Uri.EscapeDataString(encodedPath)}";
    }

    /// <summary>
    /// Hàm hỗ trợ giải mã Base64 an toàn, tự động xử lý dấu '+' bị biến đổi thành khoảng trắng ' ' trong URL query string.
    /// </summary>
    private static string DecodeBase64OrRaw(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();

        try
        {
            var normalized = trimmed.Replace(' ', '+');
            var mod4 = normalized.Length % 4;
            if (mod4 > 0)
            {
                normalized += new string('=', 4 - mod4);
            }

            var bytes = Convert.FromBase64String(normalized);
            var decoded = System.Text.Encoding.UTF8.GetString(bytes);

            // Kiểm tra xem kết quả giải mã có hợp lệ (không chứa control chars ngoại trừ CR, LF, Tab)
            if (!string.IsNullOrWhiteSpace(decoded) && !decoded.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
            {
                return decoded;
            }
        }
        catch
        {
            // Bỏ qua lỗi format nếu không phải base64
        }

        return trimmed;
    }
}

using Microsoft.AspNetCore.Http;
using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Hợp đồng dịch vụ lưu trữ tệp tin trên máy chủ.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Lưu tệp tin được tải lên từ IFormFile vào thư mục chỉ định trong cấu hình.
    /// </summary>
    /// <param name="file">Tệp tin nhận từ client form-data.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Thông tin tệp đã lưu kèm tên gốc và tên GUID 36 ký tự.</returns>
    Task<FileUploadResponse> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu tệp tin từ Stream vào thư mục chỉ định trong cấu hình.
    /// </summary>
    /// <param name="stream">Luồng dữ liệu của tệp.</param>
    /// <param name="originalFileName">Tên file gốc.</param>
    /// <param name="contentType">Kiểu MIME của file.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Thông tin tệp đã lưu kèm tên gốc và tên GUID 36 ký tự.</returns>
    Task<FileUploadResponse> SaveFileAsync(Stream stream, string originalFileName, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân giải tệp tin cần tải về từ tham số filename và path (hỗ trợ cả chuỗi mã hóa Base64 và chuỗi thường).
    /// Đường dẫn path được phân giải linh hoạt dựa theo thư mục StorageFolder cấu hình trong appsettings.
    /// </summary>
    /// <param name="encodedFileName">Tên file tải về (mã hóa Base64 hoặc chuỗi thường).</param>
    /// <param name="encodedPath">Đường dẫn tệp tin lưu trữ (mã hóa Base64 từ đường dẫn StorageFolder của appsettings hoặc đường dẫn đĩa).</param>
    /// <returns>Thông tin tệp tin tìm thấy gồm đường dẫn vật lý, kiểu MIME và tên file tải về; hoặc null nếu không tìm thấy.</returns>
    ResolvedDownloadFileResult? ResolveFileForDownload(string? encodedFileName, string? encodedPath);

    /// <summary>
    /// Sinh URL tải file với filename và path được mã hóa Base64 theo đúng chuẩn yêu cầu.
    /// </summary>
    /// <param name="originalFileName">Tên file hiển thị.</param>
    /// <param name="physicalOrRelativePath">Đường dẫn tệp tin vật lý hoặc tương đối.</param>
    /// <returns>URL tải file dạng /api/FileUpload/download?filename=...&amp;path=...</returns>
    string GenerateDownloadUrl(string originalFileName, string physicalOrRelativePath);
}

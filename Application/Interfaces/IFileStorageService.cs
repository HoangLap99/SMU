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
}

using Microsoft.AspNetCore.Http;
using SMU.Application.DTOs;
using SMU.Infrastructure.Extractors.Abstractions;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ trích xuất tài liệu tổng thể (Strategy Resolver/Facade).
/// </summary>
public interface IDocumentExtractionService
{
    /// <summary>
    /// Kiểm tra xem tệp có nằm trong danh sách định dạng hỗ trợ không.
    /// </summary>
    bool IsSupported(string fileName);

    /// <summary>
    /// Lấy danh sách thông tin các định dạng tệp được hỗ trợ.
    /// </summary>
    IReadOnlyList<SupportedFormatItemDto> GetSupportedFormats();

    /// <summary>
    /// Trích xuất tài liệu từ IFormFile tải lên.
    /// </summary>
    Task<ExtractedDocument> ExtractAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trích xuất tài liệu từ Stream và tên file.
    /// </summary>
    Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}

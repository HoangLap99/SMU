namespace SMU.Infrastructure.Extractors.Abstractions;

/// <summary>
/// Hợp đồng Strategy cho từng định dạng tệp (Excel, Word, PDF, Text...).
/// Tuân thủ nguyên lý Open/Closed (OCP) và Liskov Substitution (LSP).
/// </summary>
public interface IDocumentExtractorStrategy
{
    /// <summary>
    /// Danh sách các phần mở rộng mà Strategy hỗ trợ (ví dụ: .xlsx, .xls).
    /// </summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }

    /// <summary>
    /// Tên đại diện cho loại tệp (ví dụ: Microsoft Excel).
    /// </summary>
    string DisplayType { get; }

    /// <summary>
    /// Mô tả tóm lược về năng lực trích xuất của Strategy.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Kiểm tra chiến lược có xử lý được phần mở rộng tệp này hay không.
    /// </summary>
    bool CanHandle(string extension);

    /// <summary>
    /// Thực hiện trích xuất dữ liệu từ Stream.
    /// </summary>
    Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}

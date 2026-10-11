namespace SMU.Infrastructure.Extractors.Abstractions;

/// <summary>
/// Kết quả sau khi trích xuất tài liệu từ Strategy.
/// </summary>
public record ExtractedDocument
{
    /// <summary>
    /// Nội dung văn bản hoặc Markdown đã trích xuất.
    /// </summary>
    public string TextContent { get; init; } = string.Empty;

    /// <summary>
    /// Đoạn trích dẫn ngắn (preview) làm tóm tắt hiển thị nhanh.
    /// </summary>
    public string PreviewText { get; init; } = string.Empty;

    /// <summary>
    /// Dữ liệu thô của file (nếu là dạng multimodal như PDF/ảnh).
    /// </summary>
    public byte[]? RawBytes { get; init; }

    /// <summary>
    /// Kiểu MIME của tệp (ví dụ: application/pdf).
    /// </summary>
    public string MimeType { get; init; } = "text/plain";

    /// <summary>
    /// Đánh dấu tài liệu có thể gửi trực tiếp ở dạng binary/multimodal lên mô hình AI hay không.
    /// </summary>
    public bool IsMultimodalPreferred { get; init; }

    public static ExtractedDocument FromText(string text, int previewLimit = 800)
    {
        var preview = string.IsNullOrWhiteSpace(text) 
            ? string.Empty 
            : (text.Length <= previewLimit ? text.Trim() : text.Substring(0, previewLimit).Trim() + "\n... (nội dung còn tiếp) ...");

        return new ExtractedDocument
        {
            TextContent = text,
            PreviewText = preview,
            IsMultimodalPreferred = false,
            MimeType = "text/plain"
        };
    }
}

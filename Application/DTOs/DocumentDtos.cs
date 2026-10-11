using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SMU.Application.DTOs;

/// <summary>
/// Yêu cầu phân tích tài liệu gửi lên từ phía client.
/// </summary>
public class DocumentAnalyzeRequest
{
    [Required(ErrorMessage = "Vui lòng chọn file tải lên.")]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Câu hỏi hoặc yêu cầu cụ thể đối với tài liệu (ví dụ: Tóm tắt nội dung, Trích xuất danh sách công nợ, Tìm người có điểm cao nhất...)
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Chỉ thị hệ thống tùy chọn để định hình văn phong phản hồi (vd: Trả về bảng Markdown, Trả lời bằng tiếng Việt chuyên nghiệp...)
    /// </summary>
    public string? SystemInstruction { get; set; }
}

/// <summary>
/// Kết quả phân tích tài liệu trả về cho client.
/// </summary>
public class DocumentAnalyzeResponse
{
    public bool Success { get; set; } = true;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string PromptUsed { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string? ExtractedTextPreview { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Kết quả trích xuất văn bản thuần hoặc bảng Markdown từ tài liệu.
/// </summary>
public class ExtractTextResponse
{
    public bool Success { get; set; } = true;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int CharacterCount { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Thông tin định dạng tệp được hệ thống hỗ trợ.
/// </summary>
public record SupportedFormatItemDto(string Extension, string Type, string Description);

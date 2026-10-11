using System.ComponentModel.DataAnnotations;

namespace SMU.Infrastructure.Ai.Configuration;

/// <summary>
/// Cấu hình kết nối tới Google Gemini AI.
/// Hỗ trợ Options Pattern với xác thực tự động lúc khởi động.
/// </summary>
public class GeminiSettings
{
    public const string SectionName = "Gemini";

    [Required(ErrorMessage = "Gemini API Key là bắt buộc.")]
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-1.5-flash";

    [Url(ErrorMessage = "BaseUrl phải là định dạng URL hợp lệ.")]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

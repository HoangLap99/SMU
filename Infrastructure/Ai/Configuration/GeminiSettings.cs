namespace SMU.Infrastructure.Ai.Configuration;

/// <summary>
/// Cấu hình kết nối tới Google Gemini AI.
/// Các giá trị có thể để rỗng khi không sử dụng tính năng Gemini AI.
/// </summary>
public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    private string _model = "gemini-1.5-flash";
    public string Model
    {
        get => string.IsNullOrWhiteSpace(_model) ? "gemini-1.5-flash" : _model;
        set => _model = value;
    }

    private string _baseUrl = "https://generativelanguage.googleapis.com/v1beta";
    public string BaseUrl
    {
        get => string.IsNullOrWhiteSpace(_baseUrl) ? "https://generativelanguage.googleapis.com/v1beta" : _baseUrl;
        set => _baseUrl = value;
    }
}

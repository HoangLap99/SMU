namespace SMU.Application.Interfaces;

/// <summary>
/// Trừu tượng hóa dịch vụ AI (Gemini, OpenAI, Claude...), tuân thủ Dependency Inversion (DIP).
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Gửi yêu cầu phân tích kèm ngữ cảnh văn bản tới AI model.
    /// </summary>
    Task<string> GenerateContentFromTextAsync(
        string prompt, 
        string context, 
        string? systemInstruction = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi file binary trực tiếp dạng Multimodal (PDF, Audio, Image) tới AI model.
    /// </summary>
    Task<string> GenerateContentFromBytesAsync(
        string prompt, 
        byte[] fileBytes, 
        string mimeType, 
        string? systemInstruction = null, 
        CancellationToken cancellationToken = default);
}

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMU.Application.Interfaces;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Ai.Configuration;
using SMU.Infrastructure.Ai.Models;

namespace SMU.Infrastructure.Ai;

/// <summary>
/// Triển khai dịch vụ AI tích hợp Google Gemini REST API.
/// </summary>
public class GeminiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;

    private const string DefaultSystemInstruction = 
        "Bạn là một trợ lý AI thông minh chuyên phân tích và đọc hiểu các tài liệu văn phòng như Excel, Word, PDF. " +
        "Hãy dựa vào dữ liệu tài liệu được cung cấp để trả lời chính xác, trung thực và rõ ràng bằng tiếng Việt. " +
        "Nếu dữ liệu là bảng tính, hãy phân tích xu hướng, tổng hợp số liệu hoặc trích xuất đúng các trường thông tin được yêu cầu. " +
        "Trình bày câu trả lời dưới dạng Markdown đẹp mắt.";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiService(
        HttpClient httpClient, 
        IOptions<GeminiSettings> options, 
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateContentFromTextAsync(
        string prompt, 
        string context, 
        string? systemInstruction = null, 
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var instruction = string.IsNullOrWhiteSpace(systemInstruction) ? DefaultSystemInstruction : systemInstruction;
        var fullUserMessage = $"Nội dung tài liệu:\n```\n{context}\n```\n\nYêu cầu phân tích:\n{prompt}";

        var requestBody = new GeminiGenerateRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new() { Text = instruction }]
            },
            Contents =
            [
                new()
                {
                    Role = "user",
                    Parts = [new() { Text = fullUserMessage }]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.2
            }
        };

        return await SendGeminiRequestAsync(requestBody, cancellationToken);
    }

    public async Task<string> GenerateContentFromBytesAsync(
        string prompt, 
        byte[] fileBytes, 
        string mimeType, 
        string? systemInstruction = null, 
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var instruction = string.IsNullOrWhiteSpace(systemInstruction) ? DefaultSystemInstruction : systemInstruction;
        var base64Data = Convert.ToBase64String(fileBytes);

        var requestBody = new GeminiGenerateRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new() { Text = instruction }]
            },
            Contents =
            [
                new()
                {
                    Role = "user",
                    Parts =
                    [
                        new() { Text = prompt },
                        new()
                        {
                            InlineData = new GeminiInlineData
                            {
                                MimeType = mimeType,
                                Data = base64Data
                            }
                        }
                    ]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.2
            }
        };

        return await SendGeminiRequestAsync(requestBody, cancellationToken);
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || 
            _settings.ApiKey.Equals("YOUR_GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase) ||
            _settings.ApiKey.Contains("YOUR_"))
        {
            throw new InvalidOperationException(
                "Gemini API Key chưa được cấu hình. Vui lòng cập nhật API Key vào 'appsettings.json' (mục Gemini:ApiKey) " +
                "hoặc thông qua biến môi trường 'GEMINI__APIKEY'.");
        }
    }

    private async Task<string> SendGeminiRequestAsync(GeminiGenerateRequest requestBody, CancellationToken cancellationToken)
    {
        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-1.5-flash" : _settings.Model;
        var baseUrl = (string.IsNullOrWhiteSpace(_settings.BaseUrl) ? "https://generativelanguage.googleapis.com/v1beta" : _settings.BaseUrl).TrimEnd('/');
        var url = $"{baseUrl}/models/{model}:generateContent?key={_settings.ApiKey}";

        var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        _logger.LogInformation("Đang gửi yêu cầu tới Gemini AI model: {Model}", model);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API trả về lỗi HTTP {StatusCode}: {Response}", response.StatusCode, responseString);
            
            try
            {
                var errorObj = JsonSerializer.Deserialize<GeminiGenerateResponse>(responseString, _jsonOptions);
                if (errorObj?.Error != null && !string.IsNullOrWhiteSpace(errorObj.Error.Message))
                {
                    throw new AiServiceException($"Gemini API Error ({errorObj.Error.Code}): {errorObj.Error.Message}", response.StatusCode);
                }
            }
            catch (JsonException)
            {
                // Fallback nếu không parse được JSON lỗi
            }

            throw new AiServiceException($"Lỗi gọi Gemini API (HTTP {(int)response.StatusCode}): {responseString}", response.StatusCode);
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateResponse>(responseString, _jsonOptions);
        var candidateText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(candidateText))
        {
            return "Gemini AI không trả về nội dung hoặc nội dung bị chặn bởi bộ lọc an toàn.";
        }

        return candidateText;
    }
}

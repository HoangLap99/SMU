using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Application.Services;

/// <summary>
/// Nhạc trưởng điều phối nghiệp vụ (Orchestrator):
/// Kết nối luồng giữa bóc tách file (Extraction Service) và suy luận AI (AI Service).
/// Tuân thủ nguyên tắc Single Responsibility (SRP) và High Cohesion.
/// </summary>
public class DocumentAnalysisOrchestrator : IDocumentAnalysisOrchestrator
{
    private readonly IDocumentExtractionService _extractionService;
    private readonly IAiService _aiService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentAnalysisOrchestrator> _logger;

    private const string DefaultPrompt = "Hãy phân tích nội dung tài liệu này, tóm tắt các điểm chính và trích xuất các thông tin số liệu quan trọng.";

    public DocumentAnalysisOrchestrator(
        IDocumentExtractionService extractionService,
        IAiService aiService,
        ICurrentUserService currentUserService,
        ILogger<DocumentAnalysisOrchestrator> logger)
    {
        _extractionService = extractionService;
        _aiService = aiService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DocumentAnalyzeResponse> AnalyzeDocumentAsync(DocumentAnalyzeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file hợp lệ để tải lên.", nameof(request.File));
        }

        var fileName = request.File.FileName;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var prompt = string.IsNullOrWhiteSpace(request.Prompt) ? DefaultPrompt : request.Prompt.Trim();

        _logger.LogInformation("Người dùng '{Username}' ({UserId}) bắt đầu quy trình phân tích tệp: {FileName} ({Bytes} bytes)", 
            _currentUserService.Username ?? "N/A", _currentUserService.UserId ?? "N/A", fileName, request.File.Length);

        // 1. Trích xuất tài liệu thông qua Extraction Service
        var extractedDoc = await _extractionService.ExtractAsync(request.File, cancellationToken);

        // 2. Gửi dữ liệu tới AI Provider (ưu tiên Multimodal nếu Strategy yêu cầu và có raw bytes)
        string aiResult;
        if (extractedDoc.IsMultimodalPreferred && extractedDoc.RawBytes != null && extractedDoc.RawBytes.Length > 0)
        {
            _logger.LogInformation("Gửi tài liệu dạng Multimodal ({MimeType}) tới AI Model", extractedDoc.MimeType);
            aiResult = await _aiService.GenerateContentFromBytesAsync(
                prompt,
                extractedDoc.RawBytes,
                extractedDoc.MimeType,
                request.SystemInstruction,
                cancellationToken);
        }
        else
        {
            _logger.LogInformation("Gửi tài liệu dạng Text Context ({Length} chars) tới AI Model", extractedDoc.TextContent.Length);
            aiResult = await _aiService.GenerateContentFromTextAsync(
                prompt,
                extractedDoc.TextContent,
                request.SystemInstruction,
                cancellationToken);
        }

        return new DocumentAnalyzeResponse
        {
            Success = true,
            FileName = fileName,
            FileExtension = ext,
            FileSizeBytes = request.File.Length,
            PromptUsed = prompt,
            Analysis = aiResult,
            ExtractedTextPreview = extractedDoc.PreviewText
        };
    }

    public async Task<ExtractTextResponse> ExtractTextOnlyAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file hợp lệ để tải lên.", nameof(file));
        }

        var fileName = file.FileName;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        _logger.LogInformation("Bắt đầu trích xuất text thuần cho tệp: {FileName}", fileName);
        var extractedDoc = await _extractionService.ExtractAsync(file, cancellationToken);

        return new ExtractTextResponse
        {
            Success = true,
            FileName = fileName,
            FileExtension = ext,
            FileSizeBytes = file.Length,
            CharacterCount = extractedDoc.TextContent.Length,
            ExtractedText = extractedDoc.TextContent
        };
    }
}

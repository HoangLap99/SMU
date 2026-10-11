using System.Text;
using Microsoft.Extensions.Logging;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Extractors.Abstractions;

namespace SMU.Infrastructure.Extractors.Implementations;

/// <summary>
/// Chiến lược trích xuất dữ liệu văn bản thuần (.txt, .csv, .json, .md).
/// </summary>
public class PlainTextExtractorStrategy : IDocumentExtractorStrategy
{
    private readonly ILogger<PlainTextExtractorStrategy> _logger;

    private static readonly string[] _extensions = [".txt", ".csv", ".json", ".md"];

    public IReadOnlyCollection<string> SupportedExtensions => _extensions;
    public string DisplayType => "Text & Data";
    public string Description => "Đọc dữ liệu văn bản thuần, CSV, JSON và Markdown";

    public PlainTextExtractorStrategy(ILogger<PlainTextExtractorStrategy> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string extension)
    {
        return _extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = await reader.ReadToEndAsync(cancellationToken);
            return ExtractedDocument.FromText(content);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trích xuất tệp văn bản thuần: {FileName}", fileName);
            throw new DocumentExtractionException($"Không thể đọc nội dung file văn bản '{fileName}': {ex.Message}", ex);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Extractors.Abstractions;

namespace SMU.Infrastructure.Extractors;

/// <summary>
/// Dịch vụ điều phối và giải quyết (Resolver) chiến lược trích xuất tài liệu tương ứng theo định dạng tệp.
/// Tuân thủ nguyên tắc Dependency Inversion (DIP) và Open/Closed Principle (OCP).
/// </summary>
public class DocumentExtractionService : IDocumentExtractionService
{
    private readonly IEnumerable<IDocumentExtractorStrategy> _strategies;
    private readonly ILogger<DocumentExtractionService> _logger;

    public DocumentExtractionService(
        IEnumerable<IDocumentExtractorStrategy> strategies,
        ILogger<DocumentExtractionService> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

    public bool IsSupported(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && _strategies.Any(s => s.CanHandle(ext));
    }

    public IReadOnlyList<SupportedFormatItemDto> GetSupportedFormats()
    {
        return _strategies
            .Select(s => new SupportedFormatItemDto(
                string.Join(" / ", s.SupportedExtensions),
                s.DisplayType,
                s.Description))
            .ToList();
    }

    public async Task<ExtractedDocument> ExtractAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file hợp lệ để tải lên.", nameof(file));
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return await ExtractAsync(memoryStream, file.FileName, cancellationToken);
    }

    public async Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Tên tệp không được để trống.", nameof(fileName));
        }

        var ext = Path.GetExtension(fileName);
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(ext));

        if (strategy == null)
        {
            var supported = _strategies.SelectMany(s => s.SupportedExtensions).Distinct();
            throw new UnsupportedFileTypeException(ext, supported);
        }

        _logger.LogInformation("Sử dụng chiến lược '{StrategyType}' để xử lý tệp '{FileName}'", strategy.GetType().Name, fileName);
        return await strategy.ExtractAsync(stream, fileName, cancellationToken);
    }
}

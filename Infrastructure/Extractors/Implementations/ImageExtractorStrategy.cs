using Microsoft.Extensions.Logging;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Extractors.Abstractions;

namespace SMU.Infrastructure.Extractors.Implementations;

/// <summary>
/// Chiến lược trích xuất tệp tin hình ảnh (PNG, JPG, JPEG, WEBP, BMP, GIF) phục vụ phân tích thị giác AI Multimodal.
/// </summary>
public class ImageExtractorStrategy : IDocumentExtractorStrategy
{
    private readonly ILogger<ImageExtractorStrategy> _logger;

    private static readonly string[] _extensions = [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif"];

    public IReadOnlyCollection<string> SupportedExtensions => _extensions;
    public string DisplayType => "Hình ảnh (Image / OCR)";
    public string Description => "Hỗ trợ ảnh PNG, JPG, JPEG, WEBP, BMP cho phân tích thị giác đa phương thức AI (Multimodal OCR)";

    public ImageExtractorStrategy(ILogger<ImageExtractorStrategy> logger)
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
            byte[] rawBytes;
            if (stream is MemoryStream ms)
            {
                rawBytes = ms.ToArray();
            }
            else
            {
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);
                rawBytes = buffer.ToArray();
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                _ => "image/png"
            };

            return new ExtractedDocument
            {
                TextContent = $"[Hình ảnh tài liệu: {fileName} ({rawBytes.Length:N0} bytes)]",
                PreviewText = $"[Hình ảnh {mimeType}: {rawBytes.Length:N0} bytes]",
                RawBytes = rawBytes,
                MimeType = mimeType,
                IsMultimodalPreferred = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nạp tệp hình ảnh: {FileName}", fileName);
            throw new DocumentExtractionException($"Không thể nạp file hình ảnh '{fileName}': {ex.Message}", ex);
        }
    }
}

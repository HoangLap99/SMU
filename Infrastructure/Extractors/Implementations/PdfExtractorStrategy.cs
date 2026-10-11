using System.Text;
using Microsoft.Extensions.Logging;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Extractors.Abstractions;
using UglyToad.PdfPig;

namespace SMU.Infrastructure.Extractors.Implementations;

/// <summary>
/// Chiến lược trích xuất tài liệu PDF bằng thư viện PdfPig, hỗ trợ song song Multimodal Binary.
/// </summary>
public class PdfExtractorStrategy : IDocumentExtractorStrategy
{
    private readonly ILogger<PdfExtractorStrategy> _logger;

    private static readonly string[] _extensions = [".pdf"];

    public IReadOnlyCollection<string> SupportedExtensions => _extensions;
    public string DisplayType => "PDF Document";
    public string Description => "Đọc nội dung văn bản từng trang hoặc phân tích trực tiếp dạng Multimodal";

    public PdfExtractorStrategy(ILogger<PdfExtractorStrategy> logger)
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

            var textBuilder = new StringBuilder();

            try
            {
                using var pdfStream = new MemoryStream(rawBytes);
                using var document = PdfDocument.Open(pdfStream);

                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    textBuilder.AppendLine($"--- Trang {page.Number} ---");
                    textBuilder.AppendLine(page.Text);
                    textBuilder.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể trích xuất văn bản từ PDF '{FileName}', sẽ sử dụng fallback Multimodal.", fileName);
            }

            var extractedText = textBuilder.ToString();
            var preview = string.IsNullOrWhiteSpace(extractedText) 
                ? "[PDF Multimodal Stream]" 
                : (extractedText.Length <= 800 ? extractedText.Trim() : extractedText.Substring(0, 800).Trim() + "\n... (nội dung còn tiếp) ...");

            return new ExtractedDocument
            {
                TextContent = extractedText,
                PreviewText = preview,
                RawBytes = rawBytes,
                MimeType = "application/pdf",
                IsMultimodalPreferred = true
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trích xuất file PDF: {FileName}", fileName);
            throw new DocumentExtractionException($"Không thể đọc nội dung file PDF '{fileName}': {ex.Message}", ex);
        }
    }
}

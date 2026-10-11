using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Extractors.Abstractions;

namespace SMU.Infrastructure.Extractors.Implementations;

/// <summary>
/// Chiến lược trích xuất tài liệu Microsoft Word (.docx).
/// </summary>
public class WordExtractorStrategy : IDocumentExtractorStrategy
{
    private readonly ILogger<WordExtractorStrategy> _logger;

    private static readonly string[] _extensions = [".docx"];

    public IReadOnlyCollection<string> SupportedExtensions => _extensions;
    public string DisplayType => "Microsoft Word";
    public string Description => "Đọc các đoạn văn, tiêu đề và bảng biểu";

    public WordExtractorStrategy(ILogger<WordExtractorStrategy> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string extension)
    {
        return _extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder();
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                return Task.FromResult(ExtractedDocument.FromText("*(Tài liệu Word không có nội dung)*"));
            }

            foreach (var element in body.Elements())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (element is Paragraph paragraph)
                {
                    var text = paragraph.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        sb.AppendLine(text);
                    }
                }
                else if (element is Table table)
                {
                    var rows = table.Elements<TableRow>().ToList();
                    if (rows.Count > 0)
                    {
                        sb.AppendLine();
                        bool isFirst = true;

                        foreach (var row in rows)
                        {
                            var cells = row.Elements<TableCell>()
                                .Select(c => (c.InnerText ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("|", "/").Trim())
                                .ToList();

                            sb.AppendLine("| " + string.Join(" | ", cells) + " |");

                            if (isFirst)
                            {
                                sb.AppendLine("| " + string.Join(" | ", cells.Select(_ => "---")) + " |");
                                isFirst = false;
                            }
                        }

                        sb.AppendLine();
                    }
                }
            }

            var textResult = sb.ToString();
            return Task.FromResult(ExtractedDocument.FromText(textResult));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trích xuất tệp Word: {FileName}", fileName);
            throw new DocumentExtractionException($"Không thể đọc nội dung file Word '{fileName}': {ex.Message}", ex);
        }
    }
}

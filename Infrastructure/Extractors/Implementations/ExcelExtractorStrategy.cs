using System.Text;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using SMU.Core.Exceptions;
using SMU.Infrastructure.Extractors.Abstractions;

namespace SMU.Infrastructure.Extractors.Implementations;

/// <summary>
/// Chiến lược trích xuất bảng tính Excel (.xlsx, .xls) thành định dạng Markdown tables.
/// </summary>
public class ExcelExtractorStrategy : IDocumentExtractorStrategy
{
    private readonly ILogger<ExcelExtractorStrategy> _logger;

    private static readonly string[] _extensions = [".xlsx", ".xls"];

    public IReadOnlyCollection<string> SupportedExtensions => _extensions;
    public string DisplayType => "Microsoft Excel";
    public string Description => "Đọc tất cả sheet, trích xuất cấu trúc bảng Markdown";

    public ExcelExtractorStrategy(ILogger<ExcelExtractorStrategy> logger)
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
            using var workbook = new XLWorkbook(stream);

            foreach (var worksheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sb.AppendLine($"### Trang tính: {worksheet.Name}");

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    sb.AppendLine("*(Trang tính trống)*\n");
                    continue;
                }

                var rowCount = range.RowCount();
                var colCount = range.ColumnCount();

                // Đọc dòng tiêu đề (Header)
                var headerCells = new List<string>();
                for (int c = 1; c <= colCount; c++)
                {
                    var val = range.Cell(1, c).GetString().Replace("\r", " ").Replace("\n", " ").Replace("|", "/").Trim();
                    headerCells.Add(string.IsNullOrWhiteSpace(val) ? $"Cột {c}" : val);
                }

                sb.AppendLine("| " + string.Join(" | ", headerCells) + " |");
                sb.AppendLine("| " + string.Join(" | ", headerCells.Select(_ => "---")) + " |");

                // Đọc dữ liệu các dòng tiếp theo
                for (int r = 2; r <= rowCount; r++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rowCells = new List<string>();
                    for (int c = 1; c <= colCount; c++)
                    {
                        var cellVal = range.Cell(r, c).GetFormattedString().Replace("\r", " ").Replace("\n", " ").Replace("|", "/").Trim();
                        rowCells.Add(cellVal);
                    }

                    if (rowCells.Any(c => !string.IsNullOrEmpty(c)))
                    {
                        sb.AppendLine("| " + string.Join(" | ", rowCells) + " |");
                    }
                }

                sb.AppendLine();
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
            _logger.LogError(ex, "Lỗi trích xuất tệp Excel: {FileName}", fileName);
            throw new DocumentExtractionException($"Không thể đọc nội dung file Excel '{fileName}': {ex.Message}", ex);
        }
    }
}

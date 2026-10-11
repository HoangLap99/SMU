using Microsoft.AspNetCore.Http;
using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện điều phối toàn bộ nghiệp vụ phân tích tài liệu thông minh.
/// Đóng gói workflow giữa Document Extractor và AI Service.
/// </summary>
public interface IDocumentAnalysisOrchestrator
{
    /// <summary>
    /// Thực hiện bóc tách nội dung tài liệu và phân tích qua mô hình AI.
    /// </summary>
    Task<DocumentAnalyzeResponse> AnalyzeDocumentAsync(DocumentAnalyzeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chỉ bóc tách nội dung văn bản thuần hoặc bảng Markdown từ tài liệu mà không gọi AI.
    /// </summary>
    Task<ExtractTextResponse> ExtractTextOnlyAsync(IFormFile file, CancellationToken cancellationToken = default);
}

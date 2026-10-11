using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller cung cấp các API phân tích tài liệu thông minh sử dụng Gemini AI.
/// Yêu cầu xác thực JWT Authorization cho mọi thao tác.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentAiController : ControllerBase
{
    private readonly IDocumentAnalysisOrchestrator _orchestrator;
    private readonly IDocumentExtractionService _extractionService;
    private readonly ICurrentUserService _currentUserService;

    public DocumentAiController(
        IDocumentAnalysisOrchestrator orchestrator,
        IDocumentExtractionService extractionService,
        ICurrentUserService currentUserService)
    {
        _orchestrator = orchestrator;
        _extractionService = extractionService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Danh sách các định dạng file được hệ thống hỗ trợ trích xuất và phân tích.
    /// Dữ liệu được truy vấn động từ các Strategy đã đăng ký vào hệ thống (OCP).
    /// </summary>
    [HttpGet("supported-formats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetSupportedFormats()
    {
        var formats = _extractionService.GetSupportedFormats();
        return Ok(new { supportedFormats = formats });
    }

    /// <summary>
    /// Tải lên file Excel, Word, PDF hoặc Text để Gemini AI phân tích, tóm tắt hoặc trích xuất số liệu.
    /// </summary>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentAnalyzeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentAnalyzeResponse>> AnalyzeDocument(
        [FromForm] DocumentAnalyzeRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.AnalyzeDocumentAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bóc tách nội dung văn bản thuần hoặc bảng Markdown từ tài liệu mà không gọi AI (phục vụ kiểm tra trước dữ liệu).
    /// </summary>
    [HttpPost("extract-text")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ExtractTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExtractTextResponse>> ExtractTextOnly(
        IFormFile file, 
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ExtractTextOnlyAsync(file, cancellationToken);
        return Ok(result);
    }
}

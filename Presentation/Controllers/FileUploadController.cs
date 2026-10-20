using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller xử lý tải tệp tin từ FE lên máy chủ và lưu trữ với tên mã GUID.
/// Yêu cầu xác thực JWT Authorization cho mọi thao tác tải tệp.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FileUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    public FileUploadController(
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Đẩy 1 tệp tin bất kỳ từ Frontend lên máy chủ.
    /// File sẽ được lưu vào thư mục được chỉ định trong cấu hình appsettings và đặt tên theo mã GUID 36 ký tự.
    /// </summary>
    /// <param name="file">Tệp tin bất kỳ tải lên từ form-data.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Tên file gốc và tên file GUID 36 ký tự đã lưu trong thư mục cấu hình.</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        IFormFile file, 
        CancellationToken cancellationToken)
    {
        var result = await _fileStorageService.SaveFileAsync(file, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API hỗ trợ tải tệp tin từ máy chủ với Input theo dạng query string: ?filename=...&amp;path=...
    /// Trong đó:
    /// - filename: Tên tệp tin tải về (mã hóa Base64 hoặc chuỗi thuần, ví dụ: QmFvQ2FvLnhsc3g= -> BaoCao.xlsx).
    /// - path: Đường dẫn tệp tin lưu trữ (được mã hóa Base64 từ đường dẫn StorageFolder của appsettings hoặc đường dẫn tuyệt đối/tương đối).
    /// Hỗ trợ [AllowAnonymous] để trình duyệt tải trực tiếp qua thẻ &lt;a href&gt; hoặc window.open mà không bị chặn token.
    /// </summary>
    /// <param name="filename">Tên tệp tin hiển thị khi tải về (mã hóa Base64).</param>
    /// <param name="path">Đường dẫn tệp tin trên máy chủ (mã hóa Base64).</param>
    /// <returns>Tệp tin dạng FileResult với Content-Type phù hợp và hỗ trợ resume download (Range processing).</returns>
    [AllowAnonymous]
    [HttpGet("download")]
    [HttpGet("get-file")]
    [HttpGet]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult DownloadFile(
        [FromQuery] string? filename,
        [FromQuery] string? path)
    {
        // Hỗ trợ fallback các biến thể đặt tên query param
        var targetPath = !string.IsNullOrWhiteSpace(path)
            ? path
            : Request.Query["filePath"].FirstOrDefault() ?? Request.Query["file_path"].FirstOrDefault();

        var targetFileName = !string.IsNullOrWhiteSpace(filename)
            ? filename
            : Request.Query["fileName"].FirstOrDefault() ?? Request.Query["file_name"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Thiếu tham số bắt buộc",
                Detail = "Vui lòng cung cấp tham số 'path' (đường dẫn tệp tin mã hóa Base64 từ StorageFolder của appsettings)."
            });
        }

        var resolved = _fileStorageService.ResolveFileForDownload(targetFileName, targetPath);
        if (resolved == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Không tìm thấy tệp tin",
                Detail = "Tệp tin yêu cầu không tồn tại trên hệ thống lưu trữ hoặc đường dẫn không hợp lệ."
            });
        }

        return PhysicalFile(resolved.PhysicalFilePath, resolved.ContentType, resolved.DownloadFileName, enableRangeProcessing: true);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller cung cấp API sao lưu toàn bộ dữ liệu cơ sở dữ liệu PostgreSQL vào thư mục cấu hình trong appsettings.json.
/// Yêu cầu xác thực JWT Authorization.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// Thực hiện sao lưu toàn bộ dữ liệu cơ sở dữ liệu PostgreSQL và lưu tệp vào thư mục được cấu hình trong appsettings.
    /// Tệp được lưu dưới định dạng SQL dump (.sql) hoặc nén ZIP (.zip).
    /// </summary>
    /// <param name="request">Tùy chọn sao lưu (ghi chú, chỉ định nén zip).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Thông tin chi tiết kết quả sao lưu gồm tên file, đường dẫn lưu, kích thước và thống kê các bảng.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BackupResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BackupResultDto>> CreateBackup(
        [FromBody] BackupRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _backupService.CreateBackupAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các tệp sao lưu hiện có trong thư mục cấu hình trên máy chủ.
    /// </summary>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Danh sách các tệp sao lưu kèm dung lượng và thời gian tạo.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BackupFileInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<BackupFileInfo>>> GetBackupFiles(CancellationToken cancellationToken)
    {
        var files = await _backupService.GetBackupFilesAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Tải về tệp tin sao lưu cơ sở dữ liệu từ thư mục cấu hình.
    /// </summary>
    /// <param name="fileName">Tên tệp tin cần tải về (ví dụ: SMU_Backup_20261018_142500.sql hoặc .zip).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Tệp tin dạng FileResult hỗ trợ resume download.</returns>
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadBackupFile(
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var (fileStream, contentType, safeFileName) = await _backupService.DownloadBackupFileAsync(fileName, cancellationToken);
            return File(fileStream, contentType, safeFileName, enableRangeProcessing: true);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy tệp sao lưu",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tên tệp tin không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Xóa một tệp sao lưu khỏi thư mục cấu hình trên máy chủ.
    /// </summary>
    /// <param name="fileName">Tên tệp tin cần xóa.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Thông báo trạng thái xóa tệp tin.</returns>
    [HttpDelete("{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteBackupFile(
        string fileName,
        CancellationToken cancellationToken)
    {
        var deleted = await _backupService.DeleteBackupFileAsync(fileName, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy tệp sao lưu để xóa",
                Detail = $"Không tìm thấy tệp tin '{fileName}' trong thư mục sao lưu."
            });
        }

        return Ok(new
        {
            Success = true,
            Message = $"Đã xóa thành công tệp sao lưu '{fileName}'."
        });
    }
}

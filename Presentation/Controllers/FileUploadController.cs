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
}

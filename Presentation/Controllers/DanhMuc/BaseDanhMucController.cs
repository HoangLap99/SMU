using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller cơ sở cung cấp các API chuẩn cho từng danh mục:
/// 1. GET: Lấy toàn bộ danh sách không phân trang theo CustomerId (với GoiThueBao không cần truyền).
/// 2. POST: Lấy danh sách phân trang (KeyWord, Customer của user đang đăng nhập, CurrentPage, PageSize) -> Sắp xếp ThuTu, Ma.
/// 3. POST set: Set (Vừa thêm mới vừa cập nhật bản ghi).
/// 4. POST delete: Delete theo listId danh mục.
/// 5. POST import: Import Excel từ FileName (EPPlus 6.0.8).
/// 6. POST export: Export Excel nhiều dữ liệu (EPPlus 6.0.8).
/// </summary>
[Authorize]
[ApiController]
[Produces("application/json")]
public abstract class BaseDanhMucController<TService, TResponse, TSetRequest> : ControllerBase
    where TService : IBaseDanhMucService<TResponse, TSetRequest>
    where TResponse : BaseDanhMucResponse
    where TSetRequest : BaseSetDanhMucRequest
{
    protected readonly TService Service;

    protected BaseDanhMucController(TService service)
    {
        Service = service;
    }

    /// <summary>
    /// API: Lấy toàn bộ danh sách không phân trang, chỉ lọc theo CustomerId (với danh mục GoiThueBao không cần truyền).
    /// Output được sắp xếp tự động theo Thứ tự (ThuTu), sau đó đến Mã (Ma).
    /// </summary>
    /// <param name="customerId">Mã định danh khách hàng / đơn vị cần lấy danh mục (với GoiThueBao không cần truyền).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<ActionResult<IReadOnlyList<TResponse>>> GetAll(
        [FromQuery] string? customerId,
        CancellationToken cancellationToken)
    {
        var result = await Service.GetAllAsync(customerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 1: Lấy danh sách danh mục có phân trang, tìm kiếm và lọc theo Customer của user.
    /// Output được sắp xếp tự động theo Thứ tự (ThuTu), sau đó đến Mã (Ma).
    /// </summary>
    /// <param name="request">Bao gồm: KeyWord, CustomerId (nếu để trống tự động lấy của user đang đăng nhập), CurrentPage, PageSize, IsActive.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<ActionResult<PagedResult<TResponse>>> GetPagedList(
        [FromBody] PagedDanhMucRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await Service.GetPagedListAsync(request ?? new PagedDanhMucRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: Set danh mục (Vừa thêm mới vừa cập nhật).
    /// Nếu truyền Id và bản ghi tồn tại => Cập nhật; Nếu Id rỗng/chưa có => Thêm mới.
    /// Tự động gán Customer của user đang đăng nhập nếu không truyền CustomerId.
    /// </summary>
    /// <param name="request">Thông tin bản ghi cần thêm mới hoặc cập nhật.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("set")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<ActionResult<TResponse>> Set(
        [FromBody] TSetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Service.SetAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 3: Xóa nhiều bản ghi danh mục theo danh sách Id (listId).
    /// </summary>
    /// <param name="request">Danh sách các mã Id (GUID) cần xóa.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<ActionResult<object>> DeleteMultiple(
        [FromBody] DeleteDanhMucListRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Ids == null || request.Ids.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Danh sách ID rỗng",
                Detail = "Vui lòng cung cấp ít nhất một Id để xóa."
            });
        }

        var deletedCount = await Service.DeleteMultipleAsync(request.Ids, cancellationToken);
        return Ok(new
        {
            Success = true,
            DeletedCount = deletedCount,
            Message = $"Đã xóa thành công {deletedCount} bản ghi."
        });
    }

    /// <summary>
    /// API 4: Import dữ liệu danh mục từ file Excel (EPPlus 6.0.8).
    /// </summary>
    /// <param name="request">Tên tệp tin FileName (đã upload vào thư mục Uploads hoặc trên máy chủ) và CustomerId đích.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<ActionResult<ImportResult>> ImportExcel(
        [FromBody] ImportExcelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Service.ImportExcelAsync(request.FileName, request.CustomerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 5: Xuất (Export) danh mục nhiều dữ liệu ra file Excel (.xlsx) sử dụng EPPlus 6.0.8.
    /// </summary>
    /// <param name="request">Điều kiện lọc: KeyWord, CustomerId, IsActive.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<IActionResult> ExportExcel(
        [FromBody] ExportDanhMucRequest? request,
        CancellationToken cancellationToken)
    {
        var fileBytes = await Service.ExportExcelAsync(request ?? new ExportDanhMucRequest(), cancellationToken);
        var fileName = $"{GetExportFileName()}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    protected abstract string GetExportFileName();
}

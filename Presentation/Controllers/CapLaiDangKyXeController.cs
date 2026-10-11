using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller quản lý Lập biên lai cấp lại đăng ký xe (Thủ tục cấp lại giấy đăng ký xe không xuất kho, không giảm tồn kho biển số).
/// - Hỗ trợ Lưu nháp và Xác nhận (phát hành biên lai).
/// - Khi đã xác nhận, khóa biên lai hoàn toàn (chặn sửa và xóa).
/// - Tuyệt đối không thay đổi thông tin trong KhoBienSo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CapLaiDangKyXeController : ControllerBase
{
    private readonly ICapLaiDangKyXeService _capLaiDangKyXeService;

    public CapLaiDangKyXeController(ICapLaiDangKyXeService capLaiDangKyXeService)
    {
        _capLaiDangKyXeService = capLaiDangKyXeService;
    }

    /// <summary>
    /// API 1: Thêm mới hoặc Cập nhật biên lai cấp lại đăng ký xe.
    /// - Truyền XacNhanHoanTat = false (hoặc null): Lưu nháp bản ghi (TrangThai = 'LuuNhap'), cho phép tiếp tục chỉnh sửa.
    /// - Truyền XacNhanHoanTat = true: Lưu &amp; Phát hành biên lai ngay (TrangThai = 'DaXacNhan'), KHÓA phiếu không cho chỉnh sửa/xóa.
    /// - Tuyệt đối KHÔNG xuất kho hay thay đổi dữ liệu trong KhoBienSo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CapLaiDangKyXeResponse>> SetCapLaiDangKyXe(
        [FromBody] SetCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _capLaiDangKyXeService.SetCapLaiDangKyXeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thao tác không được phép",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API 2: Xác nhận hoàn tất biên lai cấp lại đăng ký xe (Khóa phiếu).
    /// Chuyển trạng thái từ 'LuuNhap' sang 'DaXacNhan'.
    /// Sau khi xác nhận, biên lai sẽ KHÔNG cho phép sửa hay xóa nữa.
    /// </summary>
    [HttpPost("{id}/xac-nhan")]
    [ProducesResponseType(typeof(CapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapLaiDangKyXeResponse>> XacNhan(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _capLaiDangKyXeService.XacNhanAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy dữ liệu",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thao tác không được phép",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API 3: Lấy danh sách biên lai cấp lại đăng ký xe có phân trang và bộ lọc.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CapLaiDangKyXeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CapLaiDangKyXeResponse>>> LayDanhSach(
        [FromQuery] PagedCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.LayDanhSachAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4: Lấy chi tiết một biên lai cấp lại đăng ký xe theo Id.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapLaiDangKyXeResponse>> LayChiTiet(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.LayChiTietAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy",
                Detail = $"Không tìm thấy biên lai cấp lại đăng ký xe với Id '{id}'."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// API 5: Xóa biên lai cấp lại đăng ký xe.
    /// CHỈ cho phép xóa khi biên lai đang ở trạng thái Bản nháp ('LuuNhap').
    /// Nếu đã xác nhận ('DaXacNhan'), hệ thống sẽ từ chối xóa và trả về 400 Bad Request.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> Xoa(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _capLaiDangKyXeService.XoaAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Không thể xóa",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API 6: Lấy thông tin quyển biên lai và gợi ý số biên lai tiếp theo theo ký hiệu quyển.
    /// </summary>
    [HttpGet("so-bien-lai-tiep-theo")]
    [ProducesResponseType(typeof(SoBienLaiTiepTheoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SoBienLaiTiepTheoResponse>> LaySoBienLaiTiepTheo(
        [FromQuery] string kyHieuQuyenBienLaiId,
        CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.LaySoBienLaiTiepTheoAsync(kyHieuQuyenBienLaiId, cancellationToken);
        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy",
                Detail = "Không tìm thấy thông tin Ký hiệu quyển biên lai."
            });
        }

        return Ok(result);
    }
}

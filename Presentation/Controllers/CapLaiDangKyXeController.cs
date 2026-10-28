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
    private readonly ILogHoatDongService _logHoatDongService;

    public CapLaiDangKyXeController(
        ICapLaiDangKyXeService capLaiDangKyXeService,
        ILogHoatDongService logHoatDongService)
    {
        _capLaiDangKyXeService = capLaiDangKyXeService;
        _logHoatDongService = logHoatDongService;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CapLaiDangKyXeResponse>> SetCapLaiDangKyXe(
        [FromBody] SetCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.SetCapLaiDangKyXeAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: Xác nhận hoàn tất biên lai cấp lại đăng ký xe (Khóa phiếu).
    /// Chuyển trạng thái từ 'LuuNhap' sang 'DaXacNhan'.
    /// Sau khi xác nhận, biên lai sẽ KHÔNG cho phép sửa hay xóa nữa.
    /// </summary>
    [HttpPost("{id}/xac-nhan")]
    [ProducesResponseType(typeof(CapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CapLaiDangKyXeResponse>> XacNhan(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.XacNhanAsync(id, cancellationToken);
        return Ok(result);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> Xoa(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.XoaAsync(id, cancellationToken);
        return Ok(result);
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

    /// <summary>
    /// API 7: (GET/POST) Lấy nhật ký hoạt động của phân hệ Cấp lại đăng ký xe.
    /// </summary>
    [HttpGet("log")]
    [HttpPost("log")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogCapLai([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FilterLogHoatDongRequest();
        request.LoaiNghiepVu = "CapLaiDangKy";
        var result = await _logHoatDongService.LayDanhSachLogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 8: (GET) Lấy lịch sử log của 1 biên lai cấp lại đăng ký xe cụ thể theo Id hoặc Số biên lai.
    /// </summary>
    [HttpGet("{id}/log")]
    [ProducesResponseType(typeof(IReadOnlyList<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogTheoBienLai([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _logHoatDongService.LayLogTheoChungTuAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 9: (POST) Import danh sách biên lai cấp lại đăng ký xe từ tệp Excel theo mẫu MauBaoCao/CapLaiBienDangKy.xlsx.
    /// - Biển số không lấy trong kho và tuyệt đối không thay đổi dữ liệu trong kho.
    /// - Mặc định DaXacNhan = true (tự động khóa biên lai, phát hành ngay).
    /// </summary>
    /// <param name="request">Yêu cầu chứa tên file (FileName), tùy chọn Loại xe, đơn vị và cờ DaXacNhan.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Kết quả tổng hợp số lượng import thành công, bỏ qua, lỗi, tổng tiền và danh sách biển số đã import.</returns>
    [HttpPost("import-excel")]
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportCapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportCapLaiDangKyXeResponse>> ImportExcel(
        [FromBody] ImportCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _capLaiDangKyXeService.ImportTuExcelAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy tệp Excel",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu yêu cầu không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API 10: (POST) Kiểm tra trước tệp tin Excel cấp lại đăng ký xe (dry-run).
    /// Đọc và kiểm tra tính hợp lệ dữ liệu của tệp Excel nhưng KHÔNG ghi vào cơ sở dữ liệu.
    /// </summary>
    [HttpPost("check-import-excel")]
    [ProducesResponseType(typeof(ImportCapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportCapLaiDangKyXeResponse>> CheckImportExcel(
        [FromBody] ImportCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            request.ChiKiemTra = true;
            var result = await _capLaiDangKyXeService.ImportTuExcelAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy tệp Excel",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu yêu cầu không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API 11: (POST) Upload và Import trực tiếp tệp Excel cấp lại đăng ký xe từ máy khách (multipart/form-data).
    /// - Biển số không lấy trong kho và không thay đổi dữ liệu trong kho.
    /// - Mặc định DaXacNhan = true.
    /// </summary>
    [HttpPost("import-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportCapLaiDangKyXeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportCapLaiDangKyXeResponse>> ImportUpload(
        [FromForm] ImportCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _capLaiDangKyXeService.ImportTuExcelAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu yêu cầu không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API 12: (GET) Lấy thông tin tệp tin Excel mẫu cấp lại biển đăng ký xe (MauBaoCao/CapLaiBienDangKy.xlsx).
    /// Output trả về các tham số filename và path (mã hóa Base64) là input cho API tải file /api/FileUpload/download.
    /// </summary>
    [HttpGet("export-mau")]
    [ProducesResponseType(typeof(ExportMauXuatKhoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportMauXuatKhoResponse>> ExportMau(CancellationToken cancellationToken)
    {
        var result = await _capLaiDangKyXeService.ExportFileMauCapLaiDangKyAsync(cancellationToken);
        return Ok(result);
    }
}

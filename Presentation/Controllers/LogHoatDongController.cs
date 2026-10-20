using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller Quản lý Nhật ký hoạt động (Audit Log / Log hệ thống):
/// Theo dõi và hiển thị toàn bộ lịch sử thao tác của 4 phân hệ cốt lõi:
/// 1. Nhập kho biển số xe (DotNhapKho)
/// 2. Xuất kho biển số xe &amp; Biên lai thu tiền (XuatKhoBienSo)
/// 3. Kiểm kê kho biển số xe (KiemKeKho)
/// 4. Cấp lại đăng ký xe (CapLaiDangKyXe)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/NhatKyHoatDong")]
[Route("api/AuditLog")]
[Produces("application/json")]
[Tags("Nhật ký Hoạt động (Audit Log)")]
public class LogHoatDongController : ControllerBase
{
    private readonly ILogHoatDongService _logHoatDongService;

    public LogHoatDongController(ILogHoatDongService logHoatDongService)
    {
        _logHoatDongService = logHoatDongService;
    }

    /// <summary>
    /// API 1: (POST) Lấy danh sách nhật ký hoạt động có phân trang và bộ lọc toàn diện.
    /// Hỗ trợ lọc theo:
    /// - Phân hệ (LoaiNghiepVu: "TatCa", "NhapKho", "XuatKho", "KiemKe", "CapLaiDangKy")
    /// - Hành động (HanhDong: "ThemMoi", "CapNhat", "XacNhan", "Huy", "Xoa")
    /// - Từ khóa (Keyword: tìm theo Mã chứng từ, Số biên lai, Biển số, Người thực hiện, Nội dung)
    /// - Biển kiểm soát cụ thể (BienKiemSoat)
    /// - Khoảng ngày (TuNgay, DenNgay)
    /// </summary>
    [HttpPost("danhsach")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSach([FromBody] PagedLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        var result = await _logHoatDongService.LayDanhSachLogAsync(request ?? new PagedLogHoatDongRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 1b: (GET) Lấy danh sách nhật ký hoạt động qua Query params (thuận tiện cho GET request).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSachGet([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        var result = await _logHoatDongService.LayDanhSachLogAsync(request ?? new FilterLogHoatDongRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: (GET) Lấy danh sách nhật ký riêng cho phân hệ Nhập kho biển số xe.
    /// </summary>
    [HttpGet("nhapkho")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogNhapKho([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FilterLogHoatDongRequest();
        request.LoaiNghiepVu = "NhapKho";
        var result = await _logHoatDongService.LayDanhSachLogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 3: (GET) Lấy danh sách nhật ký riêng cho phân hệ Xuất kho biển số xe &amp; Biên lai thu tiền.
    /// </summary>
    [HttpGet("xuatkho")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogXuatKho([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FilterLogHoatDongRequest();
        request.LoaiNghiepVu = "XuatKho";
        var result = await _logHoatDongService.LayDanhSachLogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4: (GET) Lấy danh sách nhật ký riêng cho phân hệ Kiểm kê kho biển số xe.
    /// </summary>
    [HttpGet("kiemke")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogKiemKe([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FilterLogHoatDongRequest();
        request.LoaiNghiepVu = "KiemKe";
        var result = await _logHoatDongService.LayDanhSachLogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 5: (GET) Lấy danh sách nhật ký riêng cho phân hệ Cấp lại đăng ký xe.
    /// </summary>
    [HttpGet("caplaidangky")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogCapLaiDangKy([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FilterLogHoatDongRequest();
        request.LoaiNghiepVu = "CapLaiDangKy";
        var result = await _logHoatDongService.LayDanhSachLogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 6: (GET) Lấy toàn bộ lịch sử các thao tác/thay đổi của một chứng từ cụ thể theo ChungTuId hoặc MaChungTu.
    /// </summary>
    [HttpGet("chungtu/{chungTuId}")]
    [ProducesResponseType(typeof(IReadOnlyList<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogTheoChungTu([FromRoute] string chungTuId, CancellationToken cancellationToken)
    {
        var result = await _logHoatDongService.LayLogTheoChungTuAsync(chungTuId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 7: (GET) Thống kê tổng số lượng nhật ký hoạt động theo từng phân hệ và mốc thời gian.
    /// </summary>
    [HttpGet("thongke")]
    [ProducesResponseType(typeof(ThongKeLogHoatDongResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayThongKeLog([FromQuery] string? customerId, CancellationToken cancellationToken)
    {
        var result = await _logHoatDongService.LayThongKeLogAsync(customerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 8: (POST) Đồng bộ nạp lại nhật ký hoạt động từ dữ liệu có sẵn của 4 phân hệ (nếu chưa có).
    /// </summary>
    [HttpPost("dongbo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DongBoLog(CancellationToken cancellationToken)
    {
        await _logHoatDongService.DongBoLogTuDuLieuCoSanAsync(cancellationToken);
        return Ok(new { success = true, message = "Đã đồng bộ nhật ký hoạt động từ dữ liệu có sẵn thành công." });
    }
}

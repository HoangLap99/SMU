using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller Báo cáo: Biểu đồ phân tích biến thiên Nhập - Xuất - Chi phí lệ phí:
/// Mỗi khoanh đỏ trên thiết kế tương ứng với 1 API độc lập.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Báo Cáo Biến Thiên Nhập - Xuất - Lệ Phí")]
public class BaoCaoBienThienController : ControllerBase
{
    private readonly IBaoCaoBienThienService _baoCaoService;

    public BaoCaoBienThienController(IBaoCaoBienThienService baoCaoService)
    {
        _baoCaoService = baoCaoService;
    }

    /// <summary>
    /// API Khoanh 2: Thẻ tóm tắt KPI / Thống kê tổng quan.
    /// Trả về: Tổng số biển nhập kho (và số đợt), Tổng số biển xuất kho, Tiền thu lệ phí xuất biển, Tỷ lệ xuất/nhập kho (%).
    /// </summary>
    [HttpPost("tong-quan")]
    [ProducesResponseType(typeof(BaoCaoKpiTongQuanResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoKpiTongQuanResponse>> LayKpiTongQuan(
        [FromBody] FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayKpiTongQuanAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Khoanh 3: Biểu đồ 1 - Biến thiên Số Lượng Biển Nhập và Biển Xuất theo thời gian.
    /// So sánh khối lượng biển tiếp nhận vào kho (Nhập) và khối lượng biển cấp phát ngẫu nhiên (Xuất) hiển thị theo Tháng hoặc Ngày.
    /// </summary>
    [HttpPost("bieu-do-nhap-xuat")]
    [ProducesResponseType(typeof(BieuDoBienThienNhapXuatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BieuDoBienThienNhapXuatResponse>> LayBieuDo1NhapXuat(
        [FromBody] FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayBieuDo1NhapXuatAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Khoanh 4: Biểu đồ 2 - Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất theo thời gian.
    /// Tổng số tiền lệ phí thu ngân sách nhà nước từ các hồ sơ xuất biển và đăng ký xe (VNĐ) theo Tháng hoặc Ngày, kèm Tổng kỳ.
    /// </summary>
    [HttpPost("bieu-do-le-phi")]
    [ProducesResponseType(typeof(BieuDoBienThienLePhiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BieuDoBienThienLePhiResponse>> LayBieuDo2LePhi(
        [FromBody] FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayBieuDo2LePhiAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Khoanh 5: Cơ cấu biển xuất theo thủ tục &amp; Phân bổ theo loại phương tiện.
    /// - Theo thủ tục: Phân bổ lượt giải quyết hồ sơ (Cấp biển, Sang tên, Cấp lại... số lượt, %, xuất từ kho, tiền thu).
    /// - Theo loại phương tiện: Ô tô, Xe máy, Rơ moóc, Khác (số lượng, %).
    /// </summary>
    [HttpPost("co-cau-thu-tuc")]
    [ProducesResponseType(typeof(CoCauBienXuatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoCauBienXuatResponse>> LayCoCauBienXuat(
        [FromBody] FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayCoCauBienXuatAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Khoanh 6: Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết.
    /// Trả về các cột: STT, Thời gian, Biển nhập kho, Biển xuất kho, Số hồ sơ, Tiền thu lệ phí, Chênh lệch (Nhập - Xuất) và dòng TỔNG CỘNG.
    /// </summary>
    [HttpPost("bang-chi-tiet")]
    [ProducesResponseType(typeof(BangSoLieuBienThienResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BangSoLieuBienThienResponse>> LayBangSoLieuChiTiet(
        [FromBody] FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayBangSoLieuChiTietAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Tổng hợp Toàn diện (All-in-One Dashboard):
    /// Trả về trọn bộ cả 5 khối dữ liệu chỉ trong 1 lần gọi duy nhất giúp tối ưu hiệu năng frontend.
    /// </summary>
    [HttpPost("dashboard")]
    [ProducesResponseType(typeof(BaoCaoBienThienDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoBienThienDashboardResponse>> LayToanBoDashboard(
        [FromBody] FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayToanBoBaoCaoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Bộ lọc: Lấy danh sách tùy chọn cho nút chuyển đổi 'Gom theo' (Button toggle: [Theo tháng], [Theo ngày]).
    /// </summary>
    [HttpGet("bo-loc/gom-theo")]
    [ProducesResponseType(typeof(List<BoLocGomTheoItemDto>), StatusCodes.Status200OK)]
    public ActionResult<List<BoLocGomTheoItemDto>> LayBoLocGomTheo()
    {
        var result = _baoCaoService.LayBoLocGomTheo();
        return Ok(result);
    }

    /// <summary>
    /// API Bộ lọc: Lấy danh sách các nút bấm chọn nhanh 'Mốc thời gian' (Pill filters: [Toàn bộ], [Tháng 8/2026], [Tháng 9/2026], [Tháng 10/2026], [Quý 3/2026]).
    /// Tự động tính toán ngày bắt đầu (tuNgay) và ngày kết thúc (denNgay) dựa trên dải dữ liệu thực tế từ bảng KhoBienSo.
    /// </summary>
    [HttpGet("bo-loc/moc-thoi-gian")]
    [ProducesResponseType(typeof(List<BoLocMocThoiGianItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BoLocMocThoiGianItemDto>>> LayBoLocMocThoiGian(
        [FromQuery] string? customerId,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoService.LayBoLocMocThoiGianAsync(customerId, cancellationToken);
        return Ok(result);
    }
}

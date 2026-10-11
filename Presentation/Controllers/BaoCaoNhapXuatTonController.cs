using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller Báo cáo: Báo cáo Nhập - Xuất - Tồn Biển số xe.
/// Đồng bộ tuyệt đối giữa màn hình hiển thị và bản in A4.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Báo Cáo Nhập - Xuất - Tồn Biển Số Xe")]
public class BaoCaoNhapXuatTonController : ControllerBase
{
    private readonly IBaoCaoNhapXuatTonService _baoCaoNhapXuatTonService;

    public BaoCaoNhapXuatTonController(IBaoCaoNhapXuatTonService baoCaoNhapXuatTonService)
    {
        _baoCaoNhapXuatTonService = baoCaoNhapXuatTonService;
    }

    /// <summary>
    /// API Lấy dữ liệu Báo cáo Nhập - Xuất - Tồn Biển số xe theo kỳ báo cáo và bộ lọc.
    /// Trả về: STT, Đầu biển, Loại xe, Màu biển, Tồn đầu kỳ, Nhập trong kỳ, Xuất trong kỳ, Tồn cuối kỳ và dòng TỔNG CỘNG.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BaoCaoNhapXuatTonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoNhapXuatTonResponse>> LayBaoCaoNhapXuatTon(
        [FromBody] FilterBaoCaoNhapXuatTonRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayBaoCaoNhapXuatTonAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Lấy dữ liệu định dạng chuẩn để in ấn A4 (Print Preview / Xuất báo cáo bản in).
    /// Đồng bộ tuyệt đối với dữ liệu màn hình hiện tại.
    /// </summary>
    [HttpPost("ina4")]
    [ProducesResponseType(typeof(BaoCaoNhapXuatTonPrintDataDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoNhapXuatTonPrintDataDto>> LayDuLieuInA4(
        [FromBody] FilterBaoCaoNhapXuatTonRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayDuLieuInA4Async(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Báo cáo 1 (Tab 3): Báo cáo Biển số tồn kho tại thời điểm.
    /// Chốt danh sách chi tiết các biển số đang tồn kho tính đến hết ngày được chọn (Ví dụ: 31/08/2026).
    /// Trả về: STT, Biển kiểm soát, Đầu biển, Loại xe, Màu biển, Ngày nhập kho, Trạng thái ("Biển được bấm (Tồn kho)").
    /// </summary>
    [HttpPost("tontaithoidiem")]
    [ProducesResponseType(typeof(BaoCaoTonTaiThoiDiemResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoTonTaiThoiDiemResponse>> LayBaoCaoTonTaiThoiDiem(
        [FromBody] FilterBaoCaoTonTaiThoiDiemRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayBaoCaoTonTaiThoiDiemAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Báo cáo 2 (Tab 4): Báo cáo Tiền thu theo biên lai và thủ tục.
    /// Danh sách chi tiết các biên lai thu tiền trong kỳ (Cấp biển, Sang tên, Cấp lại, Chỉ ghi biên lai...).
    /// Trả về: STT, Ký hiệu - Số biên lai, Ngày biên lai, Thủ tục, Biển số, Người nộp tiền, Số tiền thu và Tổng tiền thu lệ phí.
    /// </summary>
    [HttpPost("tienthubienlai")]
    [ProducesResponseType(typeof(BaoCaoTienThuBienLaiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoTienThuBienLaiResponse>> LayBaoCaoTienThuBienLai(
        [FromBody] FilterBaoCaoTienThuBienLaiRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayBaoCaoTienThuBienLaiAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Báo cáo 3 (Tab 5): Bảng thống kê thủ tục và tiền thu.
    /// Tách số lượt hồ sơ, số biển xuất kho và tiền thu theo từng loại thủ tục (Cấp biển, Sang tên, Cấp lại, Chỉ ghi biên lai...).
    /// </summary>
    [HttpPost("thongkethutuc")]
    [ProducesResponseType(typeof(BangThongKeThuTucResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BangThongKeThuTucResponse>> LayBangThongKeThuTuc(
        [FromBody] FilterBangThongKeThuTucRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayBangThongKeThuTucAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Báo cáo 4 (Tab 6): Bảng kê chi tiết đợt nhập biển số xe.
    /// Danh sách các đợt tiếp nhận dải số tiếp nhận vào kho trong kỳ.
    /// Trả về: STT, Mã đợt nhập, Ngày nhập, Đầu biển, Loại xe, Số lượng nhập, Số được bấm, Cán bộ tiếp nhận và Tổng biển đã tiếp nhận.
    /// </summary>
    [HttpPost("chitietdotnhap")]
    [ProducesResponseType(typeof(BangKeChiTietDotNhapResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BangKeChiTietDotNhapResponse>> LayBangKeChiTietDotNhap(
        [FromBody] FilterBangKeChiTietDotNhapRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayBangKeChiTietDotNhapAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API Báo cáo 5 (Tab 7): Bảng kê chi tiết biển số đã xuất kho.
    /// Danh sách các biển số đã cấp phát xuất kho ngẫu nhiên trong kỳ.
    /// Trả về: STT, Ngày xuất, Biển số đã cấp, Đầu biển, Số biên lai, Người nhận biển, Lệ phí thu, Cán bộ cấp và Tổng biển đã xuất kho.
    /// </summary>
    [HttpPost("chitietxuatkho")]
    [ProducesResponseType(typeof(BangKeChiTietXuatKhoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BangKeChiTietXuatKhoResponse>> LayBangKeChiTietXuatKho(
        [FromBody] FilterBangKeChiTietXuatKhoRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _baoCaoNhapXuatTonService.LayBangKeChiTietXuatKhoAsync(request, cancellationToken);
        return Ok(result);
    }
}

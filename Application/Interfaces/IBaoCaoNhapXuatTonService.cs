using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ Báo cáo: Báo cáo Nhập - Xuất - Tồn Biển số xe.
/// </summary>
public interface IBaoCaoNhapXuatTonService
{
    /// <summary>
    /// Lấy dữ liệu Báo cáo Nhập - Xuất - Tồn Biển số xe theo kỳ báo cáo và bộ lọc.
    /// Tính toán chính xác: Tồn đầu kỳ, Nhập trong kỳ, Xuất trong kỳ, Tồn cuối kỳ cho từng nhóm dải Đầu biển.
    /// </summary>
    Task<BaoCaoNhapXuatTonResponse> LayBaoCaoNhapXuatTonAsync(
        FilterBaoCaoNhapXuatTonRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy dữ liệu định dạng chuẩn sẵn sàng in ấn A4 (đồng bộ tuyệt đối với dữ liệu màn hình).
    /// </summary>
    Task<BaoCaoNhapXuatTonPrintDataDto> LayDuLieuInA4Async(
        FilterBaoCaoNhapXuatTonRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Báo cáo 1 (Tab 3): Báo cáo Biển số tồn kho tại thời điểm.
    /// Chốt danh sách chi tiết các biển số đang tồn kho tính đến hết ngày được chọn.
    /// </summary>
    Task<BaoCaoTonTaiThoiDiemResponse> LayBaoCaoTonTaiThoiDiemAsync(
        FilterBaoCaoTonTaiThoiDiemRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Báo cáo 2 (Tab 4): Báo cáo Tiền thu theo biên lai và thủ tục.
    /// Danh sách chi tiết các biên lai thu tiền trong kỳ (Cấp biển, Sang tên, Cấp lại, Chỉ ghi biên lai...).
    /// </summary>
    Task<BaoCaoTienThuBienLaiResponse> LayBaoCaoTienThuBienLaiAsync(
        FilterBaoCaoTienThuBienLaiRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Báo cáo 3 (Tab 5): Bảng thống kê thủ tục và tiền thu.
    /// Thống kê tách số lượt hồ sơ, số biển xuất kho và tổng tiền thu theo từng loại thủ tục.
    /// </summary>
    Task<BangThongKeThuTucResponse> LayBangThongKeThuTucAsync(
        FilterBangThongKeThuTucRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Báo cáo 4 (Tab 6): Bảng kê chi tiết đợt nhập biển số xe.
    /// Danh sách các đợt tiếp nhận dải số tiếp nhận vào kho trong kỳ.
    /// </summary>
    Task<BangKeChiTietDotNhapResponse> LayBangKeChiTietDotNhapAsync(
        FilterBangKeChiTietDotNhapRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Báo cáo 5 (Tab 7): Bảng kê chi tiết biển số đã xuất kho.
    /// Danh sách các biển số đã cấp phát xuất kho ngẫu nhiên trong kỳ.
    /// </summary>
    Task<BangKeChiTietXuatKhoResponse> LayBangKeChiTietXuatKhoAsync(
        FilterBangKeChiTietXuatKhoRequest? request,
        CancellationToken cancellationToken = default);
}

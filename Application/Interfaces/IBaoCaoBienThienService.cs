using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ Báo cáo: Biểu đồ phân tích biến thiên Nhập - Xuất - Chi phí lệ phí.
/// Cung cấp API độc lập cho từng khối biểu đồ (khoanh đỏ) và bảng số liệu.
/// </summary>
public interface IBaoCaoBienThienService
{
    /// <summary>
    /// API Khoanh 2: Thẻ tóm tắt KPI / Thống kê tổng quan.
    /// (Tổng số biển nhập, Tổng số biển xuất, Tiền thu lệ phí xuất biển, Tỷ lệ xuất/nhập).
    /// </summary>
    Task<BaoCaoKpiTongQuanResponse> LayKpiTongQuanAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Khoanh 3: Biểu đồ 1 - Biến thiên Số Lượng Biển Nhập và Biển Xuất theo thời gian.
    /// Hiển thị dạng cột kép (Nhập vs Xuất) gom theo Tháng hoặc Ngày.
    /// </summary>
    Task<BieuDoBienThienNhapXuatResponse> LayBieuDo1NhapXuatAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Khoanh 4: Biểu đồ 2 - Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất theo thời gian.
    /// Hiển thị tổng tiền thu theo từng mốc thời gian (Tháng hoặc Ngày) kèm Tổng kỳ.
    /// </summary>
    Task<BieuDoBienThienLePhiResponse> LayBieuDo2LePhiAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Khoanh 5: Cơ cấu biển xuất theo thủ tục &amp; Phân bổ theo loại phương tiện.
    /// (Cấp biển, Sang tên, Cấp lại... và Ô tô, Xe máy, Rơ moóc, Khác).
    /// </summary>
    Task<CoCauBienXuatResponse> LayCoCauBienXuatAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Khoanh 6: Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết.
    /// Chi tiết từng mốc (Tháng hoặc Ngày): Biển nhập, Biển xuất, Số hồ sơ, Tiền thu, Chênh lệch và dòng Tổng cộng.
    /// </summary>
    Task<BangSoLieuBienThienResponse> LayBangSoLieuChiTietAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Tổng hợp: Trả về toàn bộ dữ liệu của cả 5 khoanh đỏ trong 1 lần gọi duy nhất.
    /// </summary>
    Task<BaoCaoBienThienDashboardResponse> LayToanBoBaoCaoAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Bộ lọc 1: Lấy danh sách các tùy chọn cho bộ lọc 'Gom theo' (Theo tháng, Theo ngày).
    /// </summary>
    List<BoLocGomTheoItemDto> LayBoLocGomTheo();

    /// <summary>
    /// API Bộ lọc 2: Lấy danh sách các tùy chọn cho bộ lọc 'Mốc thời gian' chọn nhanh ([Toàn bộ], [Tháng 8/2026], [Tháng 9/2026], [Tháng 10/2026], [Quý 3/2026]).
    /// </summary>
    Task<List<BoLocMocThoiGianItemDto>> LayBoLocMocThoiGianAsync(
        string? customerId = null,
        CancellationToken cancellationToken = default);
}

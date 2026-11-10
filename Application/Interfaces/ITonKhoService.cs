using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Interface dịch vụ Quản lý Tồn kho biển số xe:
/// - Bảng tổng hợp tồn kho theo đầu biển (Tồn kho được bấm, Đã bấm, Tổng biển)
/// - Lấy danh sách các biển số chưa xuất khi click vào nút "Chi tiết" ở 1 nhóm đầu biển
/// - Dữ liệu danh mục phục vụ bộ lọc tìm kiếm
/// </summary>
public interface ITonKhoService
{
    /// <summary>
    /// API 1: Lấy Bảng Tổng Hợp Tồn Kho Theo Đầu Biển (áp dụng theo bộ lọc hiện tại).
    /// Group by theo Đầu biển, Loại phương tiện, Màu biển và tính Tồn kho (Được bấm), Đã bấm, Tổng biển kèm dòng tổng cộng.
    /// </summary>
    Task<TongHopTonKhoResponse> LayTongHopTonKhoAsync(FilterTonKhoRequest? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 2: Lấy danh sách các biển số CHƯA XUẤT KHO khi click vào nút "Chi tiết" của 1 nhóm đầu biển.
    /// Điều kiện: Biển thuộc nhóm đầu biển &amp;&amp; !DaXuatKho &amp;&amp; TrangThai == "DuocBam" &amp;&amp; !LaBienDauGia.
    /// </summary>
    Task<PagedResult<BienSoChuaXuatItemResponse>> LayDanhSachBienSoChuaXuatAsync(ChiTietTonKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 3: Lấy dữ liệu cấu hình bộ lọc tìm kiếm tồn kho (Danh sách đầu biển kèm số lượng, Loại xe, Màu biển, Trạng thái).
    /// </summary>
    Task<BoLocTonKhoResponse> LayThongTinBoLocAsync(string? customerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 4: Lấy Ma Trận 100 Số Dải Biển từ .00 đến .99 trong bảng KhoBienSo.
    /// Input là đầu biển số (Ví dụ: "29AC-963"), Output là dãy 100 số kèm trạng thái và màu sắc tương ứng.
    /// </summary>
    Task<MaTranDaiSoResponse> LayMaTranDaiSoAsync(string dauBien, string? customerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 5: Cập nhật trạng thái của 1 số trong ma trận dải số.
    /// </summary>
    Task<bool> CapNhatTrangThaiSoAsync(CapNhatTrangThaiSoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 6: Xuất file Excel Biên bản kiểm kê danh sách biển số tồn trong KhoBienSo theo chuẩn mẫu MauBaoCao/Tồn tháng 8.25.xlsx.
    /// </summary>
    Task<(byte[] FileBytes, string ContentType, string FileName)> ExportBienTonExcelAsync(ExportBienTonExcelRequest? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 7: Lấy danh sách cán bộ thuộc đơn vị của user hiện tại đang đăng nhập (kèm cấp bậc CAND và chức vụ).
    /// </summary>
    Task<IReadOnlyList<CanBoDonViResponse>> LayDanhSachCanBoDonViAsync(string? customerId = null, CancellationToken cancellationToken = default);
}

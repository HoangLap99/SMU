using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Interface dịch vụ Quản lý Kiểm kê kho biển số xe:
/// - Khởi tạo phiếu kiểm kê tổng hợp từ tồn kho thực tế của các biển chưa xuất
/// - Popup xác định biển thiếu / thừa theo dải đầu biển
/// - Thêm mới và cập nhật phiếu kiểm kê (Lưu nháp / Xác nhận hoàn tất)
/// - Lấy danh sách phiếu kiểm kê có phân trang và bộ lọc
/// - Lấy chi tiết và xóa phiếu kiểm kê
/// </summary>
public interface IKiemKeKhoService
{
    /// <summary>
    /// API Khởi tạo dữ liệu phiếu kiểm kê từ bảng KhoBienSo đối với các biển chưa xuất.
    /// Gom nhóm theo dải đầu biển và màu biển, tính số lượng tồn hệ thống, gán mặc định Thực tế = Tồn hệ thống.
    /// </summary>
    Task<KhoiTaoKiemKeKhoResponse> KhoiTaoPhieuKiemKeAsync(
        KhoiTaoKiemKeKhoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Lấy dữ liệu cho Popup "Xác định biển thiếu/thừa: Dải {dauBien} ({mauBien})".
    /// Lấy danh sách biển cụ thể trong kho chưa xuất để chuyển trạng thái "Có mặt" / "Thiếu" và danh sách biển thừa.
    /// </summary>
    Task<ChiTietBienKiemKeResponse> LayChiTietBienPopupAsync(
        ChiTietBienKiemKeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Thêm mới hoặc cập nhật phiếu kiểm kê kho (Lưu nháp hoặc Xác nhận hoàn tất).
    /// </summary>
    Task<KiemKeKhoResponse> SetKiemKeKhoAsync(
        SetKiemKeKhoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Lấy danh sách phiếu kiểm kê kho có phân trang và bộ lọc.
    /// </summary>
    Task<PagedResult<KiemKeKhoResponse>> LayDanhSachKiemKeAsync(
        PagedKiemKeKhoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Lấy thông tin chi tiết 1 phiếu kiểm kê kho theo Id.
    /// </summary>
    Task<KiemKeKhoResponse?> LayChiTietKiemKeAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Xóa phiếu kiểm kê kho theo Id.
    /// </summary>
    Task<bool> XoaKiemKeKhoAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tự động xác định dải đầu biển số từ biển kiểm soát (Ví dụ: "30B-207.66" -> "30B-207", "29E-794.02" -> "29E-794").
    /// </summary>
    Task<XacDinhDauBienResponse> XacDinhDauBienTuBienSoAsync(
        string bienKiemSoat,
        string? customerId = null,
        CancellationToken cancellationToken = default);
}

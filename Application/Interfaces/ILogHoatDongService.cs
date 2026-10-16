using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý Nhật ký hoạt động / Audit Log cho 4 phân hệ:
/// Nhập kho, Xuất kho, Kiểm kê kho và Cấp lại đăng ký xe.
/// </summary>
public interface ILogHoatDongService
{
    /// <summary>
    /// Lấy danh sách nhật ký hoạt động có phân trang và bộ lọc linh hoạt.
    /// Hỗ trợ lọc theo loại nghiệp vụ (NhapKho, XuatKho, KiemKe, CapLaiDangKy),
    /// hành động (ThemMoi, CapNhat, XacNhan, Huy, Xoa), từ khóa, khoảng ngày.
    /// </summary>
    Task<PagedResult<LogHoatDongResponse>> LayDanhSachLogAsync(
        PagedLogHoatDongRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ lịch sử log thao tác của một chứng từ cụ thể (theo ChungTuId hoặc MaChungTu).
    /// </summary>
    Task<IReadOnlyList<LogHoatDongResponse>> LayLogTheoChungTuAsync(
        string chungTuId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thống kê số lượng hoạt động theo từng phân hệ và mốc thời gian (hôm nay, tuần này, tháng này).
    /// </summary>
    Task<ThongKeLogHoatDongResponse> LayThongKeLogAsync(
        string? customerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi nhận một hành động nghiệp vụ vào nhật ký hoạt động.
    /// </summary>
    Task GhiLogAsync(
        string loaiNghiepVu,
        string hanhDong,
        string maChungTu,
        string noiDung,
        string? chungTuId = null,
        string? bienKiemSoat = null,
        string? trangThai = null,
        string? trangThaiTen = null,
        string? chiTietThayDoi = null,
        string? customerId = null,
        string? ghiChu = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đồng bộ / nạp các giao dịch có sẵn từ 4 bảng nghiệp vụ (Nhập kho, Xuất kho, Kiểm kê, Cấp lại)
    /// vào bảng LogHoatDong nếu chưa có log.
    /// </summary>
    Task DongBoLogTuDuLieuCoSanAsync(CancellationToken cancellationToken = default);
}

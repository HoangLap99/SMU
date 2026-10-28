using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Interface dịch vụ Quản lý Cấp lại đăng ký xe:
/// - Lập thủ tục &amp; biên lai thu tiền cấp lại đăng ký xe (không xuất kho, không giảm tồn kho)
/// - Hỗ trợ Lưu nháp và Xác nhận (phát hành biên lai)
/// - Khi đã xác nhận thì KHÓA biên lai, không cho phép chỉnh sửa hay xóa
/// - Không làm thay đổi bất kỳ thông tin nào trong KhoBienSo
/// - Quản lý danh sách, chi tiết, xóa bản nháp và gợi ý số biên lai tiếp theo
/// </summary>
public interface ICapLaiDangKyXeService
{
    /// <summary>
    /// Thêm mới hoặc Cập nhật biên lai cấp lại đăng ký xe (hỗ trợ cả Lưu nháp và Xác nhận phát hành).
    /// </summary>
    Task<CapLaiDangKyXeResponse> SetCapLaiDangKyXeAsync(SetCapLaiDangKyXeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác nhận hoàn tất biên lai đang ở trạng thái Lưu nháp sang Đã xác nhận (KHÓA biên lai).
    /// </summary>
    Task<CapLaiDangKyXeResponse> XacNhanAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách biên lai cấp lại đăng ký xe có phân trang và bộ lọc.
    /// </summary>
    Task<PagedResult<CapLaiDangKyXeResponse>> LayDanhSachAsync(PagedCapLaiDangKyXeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết một biên lai cấp lại đăng ký xe theo Id.
    /// </summary>
    Task<CapLaiDangKyXeResponse?> LayChiTietAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một biên lai cấp lại đăng ký xe (Chỉ cho phép xóa khi đang ở trạng thái Lưu nháp; Đã xác nhận sẽ chặn không cho xóa).
    /// </summary>
    Task<bool> XoaAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin quyển biên lai và gợi ý số biên lai tiếp theo theo KyHieuQuyenBienLaiId.
    /// </summary>
    Task<SoBienLaiTiepTheoResponse?> LaySoBienLaiTiepTheoAsync(string kyHieuQuyenBienLaiId, CancellationToken cancellationToken = default);


    /// <summary>
    /// API 10: (GET) Lấy thông tin tệp tin Excel mẫu cấp lại biển đăng ký (MauBaoCao/CapLaiBienDangKy.xlsx).
    /// Output trả về các tham số filename và path (mã hóa Base64) là input cho API tải file /api/FileUpload/download.
    /// </summary>
    Task<ExportMauXuatKhoResponse> ExportFileMauCapLaiDangKyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Import danh sách biên lai cấp lại đăng ký xe từ tệp Excel (mẫu MauBaoCao/CapLaiBienDangKy.xlsx).
    /// Quy tắc: Biển số không lấy trong kho, không thay đổi dữ liệu trong kho, mặc định DaXacNhan = true.
    /// </summary>
    Task<ImportCapLaiDangKyXeResponse> ImportTuExcelAsync(ImportCapLaiDangKyXeRequest request, CancellationToken cancellationToken = default);
}

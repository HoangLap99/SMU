using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Interface dịch vụ Quản lý Xuất kho biển số xe:
/// - Lấy danh sách biển số chưa xuất kho khả dụng trong kho KhoBienSo
/// - Lấy thông tin mức thu lệ phí theo nội dung và khu vực áp dụng (TT 155/2025/TT-BTC)
/// - Lấy thông tin số biên lai tiếp theo theo ký hiệu quyển
/// - Lập thủ tục &amp; biên lai thu tiền (Xuất kho)
/// - Quản lý danh sách, chi tiết và xóa phiếu xuất kho
/// </summary>
public interface IXuatKhoService
{
    /// <summary>
    /// API 1: Lấy danh sách biển số chưa xuất kho khả dụng trong bảng KhoBienSo (TrangThai == "DuocBam", !LaBienDauGia, !DaXuatKho).
    /// Kèm tổng số biển khả dụng để hiển thị "(Còn 591 biển khả dụng trong kho)".
    /// </summary>
    Task<DanhSachBienSoKhaDungResponse> LayDanhSachBienSoKhaDungAsync(FilterBienSoKhaDungRequest? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 2: Lấy thông tin mức thu và tự động điền số tiền dựa vào nội dung lệ phí (MucThuLePhiId) và khu vực (KhuVucApDungId) từ bảng MucThuLePhiKhuVuc.
    /// </summary>
    Task<ThongTinMucThuTheoKhuVucResponse?> LayMucThuTheoKhuVucAsync(string mucThuLePhiId, string khuVucApDungId, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 3: Lấy thông tin quyển biên lai và gợi ý số biên lai tiếp theo theo KyHieuQuyenBienLaiId.
    /// </summary>
    Task<SoBienLaiTiepTheoResponse?> LaySoBienLaiTiepTheoAsync(string kyHieuQuyenBienLaiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 4: Thêm mới hoặc Cập nhật phiếu Lập thủ tục &amp; Biên lai thu tiền (Xuất kho).
    /// Tự động cập nhật bản ghi trong KhoBienSo sang DaXuatKho = true, TrangThai = "DaBam" và cập nhật SoHienTai của quyển biên lai.
    /// </summary>
    Task<XuatKhoResponse> SetXuatKhoAsync(SetXuatKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 5: Lấy danh sách phiếu xuất kho có phân trang và bộ lọc.
    /// </summary>
    Task<PagedResult<XuatKhoResponse>> LayDanhSachXuatKhoAsync(PagedXuatKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 6: Lấy chi tiết một phiếu xuất kho theo Id.
    /// </summary>
    Task<XuatKhoResponse?> LayChiTietXuatKhoAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 7: Xóa phiếu xuất kho theo Id. Hoàn tác trạng thái biển số trong KhoBienSo về DaXuatKho = false, TrangThai = "DuocBam".
    /// </summary>
    Task<bool> XoaXuatKhoAsync(string id, CancellationToken cancellationToken = default);
}

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
    /// Cập nhật phiếu Lập thủ tục &amp; Biên lai xuất kho và đồng bộ toàn bộ bảng KhoBienSo theo dữ liệu mới.
    /// Đối với trường hợp đã xuất kho, cập nhật thông tin biển số trong KhoBienSo và xử lý hoàn tác/chuyển đổi biển số nếu thay đổi.
    /// </summary>
    Task<XuatKhoResponse> UpdateXuatKhoAsync(SetXuatKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác nhận xuất kho theo Id phiếu xuất kho.
    /// Khóa phiếu (DaXuatKho = true, TrangThai = 'HoanTat') và chuyển trạng thái biển số trong KhoBienSo sang 'DaBam' (Đã bấm / Đã cấp).
    /// </summary>
    Task<XuatKhoResponse> XacNhanXuatKhoAsync(string id, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// API 8: Hủy phiếu xuất kho theo Id.
    /// Cập nhật trạng thái phiếu xuất kho sang 'DaHuy', DaXuatKho = false và khôi phục trạng thái biển số trong KhoBienSo về DaXuatKho = false, TrangThai = 'DuocBam'.
    /// </summary>
    Task<XuatKhoResponse> HuyPhieuXuatKhoAsync(string id, HuyPhieuXuatKhoRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 9: Nhập xuất kho biển số xe tự động từ tệp tin Excel lưu trong thư mục StorageFolder.
    /// Tự động tạo các phiếu xuất kho với DaXuatKho = true và đánh dấu biển số trong KhoBienSo là đã xuất kho.
    /// Kiểm tra chặt chẽ các biển số đã có trong kho hay chưa, nếu chưa có thì báo danh sách chi tiết.
    /// </summary>
    Task<ImportXuatKhoExcelResponse> ImportXuatKhoTuExcelAsync(ImportXuatKhoExcelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 10: (GET) Lấy thông tin tệp tin Excel mẫu xuất kho biển số xe (MauBaoCao/XuatKhoBienSo.xlsx).
    /// Output trả về các tham số filename và path (mã hóa Base64) là input cho API tải file /api/FileUpload/download.
    /// </summary>
    Task<ExportMauXuatKhoResponse> ExportFileMauXuatKhoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy luồng dữ liệu byte[] của tệp tin mẫu xuất kho Excel để hỗ trợ tải file trực tiếp từ controller.
    /// </summary>
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadFileMauXuatKhoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// API 15: Nhập xuất kho biển số xe sử dụng AI để đọc tệp tin bất kỳ (Word, Excel, Image/PNG, PDF, Text).
    /// Không cố định mẫu, AI tự động nhận diện và trích xuất mọi trường dữ liệu có trong tài liệu.
    /// Đối soát biển số với bảng KhoBienSo: Đặt DaXuatKho = true và lưu thay đổi vào KhoBienSo đối với các biển số đã có trong kho.
    /// </summary>
    Task<ImportXuatKhoAiResponse> ImportXuatKhoTuFileAiAsync(ImportXuatKhoAiRequest request, CancellationToken cancellationToken = default);
}

using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý Đợt nhập kho và Kho biển số xe:
/// 1. (POST) Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
/// 2. (POST) Thêm mới và Update đợt nhập kho. Nếu update và DaNhapKho == true thì sẽ lưu kho (bỏ qua biển đấu giá).
/// 3. (GET) Lấy thông tin 1 đợt nhập kho theo Id.
/// 4. (GET) Xóa đợt nhập kho theo Id.
/// </summary>
public interface INhapKhoService
{
    /// <summary>
    /// 1. (POST) Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
    /// </summary>
    Task<PagedResult<DotNhapKhoResponse>> LayDanhSachDotNhapAsync(PagedDotNhapKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 2. (POST) Thêm mới và update đợt nhập kho.
    /// Nếu update và DaNhapKho == true thì sẽ lưu danh sách biển số vào bảng KhoBienSo (bỏ qua biển số LaBienDauGia == true).
    /// </summary>
    Task<DotNhapKhoResponse> SetDotNhapKhoAsync(SetDotNhapKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật dữ liệu đợt nhập kho và đồng bộ toàn bộ bảng KhoBienSo theo dữ liệu mới.
    /// Đối với các biển số đã xuất kho trước đó, giữ nguyên trạng thái xuất kho và đồng bộ dữ liệu sang phiếu xuất kho liên kết.
    /// </summary>
    Task<DotNhapKhoResponse> UpdateDotNhapKhoAsync(SetDotNhapKhoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác nhận nhập kho theo Id đợt nhập kho.
    /// Khóa phiếu (DaNhapKho = true, TrangThai = 'DaNhapKho') và chuyển toàn bộ biển số trong kho sang trạng thái 'DuocBam' (Biển được bấm / Tồn kho).
    /// </summary>
    Task<DotNhapKhoResponse> XacNhanNhapKhoAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 3. (GET) Lấy thông tin 1 đợt nhập kho theo Id.
    /// </summary>
    Task<DotNhapKhoResponse?> LayChiTietDotNhapAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 4. (GET) Xóa đợt nhập kho theo Id.
    /// </summary>
    Task<bool> XoaDotNhapKhoAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 5. (GET) Lấy thông tin tệp tin Excel mẫu nhập kho biển số xe (MauBaoCao/NhapKhoBienSo.xlsx).
    /// Output trả về thông tin (filename, path mã hóa Base64) làm input cho API tải file /api/FileUpload/download.
    /// </summary>
    Task<ExportMauNhapKhoResponse> ExportFileMauNhapKhoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 6. (POST) Đọc dữ liệu danh sách biển số xe từ file Excel mẫu theo FileName trong thư mục StorageFolder của appsettings.
    /// Output trả về List&lt;BienSoChiTietItemRequest&gt;.
    /// </summary>
    Task<List<BienSoChiTietItemRequest>> ImportExcelBienSoAsync(string fileName, CancellationToken cancellationToken = default);
}

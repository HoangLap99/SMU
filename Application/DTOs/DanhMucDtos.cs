using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

#region DTO Chung & Phân trang

/// <summary>
/// Yêu cầu lấy danh sách danh mục có phân trang, tìm kiếm và lọc theo Customer.
/// </summary>
public class PagedDanhMucRequest
{
    /// <summary>
    /// Từ khóa tìm kiếm theo Mã hoặc Tên danh mục.
    /// </summary>
    public string? KeyWord { get; set; }

    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị (nếu để trống, hệ thống sẽ tự động lấy theo đơn vị của user đang đăng nhập).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Lọc theo trạng thái hoạt động (true: đang dùng, false: ngừng dùng, null: tất cả).
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Số trang hiện tại (bắt đầu từ 1).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "CurrentPage phải lớn hơn hoặc bằng 1.")]
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi trên một trang.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "PageSize phải từ 1 đến 1000.")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Kết quả phân trang trả về cho client.
/// </summary>
public class PagedResult<T>
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
}

/// <summary>
/// Yêu cầu xóa nhiều bản ghi danh mục theo danh sách ID.
/// </summary>
public class DeleteDanhMucListRequest
{
    [Required(ErrorMessage = "Danh sách ID không được rỗng.")]
    public List<string> Ids { get; set; } = new();
}

/// <summary>
/// Yêu cầu import dữ liệu danh mục từ file Excel.
/// </summary>
public class ImportExcelRequest
{
    /// <summary>
    /// Tên file hoặc đường dẫn file Excel trên server (VD: file đã upload trong thư mục Uploads).
    /// </summary>
    [Required(ErrorMessage = "FileName không được để trống.")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Mã khách hàng / đơn vị đích (nếu rỗng, tự lấy theo đơn vị của user hiện tại).
    /// </summary>
    public string? CustomerId { get; set; }
}

/// <summary>
/// Kết quả thực thi import file Excel.
/// </summary>
public class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Yêu cầu xuất dữ liệu danh mục ra file Excel.
/// </summary>
public class ExportDanhMucRequest
{
    public string? KeyWord { get; set; }
    public string? CustomerId { get; set; }
    public bool? IsActive { get; set; }
}

#endregion

#region Base DTO cho Danh mục

/// <summary>
/// DTO phản hồi thông tin chung của các bản ghi danh mục.
/// </summary>
public abstract class BaseDanhMucResponse
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Ma { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int ThuTu { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime Modified { get; set; }
    public string? ModifiedByName { get; set; }
}

/// <summary>
/// DTO cơ sở cho API Set (Vừa thêm mới vừa cập nhật).
/// Nếu Id có giá trị và tồn tại => Cập nhật; Ngược lại => Thêm mới.
/// </summary>
public abstract class BaseSetDanhMucRequest
{
    /// <summary>
    /// Id của bản ghi (GUID). Để trống hoặc null nếu là thêm mới, truyền Id nếu là cập nhật.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Mã khách hàng / đơn vị. Nếu để trống sẽ tự động lấy đơn vị của user đang đăng nhập.
    /// </summary>
    public string? CustomerId { get; set; }

    [Required(ErrorMessage = "Mã danh mục không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã danh mục không được vượt quá 50 ký tự.")]
    public string Ma { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [StringLength(250, ErrorMessage = "Tên danh mục không được vượt quá 250 ký tự.")]
    public string Ten { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? MoTa { get; set; }

    public int ThuTu { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

#endregion

#region 1. Danh mục Đầu biển số (dau_bien_so)

public class DauBienSoResponse : BaseDanhMucResponse
{
    public string? KyHieuSeries { get; set; }
    public string? MaTinhThanh { get; set; }
}

public class SetDauBienSoRequest : BaseSetDanhMucRequest
{
    [StringLength(100, ErrorMessage = "Ký hiệu sê-ri không được vượt quá 100 ký tự.")]
    public string? KyHieuSeries { get; set; }

    [StringLength(50, ErrorMessage = "Mã tỉnh thành không được vượt quá 50 ký tự.")]
    public string? MaTinhThanh { get; set; }
}

#endregion

#region 2. Danh mục Màu biển số (mau_bien_so)

public class MauBienSoResponse : BaseDanhMucResponse
{
    public string? MaMauHex { get; set; }
    public string? DoiTuongSuDung { get; set; }
}

public class SetMauBienSoRequest : BaseSetDanhMucRequest
{
    [StringLength(20, ErrorMessage = "Mã màu Hex không được vượt quá 20 ký tự.")]
    public string? MaMauHex { get; set; }

    [StringLength(500, ErrorMessage = "Đối tượng sử dụng không được vượt quá 500 ký tự.")]
    public string? DoiTuongSuDung { get; set; }
}

#endregion

#region 3. Danh mục Trạng thái biển số (trang_thai_bien_so)

public class TrangThaiBienSoResponse : BaseDanhMucResponse
{
    public bool ChoPhepCapMoi { get; set; }
    public string? MauSacHienThi { get; set; }
}

public class SetTrangThaiBienSoRequest : BaseSetDanhMucRequest
{
    public bool ChoPhepCapMoi { get; set; } = false;

    [StringLength(50, ErrorMessage = "Màu sắc hiển thị không được vượt quá 50 ký tự.")]
    public string? MauSacHienThi { get; set; }
}

#endregion

#region 4. Danh mục Loại xe (loai_xe)

public class LoaiXeResponse : BaseDanhMucResponse
{
    public string? NhomXe { get; set; }
    public int? SoChoNgoiToiDa { get; set; }
    public double? TaiTrongToiDa { get; set; }
}

public class SetLoaiXeRequest : BaseSetDanhMucRequest
{
    [StringLength(100, ErrorMessage = "Nhóm xe không được vượt quá 100 ký tự.")]
    public string? NhomXe { get; set; }

    public int? SoChoNgoiToiDa { get; set; }
    public double? TaiTrongToiDa { get; set; }
}

#endregion

#region 5. Danh mục Loại thủ tục (loai_thu_tuc)

public class LoaiThuTucResponse : BaseDanhMucResponse
{
    public string? ThoiHanXuLy { get; set; }
    public decimal? LePhiMacDinh { get; set; }
    public string? CanCuPhapLy { get; set; }
}

public class SetLoaiThuTucRequest : BaseSetDanhMucRequest
{
    [StringLength(100, ErrorMessage = "Thời hạn xử lý không được vượt quá 100 ký tự.")]
    public string? ThoiHanXuLy { get; set; }

    public decimal? LePhiMacDinh { get; set; }

    [StringLength(500, ErrorMessage = "Căn cứ pháp lý không được vượt quá 500 ký tự.")]
    public string? CanCuPhapLy { get; set; }
}

#endregion

#region 6. Danh mục Khu vực áp dụng (khu_vuc_ap_dung)

public class KhuVucApDungResponse : BaseDanhMucResponse
{
    public string? PhamVi { get; set; }
    public double? HeSoPhi { get; set; }
}

public class SetKhuVucApDungRequest : BaseSetDanhMucRequest
{
    [StringLength(500, ErrorMessage = "Phạm vi không được vượt quá 500 ký tự.")]
    public string? PhamVi { get; set; }

    public double? HeSoPhi { get; set; }
}

#endregion

#region 7. Danh mục Ký hiệu quyển biên lai (ky_hieu_quyen_bien_lai)

public class KyHieuQuyenBienLaiResponse : BaseDanhMucResponse
{
    public long? TuSo { get; set; }
    public long? DenSo { get; set; }
    public long? SoHienTai { get; set; }
    public int? NamPhatHanh { get; set; }
    public string? DonViPhatHanh { get; set; }
}

public class SetKyHieuQuyenBienLaiRequest : BaseSetDanhMucRequest
{
    public long? TuSo { get; set; }
    public long? DenSo { get; set; }
    public long? SoHienTai { get; set; }
    public int? NamPhatHanh { get; set; }

    [StringLength(250, ErrorMessage = "Đơn vị phát hành không được vượt quá 250 ký tự.")]
    public string? DonViPhatHanh { get; set; }
}

#endregion

#region 8. Danh mục Tính chất đợt nhập (tinh_chat_dot_nhap)

public class TinhChatDotNhapResponse : BaseDanhMucResponse
{
    public string? LoaiDotNhap { get; set; }
}

public class SetTinhChatDotNhapRequest : BaseSetDanhMucRequest
{
    [StringLength(100, ErrorMessage = "Loại đợt nhập không được vượt quá 100 ký tự.")]
    public string? LoaiDotNhap { get; set; }
}

#endregion

#region 9. Danh mục Gói thuê bao (goi_thue_bao)

public class GoiThueBaoResponse : BaseDanhMucResponse
{
    /// <summary>
    /// Giới hạn số lượng user tối đa của gói thuê bao.
    /// </summary>
    public int GioiHanUser { get; set; }
}

public class SetGoiThueBaoRequest : BaseSetDanhMucRequest
{
    /// <summary>
    /// Giới hạn số lượng user tối đa của gói thuê bao.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Giới hạn user phải lớn hơn hoặc bằng 1.")]
    public int GioiHanUser { get; set; } = 1;
}

#endregion

#region 10. Danh mục Mức thu lệ phí (muc_thu_le_phi)

/// <summary>
/// DTO Chi tiết mức thu theo từng khu vực (dùng khi client gửi lên).
/// </summary>
public class MucThuKhuVucItemDto
{
    /// <summary>
    /// ID Khu vực áp dụng (chọn từ danh mục Khu vực áp dụng).
    /// </summary>
    [Required(ErrorMessage = "KhuVucApDungId không được để trống.")]
    public string KhuVucApDungId { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị mức thu lệ phí (VNĐ) nhập cho khu vực này.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Mức thu phải lớn hơn hoặc bằng 0.")]
    public decimal MucThu { get; set; }

    /// <summary>
    /// Ghi chú bổ sung (nếu có).
    /// </summary>
    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string? GhiChu { get; set; }
}

/// <summary>
/// DTO Chi tiết mức thu theo từng khu vực (dùng khi trả về cho client).
/// </summary>
public class MucThuKhuVucItemResponse
{
    public string? Id { get; set; }
    public string KhuVucApDungId { get; set; } = string.Empty;
    public string? MaKhuVuc { get; set; }
    public string? TenKhuVuc { get; set; }
    public decimal MucThu { get; set; }
    public string? GhiChu { get; set; }
}

public class MucThuLePhiResponse : BaseDanhMucResponse
{
    /// <summary>
    /// Nội dung thu lệ phí (tương đương Ten).
    /// </summary>
    public string NoiDungThuLePhi => Ten;

    /// <summary>
    /// ID Loại xe (chọn từ danh mục Loại xe).
    /// </summary>
    public string? LoaiXeId { get; set; }
    public string? MaLoaiXe { get; set; }
    public string? TenLoaiXe { get; set; }

    /// <summary>
    /// Tên nhóm xe.
    /// </summary>
    public string? NhomXe { get; set; }

    /// <summary>
    /// ID Loại thủ tục (chọn từ danh mục Loại thủ tục).
    /// </summary>
    public string? LoaiThuTucId { get; set; }
    public string? MaLoaiThuTuc { get; set; }
    public string? TenLoaiThuTuc { get; set; }

    /// <summary>
    /// Phân loại thủ tục.
    /// </summary>
    public string? PhanLoaiThuTuc { get; set; }

    /// <summary>
    /// Khoảng trị giá xe nếu có (Ví dụ: "Trên 40.000.000 đồng").
    /// </summary>
    public string? KhoangTriGiaXe { get; set; }

    /// <summary>
    /// Căn cứ pháp lý (Ví dụ: "Thông tư 155/2025/TT-BTC").
    /// </summary>
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Bắt buộc cán bộ phải chọn khoảng trị giá xe khi tính tiền.
    /// </summary>
    public bool BatBuocChonTriGiaXe { get; set; }

    /// <summary>
    /// Ghi chú hướng dẫn nghiệp vụ.
    /// </summary>
    public string? GhiChuHuongDan { get; set; }

    /// <summary>
    /// Bảng chi tiết mức thu theo từng khu vực đã chọn kèm giá trị.
    /// </summary>
    public List<MucThuKhuVucItemResponse> ChiTietMucThuKhuVuc { get; set; } = new();
}

public class SetMucThuLePhiRequest : BaseSetDanhMucRequest
{
    /// <summary>
    /// Nội dung thu lệ phí (nếu truyền vào sẽ tự động gán vào Ten nếu Ten rỗng).
    /// </summary>
    public string? NoiDungThuLePhi { get; set; }

    /// <summary>
    /// ID Loại xe (chọn từ danh mục Loại xe).
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Nhóm xe (tùy chọn, tự động điền theo Loại xe nếu để trống).
    /// </summary>
    [StringLength(100, ErrorMessage = "Nhóm xe không được vượt quá 100 ký tự.")]
    public string? NhomXe { get; set; }

    /// <summary>
    /// ID Loại thủ tục (chọn từ danh mục Loại thủ tục).
    /// </summary>
    public string? LoaiThuTucId { get; set; }

    /// <summary>
    /// Phân loại thủ tục (tùy chọn, tự động điền theo Loại thủ tục nếu để trống).
    /// </summary>
    [StringLength(150, ErrorMessage = "Phân loại thủ tục không được vượt quá 150 ký tự.")]
    public string? PhanLoaiThuTuc { get; set; }

    /// <summary>
    /// Khoảng trị giá xe nếu có.
    /// </summary>
    [StringLength(250, ErrorMessage = "Khoảng trị giá xe không được vượt quá 250 ký tự.")]
    public string? KhoangTriGiaXe { get; set; }

    /// <summary>
    /// Căn cứ pháp lý.
    /// </summary>
    [StringLength(250, ErrorMessage = "Căn cứ pháp lý không được vượt quá 250 ký tự.")]
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Bắt buộc cán bộ phải chọn khoảng trị giá xe khi tính tiền.
    /// </summary>
    public bool BatBuocChonTriGiaXe { get; set; } = false;

    /// <summary>
    /// Ghi chú hướng dẫn nghiệp vụ.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Ghi chú hướng dẫn nghiệp vụ không được vượt quá 1000 ký tự.")]
    public string? GhiChuHuongDan { get; set; }

    /// <summary>
    /// Bảng chọn mức thu theo từng khu vực (1 cột chọn khu vực, 1 cột nhập giá trị).
    /// </summary>
    public List<MucThuKhuVucItemDto> ChiTietMucThuKhuVuc { get; set; } = new();
}

#endregion

#region 12. Danh mục Cấp bậc (CapBac)

public class CapBacResponse : BaseDanhMucResponse
{
    public string? NhomCapBac { get; set; }
    public double? HeSoThamChieu { get; set; }
}

public class SetCapBacRequest : BaseSetDanhMucRequest
{
    [StringLength(100, ErrorMessage = "Nhóm cấp bậc không được vượt quá 100 ký tự.")]
    public string? NhomCapBac { get; set; }

    public double? HeSoThamChieu { get; set; }
}

#endregion

#region 13. Danh mục Vai trò phân quyền (VaiTroPhanQuyen)

public class VaiTroPhanQuyenResponse : BaseDanhMucResponse
{
    public string? QuyenHan { get; set; }
    public bool IsAdmin { get; set; }
}

public class SetVaiTroPhanQuyenRequest : BaseSetDanhMucRequest
{
    [StringLength(2000, ErrorMessage = "Quyền hạn không được vượt quá 2000 ký tự.")]
    public string? QuyenHan { get; set; }

    public bool IsAdmin { get; set; } = false;
}

#endregion



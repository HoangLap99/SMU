using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

/// <summary>
/// Yêu cầu Thêm mới hoặc Cập nhật biên lai cấp lại đăng ký xe (Hỗ trợ Lưu nháp hoặc Xác nhận phát hành).
/// </summary>
public class SetCapLaiDangKyXeRequest
{
    /// <summary>
    /// ID biên lai cấp lại đăng ký xe (để trống khi thêm mới; truyền khi cập nhật bản ghi đang ở trạng thái Lưu nháp).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị thực hiện (nếu không truyền tự động lấy đơn vị của người dùng hiện tại).
    /// </summary>
    public string? CustomerId { get; set; }

    #region 1. Thông tin số biên lai & Ngày thu

    /// <summary>
    /// ID Danh mục Ký hiệu quyển biên lai (KyHieuQuyenBienLai).
    /// </summary>
    
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;

    /// <summary>
    /// Số biên lai thu tiền (Ví dụ: "0000113").
    /// </summary>
   
    public string SoBienLai { get; set; } = string.Empty;

    /// <summary>
    /// Ngày lập biên lai.
    /// </summary>
    public DateTime NgayBienLai { get; set; } = DateTime.UtcNow;

    #endregion

    #region 2. Phương tiện & Biển số xe liên quan

    /// <summary>
    /// Biển số xe cần cấp lại đăng ký (Ví dụ: "30A-123.45" hoặc "29E-895.02").
    /// </summary>
   
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// ID Loại xe (chọn từ danh mục LoaiXe, tùy chọn).
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Loại phương tiện: "Ô tô", "Mô tô, Xe máy", "Xe máy điện", "Khác"...
    /// </summary>
   
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    #endregion

    #region 3. Nội dung thu & Mức tiền theo TT 155/2025/TT-BTC

    /// <summary>
    /// ID Danh mục Mức thu lệ phí (MucThuLePhi, tùy chọn).
    /// </summary>
    public string? MucThuLePhiId { get; set; }

    /// <summary>
    /// Nội dung thu phí (Ví dụ: "Cấp lại riêng giấy chứng nhận đăng ký xe (mất, hư hỏng - không cấp biển mới) (50.000 đ)").
    /// </summary>
    
    public string NoiDungThuPhi { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền thu (VNĐ). Mặc định 50.000đ theo Thông tư 155/2025/TT-BTC.
    /// </summary>
    [Range(0, 1000000000, ErrorMessage = "Số tiền thu phải lớn hơn hoặc bằng 0.")]
    public decimal SoTienThu { get; set; } = 50000;

    /// <summary>
    /// Hình thức thu tiền (Ví dụ: "Tiền mặt", "Chuyển khoản"). Mặc định: "Tiền mặt".
    /// </summary>
    public string HinhThucThuTien { get; set; } = "Tiền mặt";

    #endregion

    #region 4. Thông tin chủ xe / Người nộp

    /// <summary>
    /// Họ và tên chủ xe / người nộp tiền (Ví dụ: "Nguyễn Văn A").
    /// </summary>
    
    public string HoTenChuXe { get; set; } = string.Empty;

    /// <summary>
    /// Số CCCD / Mã định danh cá nhân của chủ xe / người nộp.
    /// </summary>
    public string? SoCccd { get; set; }

    /// <summary>
    /// Mã hồ sơ Dịch vụ công (nếu có, ví dụ: "HS-2026-00123").
    /// </summary>
    public string? MaHoSoDvc { get; set; }

    /// <summary>
    /// Ghi chú nghiệp vụ / Lý do cấp lại (Ví dụ: "Cấp lại chứng nhận đăng ký xe bị mất/hỏng theo đơn").
    /// </summary>
    public string? LyDoCapLai { get; set; }

    #endregion

    #region 5. Tùy chọn Xác nhận hoàn tất (Lưu nháp vs Phát hành biên lai)

    /// <summary>
    /// Cờ xác nhận:
    /// - false (hoặc null): Lưu nháp (TrangThai = "LuuNhap"), cho phép tiếp tục chỉnh sửa.
    /// - true: Lưu &amp; Phát hành biên lai ngay (TrangThai = "DaXacNhan"), KHÓA hoàn toàn không cho sửa hay xóa nữa.
    /// </summary>
    public bool? XacNhanHoanTat { get; set; } = false;

    #endregion
}

/// <summary>
/// Yêu cầu xác nhận hoàn tất biên lai cấp lại đăng ký xe (chuyển từ Lưu nháp sang Đã xác nhận / Khóa phiếu).
/// </summary>
public class XacNhanCapLaiDangKyXeRequest
{
    /// <summary>
    /// ID biên lai cấp lại đăng ký xe cần xác nhận.
    /// </summary>
    [Required(ErrorMessage = "Vui lòng cung cấp ID biên lai cần xác nhận.")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin phản hồi chi tiết của biên lai cấp lại đăng ký xe.
/// </summary>
public class CapLaiDangKyXeResponse
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }

    // Thông tin số biên lai
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;
    public string KyHieuQuyen { get; set; } = string.Empty;
    public string SoBienLai { get; set; } = string.Empty;
    public DateTime NgayBienLai { get; set; }

    // Phương tiện & Biển số
    public string BienKiemSoat { get; set; } = string.Empty;
    public string? LoaiXeId { get; set; }
    public string LoaiPhuongTien { get; set; } = string.Empty;

    // Lệ phí
    public string? MucThuLePhiId { get; set; }
    public string NoiDungThuPhi { get; set; } = string.Empty;
    public decimal SoTienThu { get; set; }
    public string HinhThucThuTien { get; set; } = "Tiền mặt";

    // Chủ xe / Người nộp
    public string HoTenChuXe { get; set; } = string.Empty;
    public string? SoCccd { get; set; }
    public string? MaHoSoDvc { get; set; }
    public string? LyDoCapLai { get; set; }

    // Trạng thái khóa
    public string TrangThai { get; set; } = "LuuNhap";
    public string TrangThaiTen => TrangThai switch
    {
        "DaXacNhan" => "Đã xác nhận (Đã phát hành)",
        "LuuNhap" => "Bản nháp",
        "DaHuy" => "Đã hủy",
        _ => TrangThai
    };
    public bool DaXacNhan { get; set; }
    public DateTime? NgayXacNhan { get; set; }
    public string? NguoiXacNhan { get; set; }

    // Audit trail
    public DateTime Created { get; set; }
    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime Modified { get; set; }
    public string? ModifileById { get; set; }
    public string? ModifileByName { get; set; }
}

/// <summary>
/// Yêu cầu lọc và phân trang danh sách biên lai cấp lại đăng ký xe.
/// </summary>
public class PagedCapLaiDangKyXeRequest
{
    public int CurrentPage { get; set; } = 1;
    public int PageIndex { get => CurrentPage; set => CurrentPage = value; }
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Từ khóa tìm kiếm: Biển số, Họ tên chủ xe, CCCD, Mã hồ sơ DVC, Số biên lai.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị (nếu không truyền sẽ lấy theo người dùng đăng nhập).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Lọc theo trạng thái ("LuuNhap", "DaXacNhan", "DaHuy"). Để trống lấy tất cả.
    /// </summary>
    public string? TrangThai { get; set; }

    /// <summary>
    /// Lọc theo loại phương tiện (Ô tô, Xe máy...).
    /// </summary>
    public string? LoaiPhuongTien { get; set; }

    /// <summary>
    /// Lọc theo Ký hiệu quyển biên lai.
    /// </summary>
    public string? KyHieuQuyenBienLaiId { get; set; }

    /// <summary>
    /// Lọc từ ngày biên lai.
    /// </summary>
    public DateTime? TuNgay { get; set; }

    /// <summary>
    /// Lọc đến ngày biên lai.
    /// </summary>
    public DateTime? DenNgay { get; set; }
}

/// <summary>
/// Yêu cầu import biên lai Cấp lại đăng ký xe từ tệp Excel (mẫu MauBaoCao/CapLaiBienDangKy.xlsx).
/// Lưu ý: Biển số không lấy trong kho, không thay đổi dữ liệu trong kho, mặc định DaXacNhan = true.
/// </summary>
public class ImportCapLaiDangKyXeRequest
{
    /// <summary>
    /// Tên tệp tin Excel mẫu đã lưu trong thư mục StorageFolder hoặc thư mục MauBaoCao (Ví dụ: "CapLaiBienDangKy.xlsx").
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Tùy chọn upload tệp trực tiếp từ máy khách (dạng multipart/form-data).
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị thực hiện (nếu để trống tự động lấy theo tài khoản đăng nhập).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Tùy chọn: ID Loại xe mặc định áp dụng cho các dòng nếu file không chỉ định.
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Tùy chọn: Loại phương tiện mặc định (Mặc định: "Ô tô").
    /// </summary>
    public string? LoaiPhuongTien { get; set; } = "Ô tô";

    /// <summary>
    /// Trạng thái xác nhận phát hành biên lai. Mặc định là true (DaXacNhan = true, TrangThai = 'DaXacNhan').
    /// </summary>
    public bool DaXacNhan { get; set; } = true;

    /// <summary>
    /// Tùy chọn: Nếu đặt là true, hệ thống chỉ kiểm tra tính hợp lệ và đọc thử dữ liệu mà không thực hiện lưu vào CSDL (dry-run).
    /// </summary>
    public bool? ChiKiemTra { get; set; } = false;
}

/// <summary>
/// Kết quả phản hồi sau khi import dữ liệu Cấp lại đăng ký xe từ tệp Excel.
/// </summary>
public class ImportCapLaiDangKyXeResponse
{
    /// <summary>
    /// Thông báo tóm tắt kết quả import.
    /// </summary>
    public string ThongBao { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số dòng dữ liệu đọc được từ tệp Excel.
    /// </summary>
    public int TongSoDong { get; set; }

    /// <summary>
    /// Số lượng biên lai cấp lại đăng ký xe được tạo thành công.
    /// </summary>
    public int SoLuongThanhCong { get; set; }

    /// <summary>
    /// Số lượng dòng bị bỏ qua (trùng lặp trong tệp, thiếu thông tin bắt buộc...).
    /// </summary>
    public int SoLuongBoQua { get; set; }

    /// <summary>
    /// Số lượng dòng bị lỗi khi xử lý.
    /// </summary>
    public int SoLuongLoi { get; set; }

    /// <summary>
    /// Tổng số tiền lệ phí thu được từ các biên lai import thành công (VNĐ).
    /// </summary>
    public decimal TongTienThu { get; set; }

    /// <summary>
    /// Danh sách các biển kiểm soát đã import cấp lại thành công.
    /// </summary>
    public List<string> DanhSachBienSoThanhCong { get; set; } = [];

    /// <summary>
    /// Danh sách chi tiết các dòng bị lỗi hoặc bỏ qua trong quá trình import.
    /// </summary>
    public List<ImportCapLaiLoiItem> DanhSachLoi { get; set; } = [];
}

/// <summary>
/// Chi tiết dòng bị lỗi hoặc bỏ qua trong quá trình import Excel cấp lại đăng ký xe.
/// </summary>
public class ImportCapLaiLoiItem
{
    /// <summary>
    /// Số thứ tự dòng trong tệp Excel.
    /// </summary>
    public int Dong { get; set; }

    /// <summary>
    /// Biển kiểm soát đọc được tại dòng đó (nếu có).
    /// </summary>
    public string? BienKiemSoat { get; set; }

    /// <summary>
    /// Lý do lỗi hoặc bỏ qua.
    /// </summary>
    public string LyDoLoi { get; set; } = string.Empty;
}

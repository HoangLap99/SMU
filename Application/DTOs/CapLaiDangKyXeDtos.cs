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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Lập biên lai cấp lại đăng ký xe (Thủ tục cấp lại giấy đăng ký xe không xuất kho, không giảm tồn biển).
/// Bảng lưu trữ: CapLaiDangKyXe
/// </summary>
[Table("CapLaiDangKyXe")]
public class CapLaiDangKyXe : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị thực hiện thủ tục.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    #region 1. Thông tin số biên lai & Ngày thu

    /// <summary>
    /// ID Danh mục Ký hiệu quyển biên lai (KyHieuQuyenBienLai).
    /// </summary>
  
    [StringLength(36)]
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;

    /// <summary>
    /// Ký hiệu quyển biên lai (Snapshot, ví dụ: "AA/26E").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string KyHieuQuyen { get; set; } = string.Empty;

    /// <summary>
    /// Số biên lai thu tiền (Ví dụ: "0000113").
    /// </summary>
    
    [StringLength(50)]
    public string SoBienLai { get; set; } = string.Empty;

    /// <summary>
    /// Ngày lập biên lai.
    /// </summary>
    public DateTime NgayBienLai { get; set; } = DateTime.UtcNow;

    #endregion

    #region 2. Phương tiện & Biển số xe liên quan

    /// <summary>
    /// Biển số xe cần cấp lại đăng ký (Ví dụ: "30A-123.45" hoặc "29E-895.02").
    /// Không thực hiện xuất kho và không làm thay đổi trạng thái tồn kho của biển số này.
    /// </summary>
   
    [StringLength(50)]
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// ID Loại xe (chọn từ danh mục LoaiXe, tùy chọn).
    /// </summary>
    [StringLength(36)]
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Loại phương tiện: "Ô tô", "Mô tô, Xe máy", "Xe máy điện", "Khác"...
    /// </summary>
    
    [StringLength(100)]
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    #endregion

    #region 3. Nội dung thu & Mức tiền theo TT 155/2025/TT-BTC

    /// <summary>
    /// ID Danh mục Mức thu lệ phí (MucThuLePhi, tùy chọn).
    /// </summary>
    [StringLength(36)]
    public string? MucThuLePhiId { get; set; }

    /// <summary>
    /// Nội dung thu phí (Ví dụ: "Cấp lại riêng giấy chứng nhận đăng ký xe (mất, hư hỏng - không cấp biển mới) (50.000 đ)").
    /// </summary>
  
    [StringLength(500)]
    public string NoiDungThuPhi { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền thu (VNĐ). Mặc định theo biểu phí là 50.000đ.
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal SoTienThu { get; set; } = 50000;

    /// <summary>
    /// Hình thức thu tiền (Ví dụ: "Tiền mặt", "Chuyển khoản"). Mặc định: "Tiền mặt".
    /// </summary>
    [StringLength(50)]
    public string HinhThucThuTien { get; set; } = "Tiền mặt";

    #endregion

    #region 4. Thông tin chủ xe / Người nộp

    /// <summary>
    /// Họ và tên chủ xe / người nộp tiền (Ví dụ: "Nguyễn Văn A").
    /// </summary>
   
    [StringLength(250)]
    public string HoTenChuXe { get; set; } = string.Empty;

    /// <summary>
    /// Số CCCD / Mã định danh cá nhân của chủ xe / người nộp.
    /// </summary>
    [StringLength(50)]
    public string? SoCccd { get; set; }

    /// <summary>
    /// Mã hồ sơ Dịch vụ công (nếu có, ví dụ: "HS-2026-00123").
    /// </summary>
    [StringLength(100)]
    public string? MaHoSoDvc { get; set; }

    /// <summary>
    /// Ghi chú nghiệp vụ / Lý do cấp lại (Ví dụ: "Cấp lại chứng nhận đăng ký xe bị mất/hỏng theo đơn").
    /// </summary>
    [StringLength(1000)]
    public string? LyDoCapLai { get; set; }

    #endregion

    #region 5. Trạng thái & Xác nhận khóa

    /// <summary>
    /// Trạng thái của biên lai:
    /// - "LuuNhap": Bản nháp, cho phép chỉnh sửa và xóa.
    /// - "DaXacNhan": Đã xác nhận / phát hành biên lai, KHÓA hoàn toàn không cho sửa hay xóa nữa.
    /// - "DaHuy": Đã hủy biên lai.
    /// </summary>
    [StringLength(50)]
    public string TrangThai { get; set; } = "LuuNhap";

    /// <summary>
    /// Đánh dấu đã xác nhận / phát hành biên lai (true: Đã khóa, không cho sửa hay xóa).
    /// </summary>
    public bool DaXacNhan { get; set; } = false;

    /// <summary>
    /// Thời điểm bấm xác nhận / phát hành biên lai.
    /// </summary>
    public DateTime? NgayXacNhan { get; set; }

    /// <summary>
    /// Họ tên hoặc Username người bấm xác nhận.
    /// </summary>
    [StringLength(250)]
    public string? NguoiXacNhan { get; set; }

    #endregion
}

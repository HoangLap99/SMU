using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể lưu trữ lịch sử các lần đăng ký mới, gia hạn, nâng cấp hoặc đổi gói thuê bao của Khách hàng / Đơn vị (Customer).
/// Bảng lưu trữ: LichSuGoiThueBao
/// </summary>
[Table("LichSuGoiThueBao")]
public class LichSuGoiThueBao : Audit
{
    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Mã khách hàng tại thời điểm thực hiện (VD: CUS-001, CAP_TAYMO).
    /// </summary>
    [StringLength(50)]
    public string? CustomerCode { get; set; }

    /// <summary>
    /// Tên khách hàng tại thời điểm thực hiện (VD: Công an phường Tây Mỗ).
    /// </summary>
    [StringLength(250)]
    public string? CustomerName { get; set; }

    /// <summary>
    /// Mã định danh Gói thuê bao được áp dụng.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string GoiThueBaoId { get; set; } = string.Empty;

    /// <summary>
    /// Mã gói thuê bao (VD: CO_BAN, NANG_CAO, DOANH_NGHIEP).
    /// </summary>
    [StringLength(50)]
    public string? GoiThueBaoMa { get; set; }

    /// <summary>
    /// Tên gói thuê bao tại thời điểm thực hiện (VD: Gói Cơ bản, Gói Nâng cao).
    /// </summary>
    [StringLength(250)]
    public string GoiThueBaoTen { get; set; } = string.Empty;

    /// <summary>
    /// Định mức số tài khoản cán bộ tối đa được phép hoạt động theo gói (Quota).
    /// </summary>
    public int GioiHanUser { get; set; } = 1;

    /// <summary>
    /// Loại giao dịch: "DangKyMoi" (Đăng ký mới), "GiaHan" (Gia hạn), "NangCapGoi" (Nâng cấp gói), "HaCapGoi" (Hạ cấp gói), "DoiGoi" (Đổi gói).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LoaiGiaoDich { get; set; } = "GiaHan";

    /// <summary>
    /// Tên hiển thị của loại giao dịch: "Đăng ký mới", "Gia hạn gói thuê bao", "Nâng cấp gói"...
    /// </summary>
    [StringLength(100)]
    public string? LoaiGiaoDichTen { get; set; }

    /// <summary>
    /// Hình thức gia hạn: "Theo tháng", "Theo năm", "Tùy chọn".
    /// </summary>
    [StringLength(100)]
    public string? HinhThucGiaHan { get; set; }

    /// <summary>
    /// Số tháng đăng ký / gia hạn (VD: 1, 3, 6, 12, 24, 36).
    /// </summary>
    public int? SoThang { get; set; }

    /// <summary>
    /// Ngày bắt đầu hiệu lực của kỳ này.
    /// </summary>
    public DateTime NgayBatDau { get; set; }

    /// <summary>
    /// Ngày hết hạn mới sau khi đăng ký / gia hạn.
    /// </summary>
    public DateTime NgayKetThuc { get; set; }

    /// <summary>
    /// Ngày hết hạn cũ trước khi thực hiện gia hạn (để đối chiếu).
    /// </summary>
    public DateTime? NgayKetThucCu { get; set; }

    /// <summary>
    /// Số tiền thanh toán / giá trị hợp đồng (VNĐ).
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal? SoTien { get; set; }

    /// <summary>
    /// Số hợp đồng / Mã biên lai / Mã hóa đơn điện tử liên quan.
    /// </summary>
    [StringLength(100)]
    public string? SoHopDong { get; set; }

    /// <summary>
    /// Ghi chú / Diễn giải lý do gia hạn hoặc điều chỉnh.
    /// </summary>
    [StringLength(1000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Trạng thái của lần đăng ký/gia hạn: "HieuLuc", "HetHan", "DaHuy".
    /// </summary>
    [StringLength(50)]
    public string TrangThai { get; set; } = "HieuLuc";

    /// <summary>
    /// Tên người thực hiện thao tác gia hạn / phê duyệt.
    /// </summary>
    [StringLength(250)]
    public string? NguoiThucHien { get; set; }
}

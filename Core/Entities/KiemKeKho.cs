using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Phiếu kiểm kê kho biển số xe.
/// Bảng lưu trữ: KiemKeKho
/// </summary>
[Table("KiemKeKho")]
public class KiemKeKho : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị thực hiện kiểm kê.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Mã phiếu kiểm kê (Ví dụ: "KK-2026-10-10-921").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string MaPhieuKiemKe { get; set; } = string.Empty;

    /// <summary>
    /// Ngày thực hiện kiểm kê (Ví dụ: 10/10/2026).
    /// </summary>
    public DateTime NgayKiemKe { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian kiểm kê (Ví dụ: "17 giờ 15 phút" hoặc "17:15").
    /// </summary>
    [StringLength(100)]
    public string? ThoiGianKiemKe { get; set; }

    /// <summary>
    /// ID Danh mục Loại xe / Loại phương tiện kiểm kê.
    /// </summary>
    [StringLength(36)]
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Tên loại phương tiện kiểm kê: "Ô tô", "Xe máy", "Rơ moóc".
    /// </summary>
    [StringLength(100)]
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    /// <summary>
    /// Địa điểm kiểm kê (Ví dụ: "Tổ đăng ký xe, Công an phường Nghĩa Đô").
    /// </summary>
    [StringLength(500)]
    public string? DiaDiemKiemKe { get; set; }

    /// <summary>
    /// Trưởng đoàn kiểm kê (Ví dụ: "Thiếu tá Bùi Văn Kiên").
    /// </summary>
    [StringLength(250)]
    public string? TruongDoanKiemKe { get; set; }

    /// <summary>
    /// Cán bộ lập biên bản (Ví dụ: "Bùi Văn Kiên").
    /// </summary>
    [StringLength(250)]
    public string? CanBoLapBienBan { get; set; }

    /// <summary>
    /// Tổng số lượng biển số tồn hệ thống (sổ sách phần mềm).
    /// </summary>
    public int TongTonHeThong { get; set; } = 0;

    /// <summary>
    /// Tổng số lượng biển số kiểm đếm thực tế.
    /// </summary>
    public int TongThucTe { get; set; } = 0;

    /// <summary>
    /// Tổng chênh lệch (Thực tế - Tồn hệ thống). 0 là khớp sổ sách.
    /// </summary>
    public int TongChenhLech { get; set; } = 0;

    /// <summary>
    /// Trạng thái của phiếu kiểm kê:
    /// - "LuuNhap": Lưu nháp bản ghi, cho phép tiếp tục chỉnh sửa số lượng thực tế &amp; đối chiếu.
    /// - "HoanTat": Xác nhận hoàn tất, chốt số liệu kiểm kê.
    /// </summary>
    [StringLength(50)]
    public string TrangThai { get; set; } = "LuuNhap";

    /// <summary>
    /// Ghi chú chung của phiếu kiểm kê.
    /// </summary>
    [StringLength(2000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Danh sách chi tiết các nhóm dải đầu biển được kiểm kê.
    /// </summary>
    public virtual ICollection<KiemKeKhoNhomDauBien> DanhSachNhomDauBien { get; set; } = new List<KiemKeKhoNhomDauBien>();

    /// <summary>
    /// Danh sách chi tiết các biển số thừa / thiếu trong phiếu kiểm kê.
    /// </summary>
    public virtual ICollection<KiemKeKhoChiTietBien> DanhSachChiTietBien { get; set; } = new List<KiemKeKhoChiTietBien>();
}

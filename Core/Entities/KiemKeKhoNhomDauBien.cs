using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Dòng chi tiết nhóm dải đầu biển trong phiếu kiểm kê kho.
/// Bảng lưu trữ: KiemKeKhoNhomDauBien
/// </summary>
[Table("KiemKeKhoNhomDauBien")]
public class KiemKeKhoNhomDauBien : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// ID Phiếu kiểm kê kho tương ứng.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string KiemKeKhoId { get; set; } = string.Empty;

    /// <summary>
    /// Số thứ tự dòng trong bảng kiểm kê.
    /// </summary>
    public int STT { get; set; } = 1;

    /// <summary>
    /// Ký hiệu dải đầu biển (Ví dụ: "30B-207", "30B-369", "29E-794").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// ID Danh mục Màu biển số.
    /// </summary>
    [StringLength(36)]
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Tên màu biển (Ví dụ: "Biển Trắng", "Biển Vàng").
    /// </summary>
    [StringLength(100)]
    public string MauBien { get; set; } = "Biển Trắng";

    /// <summary>
    /// Mã màu hex hiển thị badge màu biển.
    /// </summary>
    [StringLength(20)]
    public string? MauBienHex { get; set; }

    /// <summary>
    /// Số lượng tồn hệ thống (sổ sách phần mềm).
    /// </summary>
    public int TonHeThong { get; set; } = 0;

    /// <summary>
    /// Số lượng kiểm đếm thực tế.
    /// </summary>
    public int ThucTe { get; set; } = 0;

    /// <summary>
    /// Chênh lệch (Thực tế - Tồn hệ thống).
    /// </summary>
    public int ChenhLech { get; set; } = 0;

    /// <summary>
    /// Số lượng biển có mặt trong kho.
    /// </summary>
    public int SoBienCoMat { get; set; } = 0;

    /// <summary>
    /// Số lượng biển báo thiếu.
    /// </summary>
    public int SoBienThieu { get; set; } = 0;

    /// <summary>
    /// Số lượng biển phát hiện thừa thực tế (chưa có trong phần mềm).
    /// </summary>
    public int SoBienThua { get; set; } = 0;

    /// <summary>
    /// Ghi chú của dòng dải biển (Ví dụ: "Khớp sổ sách", "Thừa 1 biển").
    /// </summary>
    [StringLength(1000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Navigation property tới phiếu kiểm kê cha.
    /// </summary>
    [ForeignKey("KiemKeKhoId")]
    public virtual KiemKeKho? KiemKeKho { get; set; }
}

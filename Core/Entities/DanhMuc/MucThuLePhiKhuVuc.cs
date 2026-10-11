using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Chi tiết mức thu lệ phí theo từng Khu vực áp dụng (Bảng chọn khu vực và nhập giá trị).
/// Bảng lưu trữ: MucThuLePhikhuVuc
/// </summary>
[Table("MucThuLePhikhuVuc")]
public class MucThuLePhiKhuVuc : Audit
{
    /// <summary>
    /// ID Mức thu lệ phí cha.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string MucThuLePhiId { get; set; } = string.Empty;


    /// <summary>
    /// ID Khu vực áp dụng (chọn từ danh mục Khu vực áp dụng).
    /// </summary>
    [Required]
    [StringLength(36)]
    public string KhuVucApDungId { get; set; } = string.Empty;


  

    /// <summary>
    /// Giá trị mức thu lệ phí (VNĐ) nhập cho khu vực này.
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal MucThu { get; set; }

    /// <summary>
    /// Ghi chú bổ sung (nếu có).
    /// </summary>
    [StringLength(500)]
    public string? GhiChu { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Khu vực áp dụng (Ví dụ: Khu vực I - HN/HCM, Khu vực II - TP trực thuộc tỉnh, Khu vực III...).
/// Bảng lưu trữ: KhuVucApDung
/// </summary>
[Table("KhuVucApDung")]
public class KhuVucApDung : DanhMucBase
{
    /// <summary>
    /// Phạm vi địa lý / địa bàn hành chính áp dụng.
    /// </summary>
    [StringLength(500)]
    public string? PhamVi { get; set; }

    /// <summary>
    /// Hệ số phân vùng hoặc tính lệ phí (nếu có).
    /// </summary>
    public double? HeSoPhi { get; set; }
}

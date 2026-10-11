using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Loại xe (Ví dụ: Ô tô con, Ô tô tải, Xe khách, Mô tô 2 bánh, Xe chuyên dùng...).
/// Bảng lưu trữ: LoaiXe
/// </summary>
[Table("LoaiXe")]
public class LoaiXe : DanhMucBase
{
    /// <summary>
    /// Nhóm phương tiện lớn (Ví dụ: "Ô tô", "Mô tô", "Xe máy chuyên dùng", "Máy kéo", "Rơ moóc").
    /// </summary>
    [StringLength(100)]
    public string? NhomXe { get; set; }

    /// <summary>
    /// Số chỗ ngồi tối đa thiết kế (nếu có).
    /// </summary>
    public int? SoChoNgoiToiDa { get; set; }

    /// <summary>
    /// Tải trọng thiết kế tối đa tính bằng tấn (nếu có).
    /// </summary>
    public double? TaiTrongToiDa { get; set; }
}

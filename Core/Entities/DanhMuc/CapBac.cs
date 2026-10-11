using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Cấp bậc Công an nhân dân (Ví dụ: Thiếu úy, Trung úy, Thượng úy, Đại úy, Thiếu tá, Trung tá, Thượng tá, Đại tá...).
/// Bảng lưu trữ: CapBac
/// </summary>
[Table("CapBac")]
public class CapBac : DanhMucBase
{
    /// <summary>
    /// Nhóm cấp bậc (Ví dụ: "Sĩ quan cấp tướng", "Sĩ quan cấp tá", "Sĩ quan cấp úy", "Hạ sĩ quan", "Chiến sĩ").
    /// </summary>
    [StringLength(100)]
    public string? NhomCapBac { get; set; }

    /// <summary>
    /// Hệ số phụ cấp thâm niên hoặc hệ số lương tham chiếu (nếu có).
    /// </summary>
    public double? HeSoThamChieu { get; set; }
}

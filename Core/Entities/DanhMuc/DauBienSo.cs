using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Đầu biển số (Ví dụ: 29, 30, 51, 60...).
/// Bảng lưu trữ: DauBienSo
/// </summary>
[Table("DauBienSo")]
public class DauBienSo : DanhMucBase
{
    /// <summary>
    /// Ký hiệu sê-ri biển số (Ví dụ: "A, B, C", "NN", "NG", "LD"...).
    /// </summary>
    [StringLength(100)]
    public string? KyHieuSeries { get; set; }

    /// <summary>
    /// Mã hành chính của tỉnh / thành phố tương ứng (Ví dụ: "01" cho Hà Nội, "79" cho TP.HCM).
    /// </summary>
    [StringLength(50)]
    public string? MaTinhThanh { get; set; }
}

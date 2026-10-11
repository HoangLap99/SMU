using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Tính chất đợt nhập (Ví dụ: Định kỳ, Đột xuất, Bổ sung, Điều chuyển...).
/// Bảng lưu trữ: TinhChatDotNhap
/// </summary>
[Table("TinhChatDotNhap")]
public class TinhChatDotNhap : DanhMucBase
{
    /// <summary>
    /// Phân loại nhóm đợt nhập (Ví dụ: "Định kỳ theo kế hoạch", "Đột xuất", "Điều chuyển nội bộ").
    /// </summary>
    [StringLength(100)]
    public string? LoaiDotNhap { get; set; }
}

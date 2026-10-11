using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Màu biển số (Ví dụ: Biển trắng, Biển xanh, Biển vàng, Biển đỏ...).
/// Bảng lưu trữ: MauBienSo
/// </summary>
[Table("MauBienSo")]
public class MauBienSo : DanhMucBase
{
    /// <summary>
    /// Mã màu HEX đại diện hiển thị trên giao diện (Ví dụ: "#FFFFFF", "#0055D4", "#FFCC00", "#D32F2F").
    /// </summary>
    [StringLength(20)]
    public string? MaMauHex { get; set; }

    /// <summary>
    /// Đối tượng áp dụng sử dụng màu biển số này (Ví dụ: "Cá nhân và doanh nghiệp trong nước", "Cơ quan nhà nước", "Xe kinh doanh vận tải").
    /// </summary>
    [StringLength(500)]
    public string? DoiTuongSuDung { get; set; }
}

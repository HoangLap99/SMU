using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Gói thuê bao (Ví dụ: Gói Cơ bản, Gói Nâng cao, Gói Doanh nghiệp...).
/// Bảng lưu trữ: GoiThueBao
/// </summary>
[Table("GoiThueBao")]
public class GoiThueBao : DanhMucBase
{
    /// <summary>
    /// Giới hạn số lượng tài khoản người dùng (User) tối đa cho phép trong gói thuê bao.
    /// </summary>
    public int GioiHanUser { get; set; } = 1;
}

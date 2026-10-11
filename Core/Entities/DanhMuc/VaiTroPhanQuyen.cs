using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Vai trò phân quyền cán bộ trong đơn vị (Ví dụ: Quản trị đơn vị (Toàn quyền), Cán bộ nghiệp vụ (Nhập/Xuất), Cán bộ xem báo cáo...).
/// Bảng lưu trữ: VaiTroPhanQuyen
/// </summary>
[Table("VaiTroPhanQuyen")]
public class VaiTroPhanQuyen : DanhMucBase
{
    /// <summary>
    /// Danh sách mã quyền chi tiết hoặc quyền hạn được cấp (phân cách bằng dấu phẩy hoặc JSON).
    /// </summary>
    [StringLength(2000)]
    public string? QuyenHan { get; set; }

    /// <summary>
    /// Cho biết vai trò này có quyền quản trị tối cao của đơn vị hay không.
    /// </summary>
    public bool IsAdmin { get; set; } = false;
}

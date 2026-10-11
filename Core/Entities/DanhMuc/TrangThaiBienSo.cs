using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Trạng thái biển số (Ví dụ: Chưa cấp, Đã cấp, Thu hồi, Đấu giá, Đã hủy...).
/// Bảng lưu trữ: TrangThaiBienSo
/// </summary>
[Table("TrangThaiBienSo")]
public class TrangThaiBienSo : DanhMucBase
{
    /// <summary>
    /// Cho biết biển số ở trạng thái này có sẵn sàng để cấp mới cho phương tiện hay không.
    /// </summary>
    public bool ChoPhepCapMoi { get; set; } = false;

    /// <summary>
    /// Mã màu badge hiển thị trên giao diện (Ví dụ: "success", "primary", "warning", "danger").
    /// </summary>
    [StringLength(50)]
    public string? MauSacHienThi { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Loại thủ tục (Ví dụ: Đăng ký xe mới, Sang tên di chuyển, Cấp đổi biển số, Thu hồi...).
/// Bảng lưu trữ: LoaiThuTuc
/// </summary>
[Table("LoaiThuTuc")]
public class LoaiThuTuc : DanhMucBase
{
    /// <summary>
    /// Thời hạn giải quyết theo quy định hành chính (Ví dụ: "02 ngày làm việc", "Trong ngày").
    /// </summary>
    [StringLength(100)]
    public string? ThoiHanXuLy { get; set; }

    /// <summary>
    /// Mức lệ phí đăng ký mặc định tham chiếu (VNĐ).
    /// </summary>
    public decimal? LePhiMacDinh { get; set; }

    /// <summary>
    /// Căn cứ pháp lý quy định thủ tục (Thông tư, Nghị định quy định).
    /// </summary>
    [StringLength(500)]
    public string? CanCuPhapLy { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể cơ sở cho các bảng Danh mục trong hệ thống.
/// Kế thừa từ Audit để tự động ghi vết ngày tạo, người tạo, ngày cập nhật, người cập nhật.
/// </summary>
public abstract class DanhMucBase : Audit
{
    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị (Customer) mà danh mục này thuộc về.
    /// Cho phép phân tách danh mục theo từng khách hàng/đơn vị của người dùng.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }



    /// <summary>
    /// Mã danh mục (Khóa nghiệp vụ duy nhất trong phạm vi dự án, ví dụ: 29, TRANG, CHUA_CAP...).
    /// </summary>
    [Required(ErrorMessage = "Mã danh mục không được để trống.")]
    [StringLength(50)]
    public string Ma { get; set; } = string.Empty;

    /// <summary>
    /// Tên / Tiêu đề hiển thị của danh mục.
    /// </summary>
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [StringLength(250)]
    public string Ten { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết hoặc ghi chú giải thích.
    /// </summary>
    [StringLength(1000)]
    public string? MoTa { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp khi hiển thị danh sách (giá trị nhỏ ưu tiên đứng trước).
    /// </summary>
    public int ThuTu { get; set; } = 0;

    /// <summary>
    /// Trạng thái kích hoạt (true: đang hoạt động, false: tạm khóa/ngừng sử dụng).
    /// </summary>
    public bool IsActive { get; set; } = true;
}

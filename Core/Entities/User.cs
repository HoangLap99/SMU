using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể thông tin người dùng / cán bộ trong hệ thống và đơn vị (Customer).
/// </summary>
public class User : Audit
{
    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị mà cán bộ trực thuộc.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Họ và tên đầy đủ của cán bộ.
    /// </summary>
    [Required(ErrorMessage = "Họ và tên cán bộ không được để trống.")]
    [StringLength(250)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh Cấp bậc (chọn từ danh mục Cấp bậc).
    /// </summary>
    [StringLength(36)]
    public string? CapBacId { get; set; }

    /// <summary>
    /// Số hiệu Công an nhân dân (CAND) (Ví dụ: 142-890).
    /// </summary>
    [StringLength(50)]
    public string? SoHieuCAND { get; set; }

    /// <summary>
    /// Chức vụ / Vị trí công tác (Ví dụ: Cán bộ tổ CSTT, Phó Trưởng Công an phường...).
    /// </summary>
    [StringLength(200)]
    public string? ChucVu { get; set; }

    /// <summary>
    /// Mã định danh Vai trò phân quyền (chọn từ danh mục Vai trò phân quyền).
    /// </summary>
    [StringLength(36)]
    public string? VaiTroPhanQuyenId { get; set; }

    /// <summary>
    /// Email liên hệ (tùy chọn).
    /// </summary>
    [StringLength(250)]
    public string? Email { get; set; }

    /// <summary>
    /// Số điện thoại liên hệ (tùy chọn).
    /// </summary>
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Trạng thái hoạt động của cán bộ (true: Đang hoạt động, false: Ngừng hoạt động).
    /// Chú ý: Cán bộ ngừng hoạt động sẽ không chiếm quota tài khoản của đơn vị.
    /// </summary>
    public bool IsActive { get; set; } = true;

}

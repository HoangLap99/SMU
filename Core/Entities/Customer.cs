using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SMU.Core.Enums;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Khách hàng / Đơn vị (Customer) trong hệ thống.
/// </summary>
[Table("Customer")]
public class Customer : Audit
{
    /// <summary>
    /// Mã định danh khách hàng / đơn vị (Ví dụ: CUS-001, CAP_TAYMO).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên khách hàng / đơn vị (Ví dụ: Công an phường Tây Mỗ).
    /// </summary>
    [Required]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về khách hàng / đơn vị.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// ID Gói thuê bao áp dụng cho đơn vị.
    /// </summary>
    [StringLength(36)]
    public string? GoiThueBaoId { get; set; }

    /// <summary>
    /// Định mức số tài khoản cán bộ tối đa được phép hoạt động trong đơn vị (Quota).
    /// </summary>
    public int GioiHanUser { get; set; } = 10;

    /// <summary>
    /// Ngày bắt đầu hợp đồng / hoạt động.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (dự kiến hoặc thực tế).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Trạng thái khách hàng: Planning, InProgress, Completed, OnHold, Cancelled.
    /// </summary>
    public CustomerStatus Status { get; set; } = CustomerStatus.Planning;

    /// <summary>
    /// Trạng thái hoạt động của khách hàng.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

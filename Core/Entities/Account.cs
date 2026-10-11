using SMU.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể tài khoản đăng nhập gắn theo người dùng và dự án/đơn vị.
/// Lưu trữ độc lập các ID mà không sử dụng ForeignKey hay Navigation Property.
/// </summary>
public class Account : Audit
{
    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị mà tài khoản này thuộc về.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Khóa liên kết tới người dùng sở hữu tài khoản.
    /// </summary>
    [StringLength(36)]
    public string? UserId { get; set; }

    /// <summary>
    /// Tên đăng nhập (duy nhất).
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu đã được băm (BCrypt Hash).
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Phân quyền vai trò tài khoản: Admin, User, Trial.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Ngày bắt đầu dùng thử (chỉ áp dụng cho tài khoản Trial).
    /// </summary>
    public DateTime? TrialStartDate { get; set; }

    /// <summary>
    /// Ngày hết hạn dùng thử (chỉ áp dụng cho tài khoản Trial).
    /// </summary>
    public DateTime? TrialEndDate { get; set; }

    /// <summary>
    /// Trạng thái khóa tài khoản.
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// Thời điểm đăng nhập gần nhất (UTC).
    /// </summary>
    public DateTime? LastLoginAt { get; set; }


    /// <summary>
    /// Kiểm tra tài khoản dùng thử còn hiệu lực hay không.
    /// </summary>
    public bool IsTrialValid()
    {
        if (Role != UserRole.Trial) return true;
        if (!TrialEndDate.HasValue) return false;
        return DateTime.UtcNow <= TrialEndDate.Value;
    }
}

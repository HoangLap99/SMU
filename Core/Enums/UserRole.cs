namespace SMU.Core.Enums;

/// <summary>
/// Các vai trò / phân quyền của tài khoản trong hệ thống.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Quản trị viên hệ thống - toàn quyền.
    /// </summary>
    Admin = 1,
    /// <summary>
    /// Quản trị của từng khách hàng.
    /// </summary>
    AdminCustomer = 2,
    /// <summary>
    /// Người dùng chính thức.
    /// </summary>
    User = 3,

    /// <summary>
    /// Tài khoản đăng ký dùng thử (có thời hạn sử dụng).
    /// </summary>
    Trial = 4
   

}

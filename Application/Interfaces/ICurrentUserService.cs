namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện cung cấp thông tin người dùng hiện tại đang thực thi yêu cầu (Current User Context).
/// Cho phép truy xuất danh tính người dùng ở bất kỳ tầng nào (Service, Repository, Orchestrator, Controller).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Mã định danh của người dùng hiện tại (Guid dạng chuỗi).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị mà người dùng hiện tại trực thuộc.
    /// </summary>
    string? CustomerId { get; }
    /// <summary>
    /// Tên đăng nhập của người dùng hiện tại.
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Email của người dùng hiện tại.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Vai trò của người dùng hiện tại (Admin, User, Trial).
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Họ và tên đầy đủ của người dùng hiện tại.
    /// </summary>
    string? FullName { get; }

    /// <summary>
    /// Trạng thái xác thực của yêu cầu hiện tại (true nếu đã qua xác thực JWT hợp lệ).
    /// </summary>
    bool IsAuthenticated { get; }
}

using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý người dùng và tài khoản đăng nhập.
/// Tuân thủ Dependency Inversion Principle (DIP).
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lấy danh sách toàn bộ người dùng kèm thông tin tài khoản đăng nhập và phân quyền.
    /// </summary>
    Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết một người dùng theo Id.
    /// </summary>
    Task<UserResponse?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới người dùng kèm tài khoản với phân quyền chỉ định (Admin, User, Trial).
    /// </summary>
    Task<UserResponse> CreateUserWithAccountAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đăng ký tài khoản dùng thử tự do từ phía người dùng (tự động cấp quyền Trial).
    /// </summary>
    Task<UserResponse> RegisterTrialAsync(RegisterTrialRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác thực đăng nhập bằng tên đăng nhập/email và mật khẩu.
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi mật khẩu cho chính người dùng hiện tại đang đăng nhập.
    /// </summary>
    Task<UserActionResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt lại (reset) mật khẩu cho người dùng chỉ định (Chỉ dành riêng cho tài khoản có vai trò Quản trị viên - Admin).
    /// </summary>
    Task<UserActionResponse> AdminResetPasswordAsync(string targetUserIdOrUsername, string newPassword, bool unlockAccount = true, CancellationToken cancellationToken = default);
}

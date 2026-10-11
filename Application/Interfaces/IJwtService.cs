using SMU.Core.Entities;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ cấp phát token JWT xác thực người dùng.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Tạo Access Token JWT cho người dùng với thời hạn hết hạn được định cấu hình (mặc định: 1 ngày).
    /// </summary>
    /// <param name="user">Thực thể người dùng.</param>
    /// <param name="account">Thực thể tài khoản đăng nhập gắn kèm.</param>
    /// <param name="expiresAt">Thời điểm UTC token hết hạn.</param>
    /// <returns>Chuỗi JWT Token.</returns>
    string GenerateToken(User user, Account account, out DateTime expiresAt);
}

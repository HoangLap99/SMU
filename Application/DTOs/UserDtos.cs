using System.ComponentModel.DataAnnotations;
using SMU.Core.Enums;

namespace SMU.Application.DTOs;

/// <summary>
/// Yêu cầu tạo mới người dùng kèm tài khoản đăng nhập (Dành cho Quản trị viên).
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [MaxLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [MaxLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [MinLength(3, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự.")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu gạch dưới hoặc dấu chấm.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Phân quyền tài khoản: Admin, User, Trial.
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn vai trò người dùng.")]
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Số ngày được phép dùng thử (áp dụng khi Role là Trial). Mặc định là 14 ngày.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Số ngày dùng thử phải từ 1 đến 365 ngày.")]
    public int? TrialDays { get; set; } = 14;

    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị mà người dùng thuộc về (tùy chọn).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Mã khách hàng / đơn vị (Code) mà người dùng thuộc về (tùy chọn).
    /// </summary>
    public string? MaCustomer { get; set; }
}

/// <summary>
/// Yêu cầu đăng ký tài khoản dùng thử từ phía người dùng bên ngoài (Self-Registration).
/// Tự động gán quyền 'Trial' kèm thời hạn dùng thử mặc định (14 ngày).
/// </summary>
public class RegisterTrialRequest
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [MaxLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [MaxLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [MinLength(3, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự.")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu gạch dưới hoặc dấu chấm.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Mã khách hàng / đơn vị đăng ký dùng thử (tùy chọn).
    /// </summary>
    public string? MaCustomer { get; set; }
}

/// <summary>
/// Thông tin tài khoản đăng nhập trả về cho client.
/// </summary>
public class AccountResponse
{
    public string? Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleName => Role.ToString();
    public DateTime? TrialStartDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsTrialExpired => Role == UserRole.Trial && TrialEndDate.HasValue && DateTime.UtcNow > TrialEndDate.Value;
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? Modified { get; set; }
    public string? ModifileById { get; set; }
    public string? ModifileByName { get; set; }
}

/// <summary>
/// Thông tin người dùng kèm tài khoản trả về.
/// </summary>
public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? Modified { get; set; }
    public string? ModifileById { get; set; }
    public string? ModifileByName { get; set; }
    public AccountResponse? Account { get; set; }
}

/// <summary>
/// Yêu cầu đăng nhập tài khoản.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

  

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Mã khách hàng / đơn vị (Bắt buộc đối với các tài khoản không phải Admin; Tài khoản có role=Admin không cần nhập).
    /// </summary>
    public string? MaCustomer { get; set; }

    /// <summary>
    /// Mã khách hàng / đơn vị (Bí danh tương đương với MaCustomer).
    /// </summary>
    public string? CustomerCode { get; set; }
}

/// <summary>
/// Kết quả phản hồi đăng nhập kèm token JWT ủy quyền (Authorization) có thời hạn 1 ngày.
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Mã token JWT dùng cho header Authorization: Bearer &lt;AccessToken&gt;.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Chuỗi Authorization đầy đủ định dạng "Bearer &lt;AccessToken&gt;" tiện dùng trực tiếp.
    /// </summary>
    public string? Authorization => string.IsNullOrWhiteSpace(AccessToken) ? null : $"{TokenType} {AccessToken}".Trim();

    /// <summary>
    /// Loại token xác thực (mặc định: Bearer).
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Thời điểm hết hạn của Token (UTC) - Sau đúng 1 ngày kể từ lúc đăng nhập.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Số giây còn hiệu lực của token (86400 giây = 1 ngày).
    /// </summary>
    public int? ExpiresInSeconds { get; set; }

    /// <summary>
    /// Thông tin người dùng đăng nhập.
    /// </summary>
    public UserResponse? User { get; set; }
}

/// <summary>
/// Dữ liệu thông tin người dùng hiện tại đang gửi yêu cầu (Current User Identity).
/// </summary>
public class CurrentUserProfileResponse
{
    public string? UserId { get; set; }
    public string? CustomerId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? FullName { get; set; }
    public bool IsAuthenticated { get; set; }
}

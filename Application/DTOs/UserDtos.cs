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
/// Tự động khởi tạo Khách hàng / Đơn vị mới trong bảng Customer và gán quyền 'Trial' cho tài khoản với thời hạn 14 ngày.
/// </summary>
public class RegisterTrialRequest
{
    private string? _tenCustomer;
    private string? _maCustomer;

    /// <summary>
    /// Tên khách hàng / Tên đơn vị đăng ký dùng thử (Bắt buộc, ví dụ: 'Công an phường Tây Mỗ').
    /// </summary>
    [Required(ErrorMessage = "Tên đơn vị / khách hàng không được để trống.")]
    [MaxLength(250, ErrorMessage = "Tên đơn vị / khách hàng không được vượt quá 250 ký tự.")]
    public string TenCustomer
    {
        get => _tenCustomer ?? string.Empty;
        set => _tenCustomer = value;
    }

    /// <summary>
    /// Tên đơn vị (Bí danh tương đương với TenCustomer).
    /// </summary>
    public string? TenDonVi
    {
        get => _tenCustomer;
        set
        {
            if (string.IsNullOrWhiteSpace(_tenCustomer))
                _tenCustomer = value;
        }
    }

    /// <summary>
    /// Tên khách hàng (Bí danh tương đương với TenCustomer).
    /// </summary>
    public string? CustomerName
    {
        get => _tenCustomer;
        set
        {
            if (string.IsNullOrWhiteSpace(_tenCustomer))
                _tenCustomer = value;
        }
    }

    /// <summary>
    /// Mã khách hàng / đơn vị đăng ký dùng thử (Bắt buộc, ví dụ: 'CAP_TAYMO'). Sẽ lưu vào bảng Customer và dùng để đăng nhập.
    /// </summary>
    [Required(ErrorMessage = "Mã khách hàng / đơn vị không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã khách hàng / đơn vị không được vượt quá 50 ký tự.")]
    public string MaCustomer
    {
        get => _maCustomer ?? string.Empty;
        set => _maCustomer = value;
    }

    /// <summary>
    /// Mã khách hàng / đơn vị (Bí danh tương đương với MaCustomer).
    /// </summary>
    public string? CustomerCode
    {
        get => _maCustomer;
        set
        {
            if (string.IsNullOrWhiteSpace(_maCustomer))
                _maCustomer = value;
        }
    }

    /// <summary>
    /// Lấy tên khách hàng / đơn vị hợp lệ từ các trường TenCustomer, TenDonVi, CustomerName.
    /// </summary>
    public string GetEffectiveCustomerName() =>
        (!string.IsNullOrWhiteSpace(_tenCustomer) ? _tenCustomer :
         !string.IsNullOrWhiteSpace(TenDonVi) ? TenDonVi :
         CustomerName ?? string.Empty).Trim();

    /// <summary>
    /// Lấy mã khách hàng / đơn vị hợp lệ từ các trường MaCustomer, CustomerCode.
    /// </summary>
    public string GetEffectiveCustomerCode() =>
        (!string.IsNullOrWhiteSpace(_maCustomer) ? _maCustomer :
         CustomerCode ?? string.Empty).Trim();

    /// <summary>
    /// Họ và tên đầy đủ của người dùng (tối đa 150 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [MaxLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ Email liên hệ (duy nhất trên toàn hệ thống).
    /// </summary>
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [MaxLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Số điện thoại liên hệ (tùy chọn, tối đa 20 ký tự).
    /// </summary>
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Tên đăng nhập (từ 3 - 50 ký tự; chỉ gồm chữ cái, chữ số, gạch dưới, dấu chấm; không trùng Admin và duy nhất trong đơn vị).
    /// </summary>
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [MinLength(3, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự.")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu gạch dưới hoặc dấu chấm.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu đăng nhập (từ 6 - 100 ký tự; sẽ được băm một chiều bằng BCrypt trước khi lưu).
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
    public string Password { get; set; } = string.Empty;
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

    /// <summary>
    /// Mã khách hàng / đơn vị trực thuộc.
    /// </summary>
    public string? CustomerCode { get; set; }

    /// <summary>
    /// Tên khách hàng / đơn vị trực thuộc.
    /// </summary>
    public string? CustomerName { get; set; }

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

    /// <summary>
    /// Phân quyền vai trò tài khoản trả về theo số nguyên: 1 = Admin, 2 = AdminCustomer, 3 = User, 4 = Trial.
    /// </summary>
    public int? Role { get; set; }

    /// <summary>
    /// Tên vai trò tài khoản (Admin, AdminCustomer, User, Trial).
    /// </summary>
    public string? RoleName { get; set; }

    public string? FullName { get; set; }
    public bool IsAuthenticated { get; set; }
}

/// <summary>
/// Yêu cầu đổi mật khẩu cho chính người dùng đang đăng nhập.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Mật khẩu hiện tại của người dùng.
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu mới cần đổi (tối thiểu 6 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu mới không được vượt quá 100 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Xác nhận lại mật khẩu mới (phải khớp với Mật khẩu mới).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu mới.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Yêu cầu đặt lại (reset) mật khẩu người dùng do Quản trị viên (Admin) thực hiện.
/// </summary>
public class AdminResetPasswordRequest
{
    /// <summary>
    /// Mã Id người dùng hoặc Tên đăng nhập (Username) cần reset mật khẩu.
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập Id người dùng hoặc Tên đăng nhập cần reset mật khẩu.")]
    public string UserIdOrUsername { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu mới cần đặt lại (tối thiểu 6 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu mới không được vượt quá 100 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Tự động mở khóa tài khoản nếu tài khoản đang bị khóa (mặc định: true).
    /// </summary>
    public bool UnlockAccount { get; set; } = true;
}

/// <summary>
/// Yêu cầu đặt lại (reset) mật khẩu theo Id người dùng trên URL route (dành riêng cho Admin).
/// </summary>
public class ResetPasswordByIdRequest
{
    /// <summary>
    /// Mật khẩu mới cần đặt lại (tối thiểu 6 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu mới không được vượt quá 100 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Tự động mở khóa tài khoản nếu tài khoản đang bị khóa (mặc định: true).
    /// </summary>
    public bool UnlockAccount { get; set; } = true;
}

/// <summary>
/// Kết quả phản hồi chung cho các thao tác người dùng (đổi mật khẩu, reset mật khẩu...).
/// </summary>
public class UserActionResponse
{
    /// <summary>
    /// Trạng thái thành công.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Thông điệp phản hồi kết quả thao tác.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

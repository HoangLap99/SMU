using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Enums;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller quản lý danh sách người dùng, tài khoản đăng nhập và phân quyền (Admin, User, Trial).
/// Mặc định yêu cầu xác thực JWT Authorization ngoại trừ chức năng đăng nhập và đăng ký dùng thử.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        IUserService userService,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy thông tin định danh của người dùng hiện tại đang gửi yêu cầu (truy xuất từ Token JWT).
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserProfileResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.CustomerId;
        UserResponse? user = null;

        if (!string.IsNullOrEmpty(_currentUserService.UserId))
        {
            user = await _userService.GetUserByIdAsync(_currentUserService.UserId, cancellationToken);
            if (string.IsNullOrEmpty(customerId))
            {
                customerId = user?.CustomerId;
            }
        }

        int? roleValue = null;
        string? roleName = null;

        if (user?.Account != null)
        {
            roleValue = (int)user.Account.Role;
            roleName = user.Account.Role.ToString();
        }
        else if (!string.IsNullOrWhiteSpace(_currentUserService.Role))
        {
            if (Enum.TryParse<UserRole>(_currentUserService.Role, true, out var parsedRole))
            {
                roleValue = (int)parsedRole;
                roleName = parsedRole.ToString();
            }
            else if (int.TryParse(_currentUserService.Role, out var intRole))
            {
                roleValue = intRole;
                roleName = ((UserRole)intRole).ToString();
            }
        }

        return Ok(new CurrentUserProfileResponse
        {
            UserId = _currentUserService.UserId,
            CustomerId = customerId,
            Username = _currentUserService.Username ?? user?.Account?.Username,
            Email = _currentUserService.Email ?? user?.Email,
            Role = roleValue,
            RoleName = roleName,
            FullName = _currentUserService.FullName ?? user?.FullName,
            IsAuthenticated = _currentUserService.IsAuthenticated
        });
    }

    /// <summary>
    /// Lấy danh sách toàn bộ người dùng kèm tài khoản đăng nhập và phân quyền trong hệ thống.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Lấy chi tiết thông tin một người dùng theo mã định danh (Id).
    /// </summary>
    /// <param name="id">Mã GUID của người dùng.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetUserById(string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy người dùng",
                Detail = $"Không tìm thấy người dùng có Id: '{id}'."
            });
        }

        return Ok(user);
    }

    /// <summary>
    /// Tạo mới người dùng và tài khoản đăng nhập với phân quyền chỉ định (Admin, User, Trial).
    /// </summary>
    /// <remarks>
    /// - Quyền Admin: Quản trị viên hệ thống.
    /// - Quyền User: Người dùng chính thức.
    /// - Quyền Trial: Người dùng dùng thử (có thời hạn ngày dùng thử, mặc định 14 ngày).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        var createdUser = await _userService.CreateUserWithAccountAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
    }

    /// <summary>
    /// Đăng ký tài khoản dùng thử dành cho người dùng mới (Self-Registration).
    /// </summary>
    /// <remarks>
    /// Endpoint công khai không yêu cầu token xác thực, cho phép người dùng tự đăng ký trải nghiệm hệ thống với phân quyền Trial.
    /// Khi đăng ký dùng thử, người dùng sẽ nhập cả **Tên đơn vị** (`tenCustomer` / `tenDonVi`) và **Mã đơn vị** (`maCustomer` / `customerCode`).
    /// Hệ thống sẽ tự động tạo một khách hàng / đơn vị mới trong bảng `Customer` và khởi tạo tài khoản gắn `CustomerId` theo đơn vị vừa tạo.
    /// 
    /// ### 1. Quy trình Kiểm tra (Check / Validation):
    /// - **Tên đơn vị (`tenCustomer` / `tenDonVi`)**: Bắt buộc nhập, tối đa 250 ký tự.
    /// - **Mã đơn vị (`maCustomer` / `customerCode`)**: Bắt buộc nhập, tối đa 50 ký tự. Hệ thống tự động chuẩn hóa sang chữ hoa (in hoa).
    /// - **Kiểm tra trùng Mã đơn vị**: Mã đơn vị phải là duy nhất trên toàn hệ thống trong bảng `Customer`. Nếu đã tồn tại sẽ trả về lỗi `400 Bad Request`.
    /// - **Chuẩn hóa dữ liệu**: Tự động chuyển `Username` và `Email` về chữ thường (lowercase) và loại bỏ khoảng trắng thừa (Trim).
    /// - **Kiểm tra Email**: Email phải là duy nhất trên toàn hệ thống (bảng `Users`). Nếu đã tồn tại sẽ trả về lỗi `400 Bad Request`.
    /// - **Kiểm tra Tên đăng nhập (`Username`)**: Không được trùng với bất kỳ tài khoản Quản trị viên nào (`Role = Admin`).
    /// 
    /// ### 2. Quy trình Lưu trữ (Save / Persistence):
    /// - **Tạo Khách hàng (`Customer`)**: Tạo mới bản ghi trong bảng `Customer` với `Code = MaCustomer`, `Name = TenCustomer`, `Status = InProgress`, `IsActive = true`, `GioiHanUser = 10`, hạn sử dụng 14 ngày.
    /// - **Gắn Khách hàng (`CustomerId`)**: Tài khoản cán bộ (`Users`) và tài khoản đăng nhập (`Accounts`) được tự động gán `CustomerId` theo bản ghi `Customer` vừa tạo.
    /// - **Phân quyền**: Tự động gán quyền `Role = UserRole.Trial` (giá trị enum = 3).
    /// - **Thời hạn dùng thử**: 
    ///   - `TrialStartDate`: Lấy thời điểm hiện tại (`DateTime.UtcNow`).
    ///   - `TrialEndDate`: Tự động cộng thêm **14 ngày** tính từ ngày bắt đầu.
    /// - **Bảo mật mật khẩu**: Mật khẩu được băm một chiều an toàn bằng thuật toán **BCrypt** trước khi lưu vào bảng `Accounts` (không lưu mật khẩu gốc).
    /// - **Giao dịch toàn vẹn (Atomic Transaction)**: Tạo đồng thời cả 3 thực thể (`Customer`, `Users`, `Accounts`) trong một giao dịch cơ sở dữ liệu duy nhất.
    /// 
    /// ### 3. Lưu ý khi Đăng nhập sau này (`/api/Users/login`):
    /// - Tài khoản dùng thử trực thuộc đơn vị mới tạo, do đó khi đăng nhập người dùng **bắt buộc phải truyền cả MaCustomer** (cùng với Username/Email và Password).
    /// - Khi vượt quá thời hạn 14 ngày (`TrialEndDate`), trường `isTrialExpired` sẽ chuyển thành `true` để ứng dụng nhận diện và hiển thị thông báo hết hạn.
    /// </remarks>
    /// <param name="request">Thông tin đăng ký tài khoản dùng thử (Tên đơn vị, Mã đơn vị, Họ tên, Email, Tên đăng nhập, Mật khẩu, Số điện thoại).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <response code="201">Đăng ký thành công, tạo đơn vị Customer mới và trả về thông tin người dùng kèm thời hạn dùng thử 14 ngày.</response>
    /// <response code="400">Dữ liệu không hợp lệ (Mã đơn vị đã tồn tại, Email đã tồn tại, Username trùng với Admin hoặc thiếu thông tin bắt buộc).</response>
    /// <response code="500">Lỗi máy chủ nội bộ hoặc lỗi cơ sở dữ liệu.</response>
    [AllowAnonymous]
    [HttpPost("register-trial")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> RegisterTrial(
        [FromBody] RegisterTrialRequest request, 
        CancellationToken cancellationToken)
    {
        var createdUser = await _userService.RegisterTrialAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
    }

    /// <summary>
    /// Đăng nhập tài khoản bằng tên đăng nhập (hoặc email) và mật khẩu.
    /// - Tài khoản có role=Admin: Không cần nhập Mã Customer (MaCustomer).
    /// - Tài khoản có role khác Admin (AdminCustomer, User, Trial): Bắt buộc phải nhập cả Mã Customer (MaCustomer).
    /// Trả về JWT Token có thời hạn sử dụng 1 ngày (Authorization) để gọi các API khác.
    /// Endpoint công khai không yêu cầu token xác thực.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Đăng nhập không thành công",
                Detail = result.Message
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Đổi mật khẩu cho chính người dùng đang đăng nhập (Self Change Password).
    /// Yêu cầu cung cấp mật khẩu hiện tại và mật khẩu mới (tối thiểu 6 ký tự).
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(UserActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserActionResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _userService.ChangePasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Đặt lại (reset) mật khẩu người dùng theo Id hoặc Tên đăng nhập (Chỉ tài khoản có vai trò Quản trị viên - Admin).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(UserActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserActionResponse>> AdminResetPassword(
        [FromBody] AdminResetPasswordRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _userService.AdminResetPasswordAsync(request.UserIdOrUsername, request.NewPassword, request.UnlockAccount, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Đặt lại (reset) mật khẩu theo Id người dùng trên đường dẫn URL (Chỉ tài khoản có vai trò Quản trị viên - Admin).
    /// </summary>
    /// <param name="id">Mã định danh (Id) người dùng cần đặt lại mật khẩu.</param>
    /// <param name="request">Thông tin mật khẩu mới.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(typeof(UserActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserActionResponse>> ResetPasswordById(
        [FromRoute] string id,
        [FromBody] ResetPasswordByIdRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _userService.AdminResetPasswordAsync(id, request.NewPassword, request.UnlockAccount, cancellationToken);
        return Ok(result);
    }
}

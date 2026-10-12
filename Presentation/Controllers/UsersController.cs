using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

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
        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            var user = await _userService.GetUserByIdAsync(_currentUserService.UserId, cancellationToken);
            customerId = user?.CustomerId;
        }

        return Ok(new CurrentUserProfileResponse
        {
            UserId = _currentUserService.UserId,
            CustomerId = customerId,
            Username = _currentUserService.Username,
            Email = _currentUserService.Email,
            Role = _currentUserService.Role,
            FullName = _currentUserService.FullName,
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
    /// Tự động phân quyền Trial kèm thời hạn 14 ngày dùng thử.
    /// Endpoint công khai không yêu cầu token xác thực.
    /// </summary>
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

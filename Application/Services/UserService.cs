using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Core.Enums;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Dịch vụ xử lý nghiệp vụ quản lý người dùng và tài khoản đăng nhập.
/// </summary>
public class UserService : IUserService
{
    private readonly PostgresDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        PostgresDbContext dbContext, 
        IJwtService jwtService,
        ICurrentUserService currentUserService,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.Created)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id!).ToList();
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.UserId != null && userIds.Contains(a.UserId))
            .ToDictionaryAsync(a => a.UserId!, a => a, cancellationToken);

        var customerIds = users.Select(u => u.CustomerId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var customerMap = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id!, c => c, cancellationToken);

        return users.Select(u => MapToUserResponse(
            u, 
            accounts.GetValueOrDefault(u.Id!),
            u.CustomerId != null ? customerMap.GetValueOrDefault(u.CustomerId) : null
        )).ToList();
    }

    public async Task<UserResponse?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null) return null;

        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == id, cancellationToken);

        var customer = !string.IsNullOrWhiteSpace(user.CustomerId)
            ? await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == user.CustomerId, cancellationToken)
            : null;

        return MapToUserResponse(user, account, customer);
    }

    public async Task<UserResponse> CreateUserWithAccountAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Xác định CustomerId nếu có chỉ định (qua CustomerId hoặc MaCustomer)
        string? resolvedCustomerId = request.CustomerId;
        if (string.IsNullOrWhiteSpace(resolvedCustomerId) && !string.IsNullOrWhiteSpace(request.MaCustomer))
        {
            var cust = await _dbContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code.ToLower() == request.MaCustomer.Trim().ToLower(), cancellationToken);
            resolvedCustomerId = cust?.Id;
        }

        // 2. Kiểm tra trùng lặp Email
        if (await _dbContext.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail, cancellationToken))
        {
            throw new ArgumentException($"Địa chỉ email '{request.Email}' đã được đăng ký trong hệ thống.");
        }

        // 3. Kiểm tra tính duy nhất của Tên đăng nhập (Username):
        // - Không được trùng với bất kỳ tài khoản nào có Role = Admin
        var isAdminDuplicate = await _dbContext.Accounts.AnyAsync(a => 
            a.Role == UserRole.Admin && a.Username.ToLower() == normalizedUsername, 
            cancellationToken);

        if (isAdminDuplicate)
        {
            throw new ArgumentException($"Tên đăng nhập '{request.Username}' trùng với tài khoản Quản trị viên (Admin) của hệ thống.");
        }

        // - Nếu là Admin thì Username phải duy nhất toàn hệ thống
        if (request.Role == UserRole.Admin)
        {
            if (await _dbContext.Accounts.AnyAsync(a => a.Username.ToLower() == normalizedUsername, cancellationToken))
            {
                throw new ArgumentException($"Tên đăng nhập '{request.Username}' đã tồn tại trong hệ thống.");
            }
        }
        else
        {
            // - Khác CustomerId thì được đặt trùng UserName, nhưng trong cùng CustomerId thì không được trùng
            var isDuplicateInSameCustomer = await _dbContext.Accounts.AnyAsync(a => 
                a.CustomerId == resolvedCustomerId && a.Username.ToLower() == normalizedUsername, 
                cancellationToken);

            if (isDuplicateInSameCustomer)
            {
                throw new ArgumentException($"Tên đăng nhập '{request.Username}' đã tồn tại trong đơn vị / khách hàng này.");
            }
        }

        // 4. Thiết lập thời gian dùng thử nếu có
        DateTime? trialStart = null;
        DateTime? trialEnd = null;
        if (request.Role == UserRole.Trial)
        {
            trialStart = DateTime.UtcNow;
            var days = request.TrialDays.GetValueOrDefault(14);
            trialEnd = trialStart.Value.AddDays(days);
        }

        // 5. Băm mật khẩu bằng BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 5. Tạo User và Account tương ứng
        var user = new User
        {
            CustomerId = resolvedCustomerId,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Created = DateTime.UtcNow,
            IsActive = true
        };

        var account = new Account
        {
            CustomerId = resolvedCustomerId,
            UserId = user.Id,
            Username = normalizedUsername,
            PasswordHash = passwordHash,
            Role = request.Role,
            TrialStartDate = trialStart,
            TrialEndDate = trialEnd,
            IsLocked = false,
            Created = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã khởi tạo thành công người dùng '{FullName}' ({Email}) với quyền '{Role}'", 
            user.FullName, user.Email, account.Role);

        Customer? resolvedCustomer = null;
        if (!string.IsNullOrWhiteSpace(resolvedCustomerId))
        {
            resolvedCustomer = await _dbContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == resolvedCustomerId, cancellationToken);
        }

        return MapToUserResponse(user, account, resolvedCustomer);
    }

    public async Task<UserResponse> RegisterTrialAsync(RegisterTrialRequest request, CancellationToken cancellationToken = default)
    {
        var effectiveCustomerCode = request.GetEffectiveCustomerCode().ToUpperInvariant();
        var effectiveCustomerName = request.GetEffectiveCustomerName();

        if (string.IsNullOrWhiteSpace(effectiveCustomerName))
        {
            throw new ArgumentException("Tên đơn vị / khách hàng không được để trống khi đăng ký tài khoản dùng thử.");
        }

        if (effectiveCustomerName.Length > 250)
        {
            throw new ArgumentException("Tên đơn vị / khách hàng không được vượt quá 250 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(effectiveCustomerCode))
        {
            throw new ArgumentException("Mã khách hàng / đơn vị không được để trống khi đăng ký tài khoản dùng thử.");
        }

        if (effectiveCustomerCode.Length > 50)
        {
            throw new ArgumentException("Mã khách hàng / đơn vị không được vượt quá 50 ký tự.");
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Kiểm tra tính duy nhất của Mã khách hàng trong bảng Customer
        var isCustomerCodeExist = await _dbContext.Customers
            .AnyAsync(c => c.Code.ToUpper() == effectiveCustomerCode, cancellationToken);
        if (isCustomerCodeExist)
        {
            throw new ArgumentException($"Mã khách hàng / đơn vị '{effectiveCustomerCode}' đã tồn tại trong hệ thống. Vui lòng chọn mã khác.");
        }

        // 2. Kiểm tra trùng lặp Email
        if (await _dbContext.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail, cancellationToken))
        {
            throw new ArgumentException($"Địa chỉ email '{request.Email}' đã được đăng ký trong hệ thống.");
        }

        // 3. Kiểm tra tính duy nhất của Tên đăng nhập (Username) so với Admin
        var isAdminDuplicate = await _dbContext.Accounts.AnyAsync(a => 
            a.Role == UserRole.Admin && a.Username.ToLower() == normalizedUsername, 
            cancellationToken);

        if (isAdminDuplicate)
        {
            throw new ArgumentException($"Tên đăng nhập '{request.Username}' trùng với tài khoản Quản trị viên (Admin) của hệ thống.");
        }

        // 4. Thiết lập thời gian dùng thử (mặc định 14 ngày)
        var trialStart = DateTime.UtcNow;
        var trialEnd = trialStart.AddDays(14);

        // 5. Khởi tạo đối tượng Customer mới đại diện cho đơn vị đăng ký dùng thử
        var customer = new Customer
        {
            Code = effectiveCustomerCode,
            Name = effectiveCustomerName,
            Description = $"Tài khoản dùng thử đăng ký bởi {request.FullName.Trim()} ({normalizedEmail})",
            GioiHanUser = 10,
            StartDate = trialStart,
            EndDate = trialEnd,
            Status = CustomerStatus.InProgress,
            IsActive = true,
            Created = trialStart
        };

        // 6. Băm mật khẩu bằng BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 7. Tạo User gắn CustomerId theo Customer mới tạo
        var user = new User
        {
            CustomerId = customer.Id,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Created = trialStart,
            IsActive = true
        };

        // 8. Tạo Account gắn CustomerId và UserId
        var account = new Account
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            Username = normalizedUsername,
            PasswordHash = passwordHash,
            Role = UserRole.Trial,
            TrialStartDate = trialStart,
            TrialEndDate = trialEnd,
            IsLocked = false,
            Created = trialStart
        };

        // 9. Thêm vào DbContext và lưu đồng thời cả 3 thực thể trong một transaction
        _dbContext.Customers.Add(customer);
        _dbContext.Users.Add(user);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã khởi tạo thành công khách hàng dùng thử '{CustomerName}' ({CustomerCode}) và tài khoản '{Username}' ({Email}) với quyền Trial",
            customer.Name, customer.Code, account.Username, user.Email);

        return MapToUserResponse(user, account, customer);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var rawInput = !string.IsNullOrWhiteSpace(request.UsernameOrEmail) ? request.UsernameOrEmail : "";
        var input = (rawInput ?? string.Empty).Trim().ToLowerInvariant();
        var reqCustomerCode = (!string.IsNullOrWhiteSpace(request.MaCustomer) ? request.MaCustomer : request.CustomerCode)?.Trim();

        Account? account = null;
        User? user = null;
        Customer? targetCustomer = null;

        // Trường hợp 1: KHÔNG truyền Mã Customer (Chỉ tài khoản Admin được phép đăng nhập không cần Mã Customer)
        if (string.IsNullOrWhiteSpace(reqCustomerCode))
        {
            var adminUserByEmail = await _dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == input, cancellationToken);
            var adminTargetUserId = adminUserByEmail?.Id;

            // Tìm tài khoản Admin theo username hoặc email
            account = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Role == UserRole.Admin && (a.Username.ToLower() == input || (adminTargetUserId != null && a.UserId == adminTargetUserId)), cancellationToken);

            if (account == null)
            {
                // Nếu không phải Admin, kiểm tra xem có tài khoản phi Admin nào trùng username không để nhắc người dùng
                var anyNonAdminAccount = await _dbContext.Accounts
                    .AnyAsync(a => a.Username.ToLower() == input || (adminTargetUserId != null && a.UserId == adminTargetUserId), cancellationToken);

                if (anyNonAdminAccount)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Tài khoản yêu cầu bắt buộc phải nhập Mã khách hàng / đơn vị."
                    };
                }

                return new LoginResponse
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không chính xác."
                };
            }

            user = adminUserByEmail ?? (account.UserId != null ? await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == account.UserId, cancellationToken) : null);
        }
        else
        {
            // Trường hợp 2: CÓ truyền Mã Customer (Tìm trong phạm vi Customer đó)
            // 2.1. Xác thực Customer theo Mã (Code) hoặc Id
            targetCustomer = await _dbContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code.ToLower() == reqCustomerCode.ToLower() || c.Id == reqCustomerCode, cancellationToken);

            if (targetCustomer == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Mã khách hàng / đơn vị không tồn tại trong hệ thống."
                };
            }

            if (!targetCustomer.IsActive)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Khách hàng / đơn vị đã ngừng hoạt động."
                };
            }

            // 2.2. Tìm User & Account trong Customer tương ứng (hỗ trợ trùng Username giữa các Customer khác nhau)
            var userByEmail = await _dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.CustomerId == targetCustomer.Id && u.Email != null && u.Email.ToLower() == input, cancellationToken);
            var targetUserId = userByEmail?.Id;

            account = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => 
                    (a.CustomerId == targetCustomer.Id && (a.Username.ToLower() == input || (targetUserId != null && a.UserId == targetUserId)))
                    || (a.Role == UserRole.Admin && (a.Username.ToLower() == input || (targetUserId != null && a.UserId == targetUserId))),
                    cancellationToken);

            // Dự phòng cho tài khoản chưa liên kết CustomerId trong DB
            if (account == null)
            {
                var unassignedAccount = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.CustomerId == null && a.Role != UserRole.Admin && a.Username.ToLower() == input, cancellationToken);

                if (unassignedAccount != null)
                {
                    account = unassignedAccount;
                    account.CustomerId = targetCustomer.Id;
                }
            }

            if (account == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không chính xác."
                };
            }

            user = userByEmail ?? (account.UserId != null ? await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == account.UserId, cancellationToken) : null);

            // Gán ngữ cảnh CustomerId cho phiên làm việc
            if (account.Role == UserRole.Admin && user != null)
            {
                user.CustomerId = targetCustomer.Id;
            }
            else if (user != null && string.IsNullOrEmpty(user.CustomerId))
            {
                user.CustomerId = targetCustomer.Id;
            }
        }

        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Không tìm thấy thông tin người dùng liên kết với tài khoản này."
            };
        }

        // 3. Kiểm tra trạng thái khóa tài khoản
        if (account.IsLocked)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Tài khoản đã bị tạm khóa. Vui lòng liên hệ quản trị viên."
            };
        }

        // 4. Kiểm tra trạng thái người dùng
        if (!user.IsActive)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Tài khoản người dùng đã bị vô hiệu hóa."
            };
        }

        // 5. Kiểm tra thời hạn dùng thử nếu là tài khoản Trial
        if (account.Role == UserRole.Trial && !account.IsTrialValid())
        {
            return new LoginResponse
            {
                Success = false,
                Message = $"Tài khoản dùng thử đã hết hạn vào ngày {account.TrialEndDate:dd/MM/yyyy HH:mm} UTC. Vui lòng nâng cấp tài khoản."
            };
        }

        // 6. Xác thực mật khẩu băm BCrypt
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash);
        if (!isPasswordValid)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Tên đăng nhập hoặc mật khẩu không chính xác."
            };
        }

        // 7. Cấp phát Token JWT (có hiệu lực 1 ngày)
        var token = _jwtService.GenerateToken(user, account, out var expiresAt);

        // 8. Cập nhật thời điểm đăng nhập gần nhất
        account.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng '{Username}' ({Role}) đã đăng nhập thành công. Token hết hạn: {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC", 
            account.Username, account.Role, expiresAt);

        var expiresInSeconds = (int)Math.Max(0, (expiresAt - DateTime.UtcNow).TotalSeconds);

        Customer? loginCustomer = targetCustomer;
        if (loginCustomer == null && !string.IsNullOrWhiteSpace(user.CustomerId))
        {
            loginCustomer = await _dbContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == user.CustomerId, cancellationToken);
        }

        return new LoginResponse
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            ExpiresInSeconds = expiresInSeconds,
            User = MapToUserResponse(user, account, loginCustomer)
        };
    }

    public async Task<UserActionResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(currentUserId) || !_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực hoặc phiên đăng nhập đã hết hạn.");
        }

        // Tìm tài khoản theo UserId hoặc Id tài khoản
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == currentUserId || a.Id == currentUserId, cancellationToken);

        if (account == null && !string.IsNullOrWhiteSpace(_currentUserService.Username))
        {
            account = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Username.ToLower() == _currentUserService.Username.ToLower(), cancellationToken);
        }

        if (account == null)
        {
            throw new ArgumentException("Không tìm thấy thông tin tài khoản đăng nhập của người dùng hiện tại.");
        }

        // Kiểm tra mật khẩu hiện tại
        var isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash);
        if (!isOldPasswordValid)
        {
            throw new ArgumentException("Mật khẩu hiện tại không chính xác.");
        }

        // Kiểm tra mật khẩu mới không được trùng với mật khẩu cũ
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, account.PasswordHash))
        {
            throw new ArgumentException("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        // Cập nhật mật khẩu băm mới
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.Modified = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng '{Username}' (UserId: {UserId}) đã đổi mật khẩu thành công.", 
            account.Username, account.UserId);

        return new UserActionResponse
        {
            Success = true,
            Message = "Đổi mật khẩu thành công. Vui lòng sử dụng mật khẩu mới cho các lần đăng nhập tiếp theo."
        };
    }

    public async Task<UserActionResponse> AdminResetPasswordAsync(string targetUserIdOrUsername, string newPassword, bool unlockAccount = true, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra quyền Quản trị viên (Admin)
        var currentRole = _currentUserService.Role;
        if (!string.Equals(currentRole, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Chỉ có tài khoản có vai trò Quản trị viên (Admin) mới có quyền đặt lại mật khẩu.");
        }

        if (string.IsNullOrWhiteSpace(targetUserIdOrUsername))
        {
            throw new ArgumentException("Vui lòng cung cấp Id người dùng hoặc Tên đăng nhập cần đặt lại mật khẩu.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new ArgumentException("Mật khẩu mới phải có độ dài ít nhất 6 ký tự.");
        }

        var normalizedKey = targetUserIdOrUsername.Trim();

        // 2. Tìm tài khoản theo UserId, Id tài khoản, hoặc Username
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == normalizedKey || a.Id == normalizedKey || a.Username.ToLower() == normalizedKey.ToLower(), cancellationToken);

        if (account == null)
        {
            // Tìm theo email hoặc Id trong bảng Users
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == normalizedKey || (u.Email != null && u.Email.ToLower() == normalizedKey.ToLower()), cancellationToken);

            if (user != null)
            {
                account = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);
            }
        }

        if (account == null)
        {
            throw new ArgumentException($"Không tìm thấy tài khoản người dùng tương ứng với '{targetUserIdOrUsername}'.");
        }

        // 3. Đặt lại mật khẩu
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        if (unlockAccount && account.IsLocked)
        {
            account.IsLocked = false;
        }
        account.Modified = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Quản trị viên '{AdminUser}' đã đặt lại mật khẩu thành công cho tài khoản '{TargetUsername}' (UserId: {UserId}).",
            _currentUserService.Username, account.Username, account.UserId);

        return new UserActionResponse
        {
            Success = true,
            Message = $"Đã đặt lại mật khẩu thành công cho tài khoản '{account.Username}'."
        };
    }

    private static UserResponse MapToUserResponse(User user, Account? account = null, Customer? customer = null)
    {
        return new UserResponse
        {
            Id = user.Id ?? string.Empty,
            CustomerId = user.CustomerId ?? account?.CustomerId ?? customer?.Id,
            CustomerCode = customer?.Code,
            CustomerName = customer?.Name,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            Created = user.Created,
            CreatedById = user.CreatedById,
            CreatedByName = user.CreatedByName,
            Modified = user.Modified,
            ModifileById = user.ModifileById,
            ModifileByName = user.ModifileByName,
            Account = account == null ? null : new AccountResponse
            {
                Id = account.Id,
                Username = account.Username,
                Role = account.Role,
                TrialStartDate = account.TrialStartDate,
                TrialEndDate = account.TrialEndDate,
                IsLocked = account.IsLocked,
                LastLoginAt = account.LastLoginAt,
                Created = account.Created,
                CreatedById = account.CreatedById,
                CreatedByName = account.CreatedByName,
                Modified = account.Modified,
                ModifileById = account.ModifileById,
                ModifileByName = account.ModifileByName
            }
        };
    }
}

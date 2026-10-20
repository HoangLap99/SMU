using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Core.Enums;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Hiện thực hóa dịch vụ quản lý Khách hàng / Đơn vị và cán bộ (User và Account) trực thuộc đơn vị.
/// Quản lý hạn mức quota tài khoản.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CustomerService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllCustomersAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Code.ToLower().Contains(term));
        }

        var customers = await query
            .OrderByDescending(p => p.Created)
            .ToListAsync(cancellationToken);

        if (customers.Count == 0) return [];

        var customerIds = customers.Select(p => p.Id!).ToList();
        var allUsers = await _dbContext.Users.AsNoTracking()
            .Where(u => u.CustomerId != null && customerIds.Contains(u.CustomerId))
            .ToListAsync(cancellationToken);

        var allAccounts = await _dbContext.Accounts.AsNoTracking()
            .Where(a => a.CustomerId != null && customerIds.Contains(a.CustomerId))
            .ToListAsync(cancellationToken);
        var accountMap = allAccounts.Where(a => a.UserId != null).ToDictionary(a => a.UserId!, a => a);

        var goiThueBaoIds = customers.Select(p => p.GoiThueBaoId).Where(id => id != null).Distinct().ToList();
        var goiThueBaos = await _dbContext.GoiThueBaos.AsNoTracking()
            .Where(g => goiThueBaoIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id!, g => g.Ten, cancellationToken);

        var capBacIds = allUsers.Select(u => u.CapBacId).Where(id => id != null).Distinct().ToList();
        var vaiTroIds = allUsers.Select(u => u.VaiTroPhanQuyenId).Where(id => id != null).Distinct().ToList();
        var capBacs = await _dbContext.CapBacs.AsNoTracking()
            .Where(c => capBacIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id!, c => c.Ten, cancellationToken);
        var vaiTros = await _dbContext.VaiTroPhanQuyens.AsNoTracking()
            .Where(v => vaiTroIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id!, v => v.Ten, cancellationToken);

        var result = new List<CustomerResponse>();
        foreach (var customer in customers)
        {
            var pUsers = allUsers.Where(u => u.CustomerId == customer.Id).ToList();
            var activeUsers = pUsers.Where(u => u.IsActive).ToList();
            var members = pUsers.Select(u => MapToMemberResponse(
                u,
                accountMap.GetValueOrDefault(u.Id!),
                u.CapBacId != null ? capBacs.GetValueOrDefault(u.CapBacId) : null,
                u.VaiTroPhanQuyenId != null ? vaiTros.GetValueOrDefault(u.VaiTroPhanQuyenId) : null
            )).ToList();

            result.Add(new CustomerResponse
            {
                Id = customer.Id ?? string.Empty,
                Code = customer.Code,
                Name = customer.Name,
                Description = customer.Description,
                GoiThueBaoId = customer.GoiThueBaoId,
                GoiThueBaoTen = customer.GoiThueBaoId != null ? goiThueBaos.GetValueOrDefault(customer.GoiThueBaoId) : null,
                GioiHanUser = customer.GioiHanUser,
                ActiveMemberCount = activeUsers.Count,
                TotalMemberCount = pUsers.Count,
                StartDate = customer.StartDate,
                EndDate = customer.EndDate,
                Status = customer.Status,
                IsActive = customer.IsActive,
                Created = customer.Created,
                CreatedByName = customer.CreatedByName,
                Modified = customer.Modified,
                ModifiedByName = customer.ModifiedByName,
                Members = members
            });
        }

        return result;
    }

    public async Task<CustomerResponse?> GetCustomerByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (customer == null) return null;

        var goiThueBaoTen = customer.GoiThueBaoId != null
            ? await _dbContext.GoiThueBaos.AsNoTracking().Where(g => g.Id == customer.GoiThueBaoId).Select(g => g.Ten).FirstOrDefaultAsync(cancellationToken)
            : null;

        var users = await _dbContext.Users.AsNoTracking()
            .Where(u => u.CustomerId == id)
            .OrderBy(u => u.Created)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id!).ToList();
        var accounts = await _dbContext.Accounts.AsNoTracking()
            .Where(a => a.CustomerId == id && a.UserId != null && userIds.Contains(a.UserId))
            .ToDictionaryAsync(a => a.UserId!, a => a, cancellationToken);

        var capBacIds = users.Select(u => u.CapBacId).Where(c => c != null).Distinct().ToList();
        var vaiTroIds = users.Select(u => u.VaiTroPhanQuyenId).Where(v => v != null).Distinct().ToList();
        var capBacs = await _dbContext.CapBacs.AsNoTracking().Where(c => capBacIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id!, c => c.Ten, cancellationToken);
        var vaiTros = await _dbContext.VaiTroPhanQuyens.AsNoTracking().Where(v => vaiTroIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id!, v => v.Ten, cancellationToken);

        var activeCount = users.Count(u => u.IsActive);
        var members = users.Select(u => MapToMemberResponse(
            u,
            accounts.GetValueOrDefault(u.Id!),
            u.CapBacId != null ? capBacs.GetValueOrDefault(u.CapBacId) : null,
            u.VaiTroPhanQuyenId != null ? vaiTros.GetValueOrDefault(u.VaiTroPhanQuyenId) : null
        )).ToList();

        return new CustomerResponse
        {
            Id = customer.Id ?? string.Empty,
            Code = customer.Code,
            Name = customer.Name,
            Description = customer.Description,
            GoiThueBaoId = customer.GoiThueBaoId,
            GoiThueBaoTen = goiThueBaoTen,
            GioiHanUser = customer.GioiHanUser,
            ActiveMemberCount = activeCount,
            TotalMemberCount = users.Count,
            StartDate = customer.StartDate,
            EndDate = customer.EndDate,
            Status = customer.Status,
            IsActive = customer.IsActive,
            Created = customer.Created,
            CreatedByName = customer.CreatedByName,
            Modified = customer.Modified,
            ModifiedByName = customer.ModifiedByName,
            Members = members
        };
    }

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        // 1. Kiểm tra tính duy nhất của Mã khách hàng
        if (await _dbContext.Customers.AnyAsync(p => p.Code == normalizedCode, cancellationToken))
        {
            throw new ArgumentException($"Mã khách hàng '{request.Code}' đã tồn tại trong hệ thống.");
        }

        // 2. Xác định định mức tài khoản theo Gói thuê bao hoặc request
        var gioiHanUser = request.GioiHanUser ?? 10;
        if (!string.IsNullOrWhiteSpace(request.GoiThueBaoId))
        {
            var gtb = await _dbContext.GoiThueBaos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == request.GoiThueBaoId, cancellationToken);
            if (gtb != null && !request.GioiHanUser.HasValue)
            {
                gioiHanUser = gtb.GioiHanUser;
            }
        }

        // 3. Khởi tạo đối tượng Khách hàng
        var customer = new Customer
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            GoiThueBaoId = request.GoiThueBaoId,
            GioiHanUser = gioiHanUser,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            IsActive = true
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Bổ sung tài khoản ban đầu nếu có chỉ định trực tiếp qua CreateCustomerRequest
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var directMember = new AddUserToCustomerRequest
            {
                Username = request.Username,
                Password = request.Password,
                FullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : customer.Name,
                CapBacId = request.CapBacId,
                SoHieuCAND = request.SoHieuCAND,
                ChucVu = request.ChucVu,
                VaiTroPhanQuyenId = request.VaiTroPhanQuyenId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role ?? UserRole.AdminCustomer
            };
            await AddUserToCustomerAsync(customer.Id!, directMember, cancellationToken);
        }

        // 4.1. Bổ sung các cán bộ ban đầu nếu có chỉ định trong InitialMembers
        if (request.InitialMembers != null && request.InitialMembers.Count > 0)
        {
            foreach (var memberReq in request.InitialMembers)
            {
                // Tránh tạo trùng nếu username đã được tạo trực tiếp ở trên
                if (!string.IsNullOrWhiteSpace(request.Username) && 
                    string.Equals(memberReq.Username.Trim(), request.Username.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                await AddUserToCustomerAsync(customer.Id!, memberReq, cancellationToken);
            }
        }

        // 5. Tự động ghi nhận lịch sử đăng ký gói thuê bao ban đầu nếu có chọn gói
        if (!string.IsNullOrWhiteSpace(customer.GoiThueBaoId))
        {
            var gtb = await _dbContext.GoiThueBaos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == customer.GoiThueBaoId, cancellationToken);
            var initialHistory = new LichSuGoiThueBao
            {
                CustomerId = customer.Id!,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                GoiThueBaoId = customer.GoiThueBaoId,
                GoiThueBaoMa = gtb?.Ma,
                GoiThueBaoTen = gtb?.Ten ?? "Gói thuê bao khởi tạo",
                GioiHanUser = customer.GioiHanUser,
                LoaiGiaoDich = "DangKyMoi",
                LoaiGiaoDichTen = "Đăng ký mới",
                HinhThucGiaHan = "Đăng ký khởi tạo",
                NgayBatDau = customer.StartDate ?? DateTime.UtcNow,
                NgayKetThuc = customer.EndDate ?? (customer.StartDate ?? DateTime.UtcNow).AddYears(1),
                TrangThai = "HieuLuc",
                NguoiThucHien = _currentUserService.FullName ?? _currentUserService.Username ?? "System"
            };
            _dbContext.LichSuGoiThueBaos.Add(initialHistory);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Người dùng '{UserName}' ({UserId}) đã khởi tạo thành công khách hàng '{CustomerName}' (Code: {Code}, Quota: {Quota})",
            _currentUserService.Username ?? "N/A", _currentUserService.UserId ?? "N/A", customer.Name, customer.Code, customer.GioiHanUser);

        return (await GetCustomerByIdAsync(customer.Id!, cancellationToken))!;
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(string id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có Id: '{id}'.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            customer.Code = request.Code.Trim();
        }

        customer.Name = request.Name.Trim();
        customer.Description = request.Description?.Trim();
        customer.GoiThueBaoId = request.GoiThueBaoId;
        if (request.GioiHanUser.HasValue)
        {
            customer.GioiHanUser = request.GioiHanUser.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.GoiThueBaoId))
        {
            var gtb = await _dbContext.GoiThueBaos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == request.GoiThueBaoId, cancellationToken);
            if (gtb != null) customer.GioiHanUser = gtb.GioiHanUser;
        }

        customer.StartDate = request.StartDate;
        customer.EndDate = request.EndDate;
        customer.Status = request.Status;
        customer.IsActive = request.IsActive;

        // Cập nhật vai trò / thông tin tài khoản thuộc đơn vị nếu có yêu cầu
        if (request.Role.HasValue || !string.IsNullOrWhiteSpace(request.NewPassword) || !string.IsNullOrWhiteSpace(request.Username))
        {
            Account? targetAccount = null;
            User? targetUser = null;

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                targetAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => 
                    a.CustomerId == id && (a.UserId == request.UserId || a.Id == request.UserId), cancellationToken);
                targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => 
                    u.CustomerId == id && u.Id == request.UserId, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var normalizedUser = request.Username.Trim().ToLowerInvariant();
                targetAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => 
                    a.CustomerId == id && a.Username.ToLower() == normalizedUser, cancellationToken);
                if (targetAccount?.UserId != null)
                {
                    targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => 
                        u.CustomerId == id && u.Id == targetAccount.UserId, cancellationToken);
                }
            }
            else
            {
                // Ưu tiên tìm tài khoản AdminCustomer hoặc Admin, hoặc tài khoản đầu tiên thuộc đơn vị này
                targetAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => 
                    a.CustomerId == id && (a.Role == UserRole.AdminCustomer || a.Role == UserRole.Admin), cancellationToken)
                    ?? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CustomerId == id, cancellationToken);
                if (targetAccount?.UserId != null)
                {
                    targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => 
                        u.CustomerId == id && u.Id == targetAccount.UserId, cancellationToken);
                }
            }

            if (targetAccount != null)
            {
                if (request.Role.HasValue)
                {
                    targetAccount.Role = request.Role.Value;
                    if (request.Role.Value == UserRole.Trial && !targetAccount.TrialEndDate.HasValue)
                    {
                        targetAccount.TrialStartDate = DateTime.UtcNow;
                        targetAccount.TrialEndDate = DateTime.UtcNow.AddDays(14);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    targetAccount.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                if (targetUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.FullName)) targetUser.FullName = request.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(request.CapBacId)) targetUser.CapBacId = request.CapBacId;
                    if (!string.IsNullOrWhiteSpace(request.ChucVu)) targetUser.ChucVu = request.ChucVu.Trim();
                    if (!string.IsNullOrWhiteSpace(request.VaiTroPhanQuyenId)) targetUser.VaiTroPhanQuyenId = request.VaiTroPhanQuyenId;
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.Username))
            {
                // Nếu đơn vị chưa có tài khoản và caller muốn tạo mới tài khoản với Role được chọn
                var addReq = new AddUserToCustomerRequest
                {
                    Username = request.Username,
                    Password = request.NewPassword,
                    FullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : customer.Name,
                    CapBacId = request.CapBacId,
                    ChucVu = request.ChucVu,
                    VaiTroPhanQuyenId = request.VaiTroPhanQuyenId,
                    Role = request.Role ?? UserRole.AdminCustomer
                };
                await AddUserToCustomerAsync(id, addReq, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng '{UserName}' đã cập nhật khách hàng '{CustomerName}' ({Id})",
            _currentUserService.Username ?? "N/A", customer.Name, customer.Id);

        return (await GetCustomerByIdAsync(id, cancellationToken))!;
    }

    public async Task<bool> DeleteCustomerAsync(string id, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (customer == null) return false;

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng '{UserName}' đã xóa khách hàng '{CustomerName}' ({Id})",
            _currentUserService.Username ?? "N/A", customer.Name, id);

        return true;
    }

    public async Task<CustomerQuotaResponse> GetCustomerQuotaAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == customerId, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có Id: '{customerId}'.");
        }

        var dangHoatDong = await _dbContext.Users.CountAsync(u => u.CustomerId == customerId && u.IsActive, cancellationToken);
        var ngungKichHoat = await _dbContext.Users.CountAsync(u => u.CustomerId == customerId && !u.IsActive, cancellationToken);

        return new CustomerQuotaResponse
        {
            CustomerId = customer.Id!,
            CustomerName = customer.Name,
            GioiHanUser = customer.GioiHanUser,
            DangHoatDong = dangHoatDong,
            NgungKichHoat = ngungKichHoat,
            TongSo = dangHoatDong + ngungKichHoat
        };
    }

    public async Task<IReadOnlyList<CustomerMemberResponse>> GetCustomerMembersAsync(
        string customerId, 
        string? searchKeyword = null, 
        bool? isActive = null, 
        CancellationToken cancellationToken = default)
    {
        var customerExists = await _dbContext.Customers.AnyAsync(p => p.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có Id: '{customerId}'.");
        }

        var query = _dbContext.Users.AsNoTracking().Where(u => u.CustomerId == customerId);

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var users = await query.OrderBy(u => u.Created).ToListAsync(cancellationToken);
        if (users.Count == 0) return [];

        var userIds = users.Select(u => u.Id!).ToList();
        var accounts = await _dbContext.Accounts.AsNoTracking()
            .Where(a => a.CustomerId == customerId && a.UserId != null && userIds.Contains(a.UserId))
            .ToDictionaryAsync(a => a.UserId!, a => a, cancellationToken);

        var capBacIds = users.Select(u => u.CapBacId).Where(id => id != null).Distinct().ToList();
        var vaiTroIds = users.Select(u => u.VaiTroPhanQuyenId).Where(id => id != null).Distinct().ToList();

        var capBacs = await _dbContext.CapBacs.AsNoTracking()
            .Where(c => capBacIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id!, c => c.Ten, cancellationToken);
        var vaiTros = await _dbContext.VaiTroPhanQuyens.AsNoTracking()
            .Where(v => vaiTroIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id!, v => v.Ten, cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            users = users.Where(u =>
            {
                var username = accounts.TryGetValue(u.Id!, out var acc) ? acc.Username.ToLower() : "";
                return u.FullName.ToLower().Contains(kw) ||
                       username.Contains(kw) ||
                       (u.SoHieuCAND != null && u.SoHieuCAND.ToLower().Contains(kw)) ||
                       (u.ChucVu != null && u.ChucVu.ToLower().Contains(kw));
            }).ToList();
        }

        return users.Select(u => MapToMemberResponse(
            u, 
            accounts.GetValueOrDefault(u.Id!), 
            u.CapBacId != null ? capBacs.GetValueOrDefault(u.CapBacId) : null, 
            u.VaiTroPhanQuyenId != null ? vaiTros.GetValueOrDefault(u.VaiTroPhanQuyenId) : null
        )).ToList();
    }

    public async Task<CustomerMemberResponse> AddUserToCustomerAsync(
        string customerId, 
        AddUserToCustomerRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra sự tồn tại của Khách hàng / Đơn vị
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(p => p.Id == customerId, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có Id: '{customerId}'.");
        }

        // 2. Kiểm tra định mức Quota tài khoản đang hoạt động của đơn vị
        var activeCount = await _dbContext.Users.CountAsync(u => u.CustomerId == customerId && u.IsActive, cancellationToken);
        if (activeCount >= customer.GioiHanUser)
        {
            throw new InvalidOperationException($"Đơn vị đã đạt định mức tài khoản tối đa ({activeCount}/{customer.GioiHanUser}). Vui lòng nâng cấp gói thuê bao hoặc ngừng kích hoạt tài khoản khác.");
        }

        // 3. Kiểm tra tính duy nhất của Tên đăng nhập (Username):
        // - Không được trùng với bất kỳ tài khoản nào có Role = Admin
        // - Khác CustomerId thì được đặt trùng, nhưng trong cùng CustomerId thì không được trùng
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var isAdminDuplicate = await _dbContext.Accounts.AnyAsync(a => 
            a.Role == UserRole.Admin && a.Username.ToLower() == normalizedUsername, 
            cancellationToken);

        if (isAdminDuplicate)
        {
            throw new ArgumentException($"Tên đăng nhập '{request.Username}' trùng với tài khoản Quản trị viên (Admin) của hệ thống.");
        }

        var isDuplicateInSameCustomer = await _dbContext.Accounts.AnyAsync(a => 
            a.CustomerId == customerId && a.Username.ToLower() == normalizedUsername, 
            cancellationToken);

        if (isDuplicateInSameCustomer)
        {
            throw new ArgumentException($"Tên đăng nhập '{request.Username}' đã tồn tại trong đơn vị / khách hàng này.");
        }

        // 4. Tạo thông tin Người dùng (User) gắn theo Customer (lưu ID thuần túy)
        var user = new User
        {
            CustomerId = customerId,
            FullName = request.FullName.Trim(),
            CapBacId = request.CapBacId,
            SoHieuCAND = request.SoHieuCAND?.Trim(),
            ChucVu = request.ChucVu?.Trim(),
            VaiTroPhanQuyenId = request.VaiTroPhanQuyenId,
            Email = request.Email?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true
        };

        _dbContext.Users.Add(user);

        // 5. Tạo tài khoản đăng nhập (Account) gắn theo User và Customer
        var rawPassword = string.IsNullOrWhiteSpace(request.Password) ? "MatKhau123@" : request.Password;
        var role = request.Role;
        var account = new Account
        {
            CustomerId = customerId,
            UserId = user.Id,
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Role = role,
            TrialStartDate = role == UserRole.Trial ? DateTime.UtcNow : null,
            TrialEndDate = role == UserRole.Trial ? DateTime.UtcNow.AddDays(14) : null,
            IsLocked = false
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng '{CurrentUserName}' đã thêm cán bộ '{FullName}' (Username: {Username}) vào khách hàng '{CustomerName}'",
            _currentUserService.Username ?? "N/A", user.FullName, account.Username, customer.Name);

        // Nạp tên Cấp bậc và Vai trò phân quyền
        string? capBacTen = null;
        if (!string.IsNullOrWhiteSpace(user.CapBacId))
        {
            capBacTen = await _dbContext.CapBacs.AsNoTracking().Where(c => c.Id == user.CapBacId).Select(c => c.Ten).FirstOrDefaultAsync(cancellationToken);
        }

        string? vaiTroTen = null;
        if (!string.IsNullOrWhiteSpace(user.VaiTroPhanQuyenId))
        {
            vaiTroTen = await _dbContext.VaiTroPhanQuyens.AsNoTracking().Where(v => v.Id == user.VaiTroPhanQuyenId).Select(v => v.Ten).FirstOrDefaultAsync(cancellationToken);
        }

        return MapToMemberResponse(user, account, capBacTen, vaiTroTen);
    }

    public async Task<CustomerMemberResponse> UpdateUserInCustomerAsync(
        string customerId, 
        string userId, 
        UpdateUserInCustomerRequest request, 
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.CustomerId == customerId && u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy cán bộ có Id: '{userId}' trong đơn vị: '{customerId}'.");
        }

        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.UserId == userId, cancellationToken);

        user.FullName = request.FullName.Trim();
        user.CapBacId = request.CapBacId;
        user.SoHieuCAND = request.SoHieuCAND?.Trim();
        user.ChucVu = request.ChucVu?.Trim();
        user.VaiTroPhanQuyenId = request.VaiTroPhanQuyenId;
        user.Email = request.Email?.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        if (!string.IsNullOrWhiteSpace(request.NewPassword) && account != null)
        {
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        if (request.Role.HasValue && account != null)
        {
            account.Role = request.Role.Value;
            if (request.Role.Value == UserRole.Trial && !account.TrialEndDate.HasValue)
            {
                account.TrialStartDate = DateTime.UtcNow;
                account.TrialEndDate = DateTime.UtcNow.AddDays(14);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã cập nhật thông tin cán bộ '{FullName}' trong đơn vị {CustomerId}", user.FullName, customerId);

        string? capBacTen = null;
        if (!string.IsNullOrWhiteSpace(user.CapBacId))
        {
            capBacTen = await _dbContext.CapBacs.AsNoTracking().Where(c => c.Id == user.CapBacId).Select(c => c.Ten).FirstOrDefaultAsync(cancellationToken);
        }

        string? vaiTroTen = null;
        if (!string.IsNullOrWhiteSpace(user.VaiTroPhanQuyenId))
        {
            vaiTroTen = await _dbContext.VaiTroPhanQuyens.AsNoTracking().Where(v => v.Id == user.VaiTroPhanQuyenId).Select(v => v.Ten).FirstOrDefaultAsync(cancellationToken);
        }

        return MapToMemberResponse(user, account, capBacTen, vaiTroTen);
    }

    public async Task<CustomerMemberResponse> ToggleUserStatusAsync(
        string customerId, 
        string userId, 
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.CustomerId == customerId && u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy cán bộ có Id: '{userId}' trong đơn vị: '{customerId}'.");
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(p => p.Id == customerId, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có Id: '{customerId}'.");
        }

        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.UserId == userId, cancellationToken);

        // Nếu chuyển từ Ngừng hoạt động -> Kích hoạt, phải kiểm tra Quota
        if (!user.IsActive)
        {
            var activeCount = await _dbContext.Users.CountAsync(u => u.CustomerId == customerId && u.IsActive, cancellationToken);
            if (activeCount >= customer.GioiHanUser)
            {
                throw new InvalidOperationException($"Đơn vị đã đạt định mức tài khoản tối đa ({activeCount}/{customer.GioiHanUser}). Không thể kích hoạt thêm cán bộ.");
            }

            user.IsActive = true;
            if (account != null)
            {
                account.IsLocked = false;
            }

            _logger.LogInformation("Đã kích hoạt lại tài khoản cán bộ '{FullName}' ({UserId}) trong đơn vị '{CustomerName}'",
                user.FullName, user.Id, customer.Name);
        }
        else
        {
            // Ngừng kích hoạt => không chiếm quota
            user.IsActive = false;
            if (account != null)
            {
                account.IsLocked = true;
            }

            _logger.LogInformation("Đã ngừng kích hoạt tài khoản cán bộ '{FullName}' ({UserId}) trong đơn vị '{CustomerName}'",
                user.FullName, user.Id, customer.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        string? capBacTen = null;
        if (!string.IsNullOrWhiteSpace(user.CapBacId))
        {
            capBacTen = await _dbContext.CapBacs.AsNoTracking().Where(c => c.Id == user.CapBacId).Select(c => c.Ten).FirstOrDefaultAsync(cancellationToken);
        }

        string? vaiTroTen = null;
        if (!string.IsNullOrWhiteSpace(user.VaiTroPhanQuyenId))
        {
            vaiTroTen = await _dbContext.VaiTroPhanQuyens.AsNoTracking().Where(v => v.Id == user.VaiTroPhanQuyenId).Select(v => v.Ten).FirstOrDefaultAsync(cancellationToken);
        }

        return MapToMemberResponse(user, account, capBacTen, vaiTroTen);
    }

    public async Task<bool> RemoveUserFromCustomerAsync(string customerId, string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.CustomerId == customerId && u.Id == userId, cancellationToken);
        if (user == null) return false;

        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.UserId == userId, cancellationToken);
        if (account != null)
        {
            _dbContext.Accounts.Remove(account);
        }
        _dbContext.Users.Remove(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã xóa tài khoản cán bộ '{UserId}' khỏi khách hàng '{CustomerId}'", userId, customerId);
        return true;
    }

    private static CustomerMemberResponse MapToMemberResponse(
        User user, 
        Account? account, 
        string? capBacTen, 
        string? vaiTroTen)
    {
        return new CustomerMemberResponse
        {
            UserId = user.Id ?? string.Empty,
            AccountId = account?.Id,
            CustomerId = user.CustomerId ?? string.Empty,
            Username = account?.Username ?? string.Empty,
            Role = account?.Role ?? UserRole.User,
            FullName = user.FullName,
            CapBacId = user.CapBacId,
            CapBacTen = capBacTen,
            SoHieuCAND = user.SoHieuCAND,
            ChucVu = user.ChucVu,
            VaiTroPhanQuyenId = user.VaiTroPhanQuyenId,
            VaiTroPhanQuyenTen = vaiTroTen,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            Created = user.Created,
            CreatedByName = user.CreatedByName,
            Modified = user.Modified,
            ModifiedByName = user.ModifiedByName
        };
    }

    #region Nghiệp vụ Gia hạn & Xem Lịch sử Gói thuê bao (Subscription Renewal & History)

    public async Task<LichSuGoiThueBaoResponse> GiaHanGoiThueBaoAsync(
        string? customerId, 
        GiaHanGoiThueBaoRequest request, 
        CancellationToken cancellationToken = default)
    {
        var targetCustomerId = customerId ?? request.CustomerId ?? _currentUserService.CustomerId;
        if (string.IsNullOrWhiteSpace(targetCustomerId))
        {
            throw new ArgumentException("Vui lòng cung cấp mã khách hàng / đơn vị cần gia hạn gói thuê bao.");
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == targetCustomerId, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng / đơn vị có Id: '{targetCustomerId}'.");
        }

        // Xác định Gói thuê bao áp dụng (dùng gói mới nếu có, hoặc giữ nguyên gói hiện tại của customer)
        var targetGoiThueBaoId = request.GoiThueBaoId ?? customer.GoiThueBaoId;
        if (string.IsNullOrWhiteSpace(targetGoiThueBaoId))
        {
            throw new ArgumentException("Khách hàng chưa có gói thuê bao và yêu cầu không chỉ định gói thuê bao cần đăng ký.");
        }

        var package = await _dbContext.GoiThueBaos.FirstOrDefaultAsync(g => g.Id == targetGoiThueBaoId, cancellationToken);
        if (package == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy gói thuê bao có Id: '{targetGoiThueBaoId}'.");
        }

        // Xác định loại giao dịch (Đăng ký mới, Gia hạn, Nâng cấp, Hạ cấp, Đổi gói)
        string loaiGiaoDich;
        string loaiGiaoDichTen;

        if (string.IsNullOrWhiteSpace(customer.GoiThueBaoId))
        {
            loaiGiaoDich = "DangKyMoi";
            loaiGiaoDichTen = "Đăng ký mới";
        }
        else if (!string.IsNullOrWhiteSpace(request.GoiThueBaoId) && request.GoiThueBaoId != customer.GoiThueBaoId)
        {
            if (package.GioiHanUser > customer.GioiHanUser)
            {
                loaiGiaoDich = "NangCapGoi";
                loaiGiaoDichTen = "Nâng cấp gói thuê bao";
            }
            else if (package.GioiHanUser < customer.GioiHanUser)
            {
                loaiGiaoDich = "HaCapGoi";
                loaiGiaoDichTen = "Hạ cấp gói thuê bao";
            }
            else
            {
                loaiGiaoDich = "DoiGoi";
                loaiGiaoDichTen = "Đổi gói thuê bao";
            }
        }
        else
        {
            loaiGiaoDich = "GiaHan";
            loaiGiaoDichTen = "Gia hạn gói thuê bao";
        }

        // Tính toán thời gian bắt đầu và kết thúc kỳ gia hạn
        var now = DateTime.UtcNow;
        var oldEndDate = customer.EndDate;

        DateTime startDate;
        if (request.NgayBatDauMoi.HasValue)
        {
            startDate = request.NgayBatDauMoi.Value;
        }
        else if (oldEndDate.HasValue && oldEndDate.Value > now)
        {
            // Nếu gói cũ vẫn còn hạn: nối tiếp từ ngày hết hạn cũ
            startDate = oldEndDate.Value;
        }
        else
        {
            // Nếu đã hết hạn hoặc chưa có: bắt đầu từ thời điểm hiện tại
            startDate = now;
        }

        DateTime newEndDate;
        if (request.NgayKetThucMoi.HasValue)
        {
            newEndDate = request.NgayKetThucMoi.Value;
        }
        else
        {
            int months = request.SoThang ?? 12;
            newEndDate = startDate.AddMonths(months);
        }

        if (newEndDate <= startDate)
        {
            throw new ArgumentException("Ngày kết thúc gia hạn phải lớn hơn ngày bắt đầu.");
        }

        // Xác định định mức tài khoản cán bộ mới
        int newGioiHanUser = request.GioiHanUser ?? package.GioiHanUser;
        if (newGioiHanUser <= 0) newGioiHanUser = 10;

        // Cập nhật thông tin Customer
        customer.GoiThueBaoId = package.Id;
        customer.GioiHanUser = newGioiHanUser;
        customer.StartDate ??= startDate;
        customer.EndDate = newEndDate;
        customer.Status = CustomerStatus.InProgress;
        customer.IsActive = true;

        // Tạo bản ghi lưu vết lịch sử gia hạn
        var history = new LichSuGoiThueBao
        {
            CustomerId = customer.Id!,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            GoiThueBaoId = package.Id!,
            GoiThueBaoMa = package.Ma,
            GoiThueBaoTen = package.Ten,
            GioiHanUser = newGioiHanUser,
            LoaiGiaoDich = loaiGiaoDich,
            LoaiGiaoDichTen = loaiGiaoDichTen,
            HinhThucGiaHan = request.SoThang.HasValue ? $"Theo {request.SoThang} tháng" : "Tùy chọn",
            SoThang = request.SoThang,
            NgayBatDau = startDate,
            NgayKetThuc = newEndDate,
            NgayKetThucCu = oldEndDate,
            SoTien = request.SoTien,
            SoHopDong = request.SoHopDong?.Trim(),
            GhiChu = request.GhiChu?.Trim(),
            TrangThai = "HieuLuc",
            NguoiThucHien = _currentUserService.FullName ?? _currentUserService.Username ?? "System"
        };

        _dbContext.LichSuGoiThueBaos.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã thực hiện '{LoaiGiaoDichTen}' cho khách hàng '{CustomerName}' ({CustomerId}): Hạn mới đến {NewEndDate}, Quota: {Quota}",
            loaiGiaoDichTen, customer.Name, customer.Id, newEndDate.ToString("dd/MM/yyyy"), newGioiHanUser);

        return MapToLichSuResponse(history);
    }

    public async Task<ThongTinGoiThueBaoCustomerResponse> LayThongTinVaLichSuGoiThueBaoAsync(
        string? customerId, 
        CancellationToken cancellationToken = default)
    {
        var targetCustomerId = customerId ?? _currentUserService.CustomerId;
        if (string.IsNullOrWhiteSpace(targetCustomerId))
        {
            throw new ArgumentException("Vui lòng cung cấp mã khách hàng / đơn vị cần xem thông tin gói thuê bao.");
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == targetCustomerId, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng / đơn vị có Id: '{targetCustomerId}'.");
        }

        var package = customer.GoiThueBaoId != null 
            ? await _dbContext.GoiThueBaos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == customer.GoiThueBaoId, cancellationToken) 
            : null;

        int dangHoatDong = await _dbContext.Users.AsNoTracking()
            .CountAsync(u => u.CustomerId == customer.Id && u.IsActive, cancellationToken);

        var historyList = await _dbContext.LichSuGoiThueBaos.AsNoTracking()
            .Where(h => h.CustomerId == customer.Id)
            .OrderByDescending(h => h.Created)
            .ToListAsync(cancellationToken);

        // Hỗ trợ dữ liệu lịch sử cho các khách hàng đã có gói trước khi bảng LichSuGoiThueBao được tạo
        if (historyList.Count == 0 && !string.IsNullOrWhiteSpace(customer.GoiThueBaoId))
        {
            var initialHistory = new LichSuGoiThueBao
            {
                CustomerId = customer.Id!,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                GoiThueBaoId = customer.GoiThueBaoId,
                GoiThueBaoMa = package?.Ma,
                GoiThueBaoTen = package?.Ten ?? "Gói thuê bao khởi tạo",
                GioiHanUser = customer.GioiHanUser,
                LoaiGiaoDich = "DangKyMoi",
                LoaiGiaoDichTen = "Đăng ký mới",
                HinhThucGiaHan = "Đăng ký ban đầu",
                NgayBatDau = customer.StartDate ?? customer.Created,
                NgayKetThuc = customer.EndDate ?? (customer.StartDate ?? customer.Created).AddYears(1),
                TrangThai = "HieuLuc",
                NguoiThucHien = customer.CreatedByName ?? "System"
            };

            _dbContext.LichSuGoiThueBaos.Add(initialHistory);
            await _dbContext.SaveChangesAsync(cancellationToken);
            historyList.Add(initialHistory);
        }

        return new ThongTinGoiThueBaoCustomerResponse
        {
            CustomerId = customer.Id!,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            GoiThueBaoId = customer.GoiThueBaoId,
            GoiThueBaoMa = package?.Ma,
            GoiThueBaoTen = package?.Ten,
            GioiHanUser = customer.GioiHanUser,
            DangHoatDong = dangHoatDong,
            StartDate = customer.StartDate,
            EndDate = customer.EndDate,
            LichSu = historyList.Select(MapToLichSuResponse).ToList()
        };
    }

    public async Task<IReadOnlyList<LichSuGoiThueBaoResponse>> LayLichSuGoiThueBaoAsync(
        string? customerId, 
        CancellationToken cancellationToken = default)
    {
        var response = await LayThongTinVaLichSuGoiThueBaoAsync(customerId, cancellationToken);
        return response.LichSu;
    }

    private static LichSuGoiThueBaoResponse MapToLichSuResponse(LichSuGoiThueBao item)
    {
        return new LichSuGoiThueBaoResponse
        {
            Id = item.Id ?? string.Empty,
            CustomerId = item.CustomerId,
            CustomerCode = item.CustomerCode,
            CustomerName = item.CustomerName,
            GoiThueBaoId = item.GoiThueBaoId,
            GoiThueBaoMa = item.GoiThueBaoMa,
            GoiThueBaoTen = item.GoiThueBaoTen,
            GioiHanUser = item.GioiHanUser,
            LoaiGiaoDich = item.LoaiGiaoDich,
            LoaiGiaoDichTen = item.LoaiGiaoDichTen ?? item.LoaiGiaoDich,
            HinhThucGiaHan = item.HinhThucGiaHan,
            SoThang = item.SoThang,
            NgayBatDau = item.NgayBatDau,
            NgayKetThuc = item.NgayKetThuc,
            NgayKetThucCu = item.NgayKetThucCu,
            SoTien = item.SoTien,
            SoHopDong = item.SoHopDong,
            GhiChu = item.GhiChu,
            TrangThai = item.TrangThai,
            NguoiThucHien = item.NguoiThucHien,
            Created = item.Created,
            CreatedByName = item.CreatedByName
        };
    }

    #endregion
}

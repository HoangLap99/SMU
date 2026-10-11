using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý Khách hàng / Đơn vị và danh sách cán bộ (User và Account) trực thuộc.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Lấy danh sách toàn bộ khách hàng / đơn vị.
    /// </summary>
    Task<IReadOnlyList<CustomerResponse>> GetAllCustomersAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết một khách hàng / đơn vị theo Id.
    /// </summary>
    Task<CustomerResponse?> GetCustomerByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới một khách hàng / đơn vị (có thể thêm sẵn cán bộ ban đầu).
    /// </summary>
    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin khách hàng / đơn vị.
    /// </summary>
    Task<CustomerResponse> UpdateCustomerAsync(string id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một khách hàng / đơn vị.
    /// </summary>
    Task<bool> DeleteCustomerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin định mức tài khoản (Quota) của đơn vị.
    /// </summary>
    Task<CustomerQuotaResponse> GetCustomerQuotaAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách cán bộ thuộc đơn vị, hỗ trợ tìm kiếm và lọc trạng thái hoạt động.
    /// </summary>
    Task<IReadOnlyList<CustomerMemberResponse>> GetCustomerMembersAsync(
        string customerId, 
        string? searchKeyword = null, 
        bool? isActive = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm tài khoản cán bộ mới vào đơn vị (kiểm tra hạn mức Quota, tạo đồng thời User và Account).
    /// </summary>
    Task<CustomerMemberResponse> AddUserToCustomerAsync(string customerId, AddUserToCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin cán bộ trong đơn vị.
    /// </summary>
    Task<CustomerMemberResponse> UpdateUserInCustomerAsync(string customerId, string userId, UpdateUserInCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của cán bộ (Kích hoạt / Ngừng kích hoạt).
    /// Khi kích hoạt sẽ kiểm tra hạn mức Quota của đơn vị.
    /// </summary>
    Task<CustomerMemberResponse> ToggleUserStatusAsync(string customerId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa cán bộ khỏi đơn vị.
    /// </summary>
    Task<bool> RemoveUserFromCustomerAsync(string customerId, string userId, CancellationToken cancellationToken = default);
}

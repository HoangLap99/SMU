using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SMU.Application.Interfaces;

namespace SMU.Infrastructure.Identity;

/// <summary>
/// Hiện thực hóa ICurrentUserService lấy danh tính người dùng hiện tại từ HttpContext.User (ClaimsPrincipal).
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? CurrentUser => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        CurrentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        CurrentUser?.FindFirst("sub")?.Value;

    public string? CustomerId =>
        CurrentUser?.FindFirst("CustomerId")?.Value ??
        CurrentUser?.FindFirst("customerId")?.Value;

    public string? Username =>
        CurrentUser?.FindFirst(ClaimTypes.Name)?.Value ??
        CurrentUser?.FindFirst("unique_name")?.Value ??
        CurrentUser?.Identity?.Name;

    public string? Email =>
        CurrentUser?.FindFirst(ClaimTypes.Email)?.Value ??
        CurrentUser?.FindFirst("email")?.Value;

    public string? Role =>
        CurrentUser?.FindFirst(ClaimTypes.Role)?.Value ??
        CurrentUser?.FindFirst("role")?.Value;

    public string? FullName =>
        CurrentUser?.FindFirst("FullName")?.Value;

    public bool IsAuthenticated =>
        CurrentUser?.Identity?.IsAuthenticated ?? false;
}

using System.Security.Claims;

namespace SMU.Extensions;

/// <summary>
/// Các phương thức mở rộng cho đối tượng ClaimsPrincipal để dễ dàng truy xuất thông tin người dùng từ Controller hoặc Filter.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        principal.FindFirst("sub")?.Value;

    public static string? GetUsername(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Name)?.Value ??
        principal.FindFirst("unique_name")?.Value ??
        principal.Identity?.Name;

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Email)?.Value ??
        principal.FindFirst("email")?.Value;

    public static string? GetRole(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Role)?.Value ??
        principal.FindFirst("role")?.Value;

    public static string? GetFullName(this ClaimsPrincipal principal) =>
        principal.FindFirst("FullName")?.Value;

    public static string? GetCustomerId(this ClaimsPrincipal principal) =>
        principal.FindFirst("CustomerId")?.Value ??
        principal.FindFirst("customerId")?.Value;
}

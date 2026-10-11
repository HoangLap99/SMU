using System.ComponentModel.DataAnnotations;

namespace SMU.Infrastructure.Identity.Configuration;

/// <summary>
/// Cấu hình các thông số xác thực JWT từ appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Khóa bí mật ký token (tối thiểu 256-bit).
    /// </summary>
    [Required(ErrorMessage = "SecretKey cho JWT không được để trống.")]
    [MinLength(32, ErrorMessage = "SecretKey cho JWT phải có ít nhất 32 ký tự để đảm bảo an toàn HMAC-SHA256.")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Đơn vị phát hành token.
    /// </summary>
    [Required(ErrorMessage = "Issuer không được để trống.")]
    public string Issuer { get; set; } = "SMU_API";

    /// <summary>
    /// Đối tượng sử dụng token.
    /// </summary>
    [Required(ErrorMessage = "Audience không được để trống.")]
    public string Audience { get; set; } = "SMU_Client";

    /// <summary>
    /// Thời hạn hiệu lực của token tính theo ngày (mặc định: 1 ngày).
    /// </summary>
    [Range(1, 365, ErrorMessage = "ExpiryDays phải từ 1 đến 365 ngày.")]
    public int ExpiryDays { get; set; } = 1;
}

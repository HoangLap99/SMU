using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Identity.Configuration;

namespace SMU.Infrastructure.Identity;

/// <summary>
/// Dịch vụ khởi tạo token JWT xác thực theo chuẩn HMAC-SHA256 với thời hạn 1 ngày.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> options, ILogger<JwtService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public string GenerateToken(User user, Account account, out DateTime expiresAt)
    {
        var expiryDays = _settings.ExpiryDays > 0 ? _settings.ExpiryDays : 1;
        expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        var keyBytes = Encoding.UTF8.GetBytes(_settings.SecretKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var customerId = user.CustomerId ?? account.CustomerId;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, account.Role.ToString()),
            new("FullName", user.FullName),
            new("AccountId", account.Id ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(customerId))
        {
            claims.Add(new("CustomerId", customerId));
            claims.Add(new("customerId", customerId));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("Đã cấp phát JWT Token thành công cho người dùng '{Username}' (UserId: {UserId}), hết hạn vào {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC",
            account.Username, user.Id, expiresAt);

        return tokenString;
    }
}

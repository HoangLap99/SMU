using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SMU.Infrastructure.Data;

/// <summary>
/// Quản lý kiểm tra cấu trúc cơ sở dữ liệu khi ứng dụng khởi động.
/// Không tự động thêm bất kỳ dữ liệu nào (danh mục, tài khoản, đợt nhập, xuất kho) vào database.
/// Toàn bộ dữ liệu được quản lý và nhập thủ công qua giao diện người dùng.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Đảm bảo cấu trúc cột và chỉ mục trong cơ sở dữ liệu.
    /// Tuyệt đối không can thiệp hoặc thêm dữ liệu vào database.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<PostgresDbContext>>();

        try
        {
            // Đảm bảo cấu trúc cột DaXuatKho trong bảng XuatKhoBienSo và XacNhanHoanTat trong bảng KiemKeKho
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"XuatKhoBienSo\" ADD COLUMN IF NOT EXISTS \"DaXuatKho\" boolean NOT NULL DEFAULT false; " +
                "CREATE INDEX IF NOT EXISTS \"IX_XuatKhoBienSo_DaXuatKho\" ON \"XuatKhoBienSo\" (\"DaXuatKho\"); " +
                "ALTER TABLE \"KiemKeKho\" ADD COLUMN IF NOT EXISTS \"XacNhanHoanTat\" boolean NOT NULL DEFAULT false; " +
                "CREATE INDEX IF NOT EXISTS \"IX_KiemKeKho_XacNhanHoanTat\" ON \"KiemKeKho\" (\"XacNhanHoanTat\");"
            );
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Lỗi khi kiểm tra cấu trúc cột DaXuatKho / XacNhanHoanTat trong cơ sở dữ liệu");
        }
    }
}

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
                "CREATE INDEX IF NOT EXISTS \"IX_KiemKeKho_XacNhanHoanTat\" ON \"KiemKeKho\" (\"XacNhanHoanTat\"); " +
                "CREATE TABLE IF NOT EXISTS \"LichSuGoiThueBao\" (" +
                "\"Id\" character varying(36) NOT NULL PRIMARY KEY, " +
                "\"CustomerId\" character varying(36) NOT NULL, " +
                "\"CustomerCode\" character varying(50), " +
                "\"CustomerName\" character varying(250), " +
                "\"GoiThueBaoId\" character varying(36) NOT NULL, " +
                "\"GoiThueBaoMa\" character varying(50), " +
                "\"GoiThueBaoTen\" character varying(250) NOT NULL, " +
                "\"GioiHanUser\" integer NOT NULL DEFAULT 1, " +
                "\"LoaiGiaoDich\" character varying(50) NOT NULL DEFAULT 'GiaHan', " +
                "\"LoaiGiaoDichTen\" character varying(100), " +
                "\"HinhThucGiaHan\" character varying(100), " +
                "\"SoThang\" integer, " +
                "\"NgayBatDau\" timestamp without time zone NOT NULL, " +
                "\"NgayKetThuc\" timestamp without time zone NOT NULL, " +
                "\"NgayKetThucCu\" timestamp without time zone, " +
                "\"SoTien\" numeric(18,2), " +
                "\"SoHopDong\" character varying(100), " +
                "\"GhiChu\" character varying(1000), " +
                "\"TrangThai\" character varying(50) NOT NULL DEFAULT 'HieuLuc', " +
                "\"NguoiThucHien\" character varying(250), " +
                "\"Created\" timestamp without time zone NOT NULL, " +
                "\"CreatedById\" character varying(36), " +
                "\"CreatedByName\" character varying(250), " +
                "\"Modified\" timestamp without time zone NOT NULL, " +
                "\"ModifileById\" character varying(36), " +
                "\"ModifileByName\" character varying(250), " +
                "\"TenantId\" character varying(50)" +
                "); " +
                "CREATE INDEX IF NOT EXISTS \"IX_LichSuGoiThueBao_CustomerId\" ON \"LichSuGoiThueBao\" (\"CustomerId\"); " +
                "CREATE INDEX IF NOT EXISTS \"IX_LichSuGoiThueBao_Created\" ON \"LichSuGoiThueBao\" (\"Created\"); " +
                "UPDATE \"KhoBienSo\" k SET \"NgayNhapKho\" = d.\"NgayNhapKho\" FROM \"DotNhapKho\" d WHERE k.\"DotNhapKhoId\" = d.\"Id\" AND k.\"NgayNhapKho\" != d.\"NgayNhapKho\";"
            );

            // Đồng bộ trạng thái các biển số chưa xuất kho thành 'DuocBam' và 'Biển được bấm (Tồn kho)'
            await dbContext.Database.ExecuteSqlRawAsync(
                "DO $$ " +
                "DECLARE v_status_id VARCHAR(36); " +
                "BEGIN " +
                "    SELECT \"Id\" INTO v_status_id FROM \"TrangThaiBienSo\" WHERE \"Ma\" = 'DUOC_BAM' OR LOWER(\"Ten\") LIKE '%được bấm%' LIMIT 1; " +
                "    UPDATE \"KhoBienSo\" " +
                "    SET \"TrangThai\" = 'DuocBam', " +
                "        \"TrangThaiTen\" = 'Biển được bấm (Tồn kho)', " +
                "        \"TrangThaiBienSoId\" = COALESCE(v_status_id, \"TrangThaiBienSoId\") " +
                "    WHERE \"DaXuatKho\" = false AND (\"TrangThai\" != 'DuocBam' OR \"TrangThaiTen\" != 'Biển được bấm (Tồn kho)'); " +
                "    UPDATE \"ChiTietDotNhapBienSo\" ct " +
                "    SET \"TrangThaiBienSoTen\" = 'Biển được bấm (Tồn kho)', " +
                "        \"MauSacHienThi\" = 'success', " +
                "        \"TrangThaiBienSoId\" = COALESCE(v_status_id, ct.\"TrangThaiBienSoId\") " +
                "    FROM \"KhoBienSo\" k " +
                "    WHERE (k.\"ChiTietDotNhapBienSoId\" = ct.\"Id\" OR (k.\"DotNhapKhoId\" = ct.\"DotNhapKhoId\" AND LOWER(k.\"BienKiemSoat\") = LOWER(ct.\"BienKiemSoat\"))) " +
                "      AND k.\"DaXuatKho\" = false " +
                "      AND (ct.\"TrangThaiBienSoTen\" != 'Biển được bấm (Tồn kho)' OR ct.\"MauSacHienThi\" != 'success'); " +
                "END $$;"
            );
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Lỗi khi kiểm tra cấu trúc cơ sở dữ liệu và đồng bộ trạng thái biển số");
        }
    }
}

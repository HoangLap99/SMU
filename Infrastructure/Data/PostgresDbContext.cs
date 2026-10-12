using Microsoft.EntityFrameworkCore;
using SMU.Application.Interfaces;
using SMU.Core.Entities;

namespace SMU.Infrastructure.Data;

/// <summary>
/// DbContext quản lý kết nối và thao tác với cơ sở dữ liệu PostgreSQL.
/// Tự động cập nhật ngày tạo (Created) và ngày sửa (Modified) qua ChangeTracker khi gọi SaveChanges/SaveChangesAsync.
/// </summary>
public class PostgresDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public PostgresDbContext(
        DbContextOptions<PostgresDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Bảng quản lý thông tin hồ sơ người dùng.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Bảng quản lý tài khoản đăng nhập và phân quyền.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Bảng quản lý thông tin các khách hàng / đơn vị.
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Bảng quản lý các đợt nhập kho biển số xe (vật tư quản lý kho).
    /// </summary>
    public DbSet<DotNhapKho> DotNhapKhos => Set<DotNhapKho>();

    /// <summary>
    /// Bảng quản lý kho biển số xe (vật tư biển số tồn kho).
    /// </summary>
    public DbSet<KhoBienSo> KhoBienSos => Set<KhoBienSo>();

    /// <summary>
    /// Bảng danh sách chi tiết các biển số xe thuộc từng phiếu đợt nhập kho.
    /// </summary>
    public DbSet<ChiTietDotNhapBienSo> ChiTietDotNhapBienSos => Set<ChiTietDotNhapBienSo>();

    /// <summary>
    /// Bảng quản lý các phiếu xuất kho biển số xe và biên lai thu tiền.
    /// </summary>
    public DbSet<XuatKhoBienSo> XuatKhoBienSos => Set<XuatKhoBienSo>();

    /// <summary>
    /// Bảng quản lý biên lai cấp lại đăng ký xe (Thủ tục cấp lại không xuất kho, không giảm tồn biển).
    /// </summary>
    public DbSet<CapLaiDangKyXe> CapLaiDangKyXes => Set<CapLaiDangKyXe>();

    /// <summary>
    /// Bảng quản lý phiếu kiểm kê kho biển số xe.
    /// </summary>
    public DbSet<KiemKeKho> KiemKeKhos => Set<KiemKeKho>();

    /// <summary>
    /// Bảng chi tiết nhóm dải đầu biển trong phiếu kiểm kê kho.
    /// </summary>
    public DbSet<KiemKeKhoNhomDauBien> KiemKeKhoNhomDauBiens => Set<KiemKeKhoNhomDauBien>();

    /// <summary>
    /// Bảng chi tiết từng biển số thừa / thiếu trong phiếu kiểm kê kho.
    /// </summary>
    public DbSet<KiemKeKhoChiTietBien> KiemKeKhoChiTietBiens => Set<KiemKeKhoChiTietBien>();

    #region Danh mục hệ thống

    /// <summary>
    /// Bảng danh mục Cấp bậc (CapBac).
    /// </summary>
    public DbSet<CapBac> CapBacs => Set<CapBac>();

    /// <summary>
    /// Bảng danh mục Vai trò phân quyền (VaiTroPhanQuyen).
    /// </summary>
    public DbSet<VaiTroPhanQuyen> VaiTroPhanQuyens => Set<VaiTroPhanQuyen>();

    /// <summary>
    /// Bảng danh mục Đầu biển số (DauBienSo).
    /// </summary>
    public DbSet<DauBienSo> DauBienSos => Set<DauBienSo>();

    /// <summary>
    /// Bảng danh mục Màu biển số (mau_bien_so).
    /// </summary>
    public DbSet<MauBienSo> MauBienSos => Set<MauBienSo>();

    /// <summary>
    /// Bảng danh mục Trạng thái biển số (trang_thai_bien_so).
    /// </summary>
    public DbSet<TrangThaiBienSo> TrangThaiBienSos => Set<TrangThaiBienSo>();

    /// <summary>
    /// Bảng danh mục Loại xe (loai_xe).
    /// </summary>
    public DbSet<LoaiXe> LoaiXes => Set<LoaiXe>();

    /// <summary>
    /// Bảng danh mục Loại thủ tục (loai_thu_tuc).
    /// </summary>
    public DbSet<LoaiThuTuc> LoaiThuTucs => Set<LoaiThuTuc>();

    /// <summary>
    /// Bảng danh mục Khu vực áp dụng (khu_vuc_ap_dung).
    /// </summary>
    public DbSet<KhuVucApDung> KhuVucApDungs => Set<KhuVucApDung>();

    /// <summary>
    /// Bảng danh mục Ký hiệu quyển biên lai (ky_hieu_quyen_bien_lai).
    /// </summary>
    public DbSet<KyHieuQuyenBienLai> KyHieuQuyenBienLais => Set<KyHieuQuyenBienLai>();

    /// <summary>
    /// Bảng danh mục Tính chất đợt nhập (tinh_chat_dot_nhap).
    /// </summary>
    public DbSet<TinhChatDotNhap> TinhChatDotNhaps => Set<TinhChatDotNhap>();

    /// <summary>
    /// Bảng danh mục Gói thuê bao (goi_thue_bao).
    /// </summary>
    public DbSet<GoiThueBao> GoiThueBaos => Set<GoiThueBao>();

    /// <summary>
    /// Bảng danh mục Mức thu lệ phí (muc_thu_le_phi) theo Thông tư 155/2025/TT-BTC.
    /// </summary>
    public DbSet<MucThuLePhi> MucThuLePhis => Set<MucThuLePhi>();

    /// <summary>
    /// Bảng chi tiết mức thu lệ phí theo từng khu vực (muc_thu_le_phi_khu_vuc).
    /// </summary>
    public DbSet<MucThuLePhiKhuVuc> MucThuLePhiKhuVucs => Set<MucThuLePhiKhuVuc>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasIndex(a => new { a.CustomerId, a.Username }).IsUnique();
            entity.Property(a => a.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Status).HasConversion<string>();
        });

        #region Cấu hình các bảng danh mục (Table name không có dấu gạch dưới)

        modelBuilder.Entity<CapBac>(entity =>
        {
            entity.ToTable("CapBac");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<VaiTroPhanQuyen>(entity =>
        {
            entity.ToTable("VaiTroPhanQuyen");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<DauBienSo>(entity =>
        {
            entity.ToTable("DauBienSo");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<MauBienSo>(entity =>
        {
            entity.ToTable("MauBienSo");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<TrangThaiBienSo>(entity =>
        {
            entity.ToTable("TrangThaiBienSo");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<LoaiXe>(entity =>
        {
            entity.ToTable("LoaiXe");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<LoaiThuTuc>(entity =>
        {
            entity.ToTable("LoaiThuTuc");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<KhuVucApDung>(entity =>
        {
            entity.ToTable("KhuVucApDung");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<KyHieuQuyenBienLai>(entity =>
        {
            entity.ToTable("KyHieuQuyenBienLai");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<TinhChatDotNhap>(entity =>
        {
            entity.ToTable("TinhChatDotNhap");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<GoiThueBao>(entity =>
        {
            entity.ToTable("GoiThueBao");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
        });

        modelBuilder.Entity<MucThuLePhi>(entity =>
        {
            entity.ToTable("MucThuLePhi");
            entity.HasIndex(e => new { e.CustomerId, e.Ma }).IsUnique();
            entity.HasMany(e => e.ChiTietMucThuKhuVuc)
                .WithOne()
                .HasForeignKey(e => e.MucThuLePhiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MucThuLePhiKhuVuc>(entity =>
        {
            entity.ToTable("MucThuLePhikhuVuc");
            entity.HasIndex(e => new { e.MucThuLePhiId, e.KhuVucApDungId }).IsUnique();
        });

        #endregion

        #region Quản lý kho biển số xe (Vật tư đợt nhập)

        modelBuilder.Entity<DotNhapKho>(entity =>
        {
            entity.ToTable("DotNhapKho");
            entity.HasIndex(e => new { e.CustomerId, e.MaDotNhap });
            entity.HasIndex(e => e.NgayNhapKho);
        });

        modelBuilder.Entity<KhoBienSo>(entity =>
        {
            entity.ToTable("KhoBienSo");
            entity.HasIndex(e => new { e.CustomerId, e.BienKiemSoat });
            entity.HasIndex(e => e.DotNhapKhoId);
            entity.HasIndex(e => e.TrangThai);
        });

        modelBuilder.Entity<ChiTietDotNhapBienSo>(entity =>
        {
            entity.ToTable("ChiTietDotNhapBienSo");
            entity.HasIndex(e => new { e.CustomerId, e.DotNhapKhoId });
            entity.HasIndex(e => e.BienKiemSoat);
        });

        modelBuilder.Entity<XuatKhoBienSo>(entity =>
        {
            entity.ToTable("XuatKhoBienSo");
            entity.HasIndex(e => new { e.CustomerId, e.BienKiemSoat });
            entity.HasIndex(e => new { e.CustomerId, e.SoBienLai });
            entity.HasIndex(e => e.NgayThucHien);
            entity.HasIndex(e => e.KhoBienSoId);
            entity.HasIndex(e => e.DaXuatKho);
            entity.HasIndex(e => e.TrangThai);
        });

        modelBuilder.Entity<CapLaiDangKyXe>(entity =>
        {
            entity.ToTable("CapLaiDangKyXe");
            entity.HasIndex(e => new { e.CustomerId, e.BienKiemSoat });
            entity.HasIndex(e => new { e.CustomerId, e.SoBienLai });
            entity.HasIndex(e => new { e.KyHieuQuyenBienLaiId, e.SoBienLai });
            entity.HasIndex(e => e.NgayBienLai);
            entity.HasIndex(e => e.TrangThai);
        });

        modelBuilder.Entity<KiemKeKho>(entity =>
        {
            entity.ToTable("KiemKeKho");
            entity.HasIndex(e => new { e.CustomerId, e.MaPhieuKiemKe });
            entity.HasIndex(e => e.NgayKiemKe);
            entity.HasIndex(e => e.XacNhanHoanTat);
            entity.HasIndex(e => e.TrangThai);
            entity.HasMany(e => e.DanhSachNhomDauBien)
                .WithOne(e => e.KiemKeKho)
                .HasForeignKey(e => e.KiemKeKhoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.DanhSachChiTietBien)
                .WithOne(e => e.KiemKeKho)
                .HasForeignKey(e => e.KiemKeKhoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KiemKeKhoNhomDauBien>(entity =>
        {
            entity.ToTable("KiemKeKhoNhomDauBien");
            entity.HasIndex(e => new { e.CustomerId, e.KiemKeKhoId });
            entity.HasIndex(e => e.DauBien);
        });

        modelBuilder.Entity<KiemKeKhoChiTietBien>(entity =>
        {
            entity.ToTable("KiemKeKhoChiTietBien");
            entity.HasIndex(e => new { e.CustomerId, e.KiemKeKhoId });
            entity.HasIndex(e => e.BienKiemSoat);
            entity.HasIndex(e => e.KhoBienSoId);
        });

        #endregion
    }

    #region Tự động gắn Created khi thêm mới và Modified khi cập nhật bản ghi

    /// <summary>
    /// Ghi đè phương thức SaveChanges đồng bộ.
    /// Tự động cập nhật Created cho bản ghi thêm mới và Modified cho bản ghi cập nhật.
    /// </summary>
    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Ghi đè phương thức SaveChanges đồng bộ kèm cờ acceptAllChangesOnSuccess.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Ghi đè phương thức SaveChangesAsync bất đồng bộ.
    /// Tự động cập nhật Created cho bản ghi thêm mới và Modified cho bản ghi cập nhật.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ghi đè phương thức SaveChangesAsync bất đồng bộ kèm cờ acceptAllChangesOnSuccess.
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Phương thức SaveChangesAsync mở rộng cho phép truyền thêm thông tin người thực hiện thao tác (UserId, UserName).
    /// </summary>
    public Task<int> SaveChangesAsync(string? userId, string? userName = null, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps(userId, userName);
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Phương thức SaveChanges đồng bộ mở rộng cho phép truyền thêm thông tin người thực hiện thao tác (UserId, UserName).
    /// </summary>
    public int SaveChanges(string? userId, string? userName = null)
    {
        ApplyAuditTimestamps(userId, userName);
        return base.SaveChanges();
    }

    /// <summary>
    /// Hàm cốt lõi duyệt qua ChangeTracker:
    /// - Added: Gắn Id GUID (nếu chưa có), gán Created/Modified = DateTime.UtcNow, CreatedById, CreatedByName, ModifileById, ModifileByName.
    /// - Modified: Gán Modified = DateTime.UtcNow, ModifileById, ModifileByName theo user authen hiện tại, đồng thời khóa không cho sửa trường Created/CreatedById/CreatedByName.
    /// </summary>
    private void ApplyAuditTimestamps(string? currentUserId = null, string? currentUserName = null)
    {
        var now = DateTime.UtcNow;

        // 1. Tự động lấy danh tính người dùng từ Token JWT (Authentication) qua ICurrentUserService nếu không truyền trực tiếp
        if (string.IsNullOrWhiteSpace(currentUserId) && _currentUserService?.IsAuthenticated == true)
        {
            currentUserId = _currentUserService.UserId;
            currentUserName = !string.IsNullOrWhiteSpace(_currentUserService.FullName) 
                ? _currentUserService.FullName 
                : _currentUserService.Username;
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
            {
                continue;
            }

            // Chuẩn hóa tất cả DateTime/DateTime? có Kind == Unspecified sang UTC để tránh lỗi Npgsql
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.ClrType == typeof(DateTime) && prop.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                {
                    prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
                else if (prop.Metadata.ClrType == typeof(DateTime?) && prop.CurrentValue is DateTime dtNullable && dtNullable.Kind == DateTimeKind.Unspecified)
                {
                    prop.CurrentValue = DateTime.SpecifyKind(dtNullable, DateTimeKind.Utc);
                }
            }

            // 2. Áp dụng cho các thực thể kế thừa từ class Audit (Core/Entities/Audit.cs: User, Account, Project, ProjectMember)
            if (entry.Entity is Audit audit)
            {
                if (entry.State == EntityState.Added)
                {
                    // Tự động sinh Id GUID 36 ký tự nếu chưa được gán
                    if (string.IsNullOrWhiteSpace(audit.Id))
                    {
                        audit.Id = Guid.NewGuid().ToString();
                    }

                    // Tự động gán thời gian khởi tạo Created
                    if (audit.Created == default)
                    {
                        audit.Created = now;
                    }

                    // Bản ghi mới thì thời điểm Modified ban đầu bằng Created
                    audit.Modified = now;

                    // Tự động gán CreatedById và ModifileById từ User của Authentication
                    if (!string.IsNullOrWhiteSpace(currentUserId))
                    {
                        audit.CreatedById ??= currentUserId;
                        audit.ModifileById ??= currentUserId;
                    }

                    // Tự động gán CreatedByName và ModifileByName từ User của Authentication
                    if (!string.IsNullOrWhiteSpace(currentUserName))
                    {
                        audit.CreatedByName ??= currentUserName;
                        audit.ModifileByName ??= currentUserName;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Tự động cập nhật thời gian Modified theo thời điểm cập nhật bản ghi
                    audit.Modified = now;

                    // Tự động cập nhật người sửa gần nhất (ModifileById / ModifileByName)
                    if (!string.IsNullOrWhiteSpace(currentUserId))
                    {
                        audit.ModifileById = currentUserId;
                    }

                    if (!string.IsNullOrWhiteSpace(currentUserName))
                    {
                        audit.ModifileByName = currentUserName;
                    }

                    // Bảo vệ tuyệt đối các trường Created không bị ghi đè khi sửa bản ghi
                    entry.Property(nameof(Audit.Created)).IsModified = false;
                    entry.Property(nameof(Audit.CreatedById)).IsModified = false;
                    entry.Property(nameof(Audit.CreatedByName)).IsModified = false;
                }

                continue;
            }

            // 3. Dự phòng cho các thực thể khác không kế thừa Audit nhưng có các cột tương ứng
            if (entry.State == EntityState.Added)
            {
                SetPropertyValueIfPresent(entry, "Id", Guid.NewGuid().ToString(), onlyIfEmpty: true);
                SetPropertyValueIfPresent(entry, "Created", now, onlyIfEmpty: true);
                SetPropertyValueIfPresent(entry, "CreatedAt", now, onlyIfEmpty: true);
                SetPropertyValueIfPresent(entry, "Modified", now);
                SetPropertyValueIfPresent(entry, "UpdatedAt", now);

                if (!string.IsNullOrWhiteSpace(currentUserId))
                {
                    SetPropertyValueIfPresent(entry, "CreatedById", currentUserId, onlyIfEmpty: true);
                    SetPropertyValueIfPresent(entry, "ModifileById", currentUserId, onlyIfEmpty: true);
                    SetPropertyValueIfPresent(entry, "ModifiedById", currentUserId, onlyIfEmpty: true);
                }

                if (!string.IsNullOrWhiteSpace(currentUserName))
                {
                    SetPropertyValueIfPresent(entry, "CreatedByName", currentUserName, onlyIfEmpty: true);
                    SetPropertyValueIfPresent(entry, "ModifileByName", currentUserName, onlyIfEmpty: true);
                    SetPropertyValueIfPresent(entry, "ModifiedByName", currentUserName, onlyIfEmpty: true);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                SetPropertyValueIfPresent(entry, "Modified", now);
                SetPropertyValueIfPresent(entry, "UpdatedAt", now);

                if (!string.IsNullOrWhiteSpace(currentUserId))
                {
                    SetPropertyValueIfPresent(entry, "ModifileById", currentUserId);
                    SetPropertyValueIfPresent(entry, "ModifiedById", currentUserId);
                }

                if (!string.IsNullOrWhiteSpace(currentUserName))
                {
                    SetPropertyValueIfPresent(entry, "ModifileByName", currentUserName);
                    SetPropertyValueIfPresent(entry, "ModifiedByName", currentUserName);
                }

                PreventModificationIfPresent(entry, "Created");
                PreventModificationIfPresent(entry, "CreatedAt");
                PreventModificationIfPresent(entry, "CreatedById");
                PreventModificationIfPresent(entry, "CreatedByName");
            }
        }
    }

    private static void SetPropertyValueIfPresent(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object value, bool onlyIfEmpty = false)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        if (prop != null)
        {
            if (!onlyIfEmpty || prop.CurrentValue == null || string.IsNullOrWhiteSpace(prop.CurrentValue.ToString()) || prop.CurrentValue.Equals(default(DateTime)))
            {
                prop.CurrentValue = value;
            }
        }
    }

    private static void PreventModificationIfPresent(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        if (prop != null)
        {
            prop.IsModified = false;
        }
    }

    #endregion
}

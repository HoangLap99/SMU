using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Dịch vụ quản lý và truy vấn Nhật ký hoạt động / Audit Log cho 4 phân hệ:
/// 1. Nhập kho biển số xe (DotNhapKho)
/// 2. Xuất kho biển số xe &amp; Biên lai thu tiền (XuatKhoBienSo)
/// 3. Kiểm kê kho biển số xe (KiemKeKho)
/// 4. Cấp lại đăng ký xe (CapLaiDangKyXe)
/// </summary>
public class LogHoatDongService : ILogHoatDongService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LogHoatDongService> _logger;

    public LogHoatDongService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<LogHoatDongService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách nhật ký hoạt động có phân trang và bộ lọc linh hoạt.
    /// </summary>
    public async Task<PagedResult<LogHoatDongResponse>> LayDanhSachLogAsync(
        PagedLogHoatDongRequest request,
        CancellationToken cancellationToken = default)
    {
        request ??= new PagedLogHoatDongRequest();

        // 1. Nếu bảng log chưa có dữ liệu nào, thực hiện đồng bộ ban đầu từ các chứng từ có sẵn
        var hasLogs = await _dbContext.LogHoatDongs.AnyAsync(cancellationToken);
        if (!hasLogs)
        {
            await DongBoLogTuDuLieuCoSanAsync(cancellationToken);
        }

        var customerId = request.CustomerId ?? _currentUserService.CustomerId;
        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            customerId = await _dbContext.Users
                .Where(u => u.Id == _currentUserService.UserId)
                .Select(u => u.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var query = _dbContext.LogHoatDongs.AsNoTracking().AsQueryable();

        // Lọc theo đơn vị / khách hàng
        if (!string.IsNullOrEmpty(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        // Lọc theo Phân hệ / Loại nghiệp vụ
        if (!string.IsNullOrWhiteSpace(request.LoaiNghiepVu) && !request.LoaiNghiepVu.Equals("TatCa", StringComparison.OrdinalIgnoreCase))
        {
            var nv = request.LoaiNghiepVu.Trim();
            query = query.Where(x => x.LoaiNghiepVu.ToLower() == nv.ToLower());
        }

        // Lọc theo Hành động thao tác
        if (!string.IsNullOrWhiteSpace(request.HanhDong) && !request.HanhDong.Equals("TatCa", StringComparison.OrdinalIgnoreCase))
        {
            var hd = request.HanhDong.Trim();
            query = query.Where(x => x.HanhDong.ToLower() == hd.ToLower());
        }

        // Lọc theo ID chứng từ cụ thể
        if (!string.IsNullOrWhiteSpace(request.ChungTuId))
        {
            query = query.Where(x => x.ChungTuId == request.ChungTuId.Trim());
        }

        // Lọc theo Biển kiểm soát
        if (!string.IsNullOrWhiteSpace(request.BienKiemSoat))
        {
            var bks = request.BienKiemSoat.Trim().ToLower();
            query = query.Where(x => x.BienKiemSoat != null && x.BienKiemSoat.ToLower().Contains(bks));
        }

        // Lọc theo từ khóa tìm kiếm
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(x =>
                x.MaChungTu.ToLower().Contains(kw) ||
                (x.BienKiemSoat != null && x.BienKiemSoat.ToLower().Contains(kw)) ||
                x.NoiDung.ToLower().Contains(kw) ||
                (x.NguoiThucHien != null && x.NguoiThucHien.ToLower().Contains(kw)) ||
                (x.GhiChu != null && x.GhiChu.ToLower().Contains(kw)) ||
                (x.HanhDongTen != null && x.HanhDongTen.ToLower().Contains(kw)) ||
                (x.LoaiNghiepVuTen != null && x.LoaiNghiepVuTen.ToLower().Contains(kw)));
        }

        // Lọc từ ngày
        if (request.TuNgay.HasValue)
        {
            var tuNgay = request.TuNgay.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.TuNgay.Value, DateTimeKind.Utc)
                : request.TuNgay.Value.ToUniversalTime();
            query = query.Where(x => x.ThoiGian >= tuNgay);
        }

        // Lọc đến ngày
        if (request.DenNgay.HasValue)
        {
            var denNgay = request.DenNgay.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.DenNgay.Value, DateTimeKind.Utc)
                : request.DenNgay.Value.ToUniversalTime();
            // Nếu ngày đến không có giờ (00:00:00), lấy hết cuối ngày
            if (denNgay.TimeOfDay == TimeSpan.Zero)
            {
                denNgay = denNgay.Date.AddDays(1).AddTicks(-1);
            }
            query = query.Where(x => x.ThoiGian <= denNgay);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var currentPage = request.CurrentPage > 0 ? request.CurrentPage : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var items = await query
            .OrderByDescending(x => x.ThoiGian)
            .ThenByDescending(x => x.Created)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LogHoatDongResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Lấy toàn bộ lịch sử log thao tác của một chứng từ cụ thể (theo ChungTuId hoặc MaChungTu).
    /// </summary>
    public async Task<IReadOnlyList<LogHoatDongResponse>> LayLogTheoChungTuAsync(
        string chungTuId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chungTuId))
        {
            return Array.Empty<LogHoatDongResponse>();
        }

        var idTrim = chungTuId.Trim();
        var logs = await _dbContext.LogHoatDongs.AsNoTracking()
            .Where(x => x.ChungTuId == idTrim || x.MaChungTu == idTrim)
            .OrderByDescending(x => x.ThoiGian)
            .ThenByDescending(x => x.Created)
            .ToListAsync(cancellationToken);

        return logs.Select(MapToResponse).ToList();
    }

    /// <summary>
    /// Thống kê số lượng hoạt động theo từng phân hệ và mốc thời gian.
    /// </summary>
    public async Task<ThongKeLogHoatDongResponse> LayThongKeLogAsync(
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        customerId ??= _currentUserService.CustomerId;

        var query = _dbContext.LogHoatDongs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        var now = DateTime.UtcNow;
        var startOfToday = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var startOfWeek = DateTime.SpecifyKind(now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday), DateTimeKind.Utc);
        var startOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

        var tongSoLog = await query.CountAsync(cancellationToken);
        var soLogNhapKho = await query.CountAsync(x => x.LoaiNghiepVu == "NhapKho", cancellationToken);
        var soLogXuatKho = await query.CountAsync(x => x.LoaiNghiepVu == "XuatKho", cancellationToken);
        var soLogKiemKe = await query.CountAsync(x => x.LoaiNghiepVu == "KiemKe", cancellationToken);
        var soLogCapLai = await query.CountAsync(x => x.LoaiNghiepVu == "CapLaiDangKy", cancellationToken);

        var soLogHomNay = await query.CountAsync(x => x.ThoiGian >= startOfToday, cancellationToken);
        var soLogTrongTuan = await query.CountAsync(x => x.ThoiGian >= startOfWeek, cancellationToken);
        var soLogTrongThang = await query.CountAsync(x => x.ThoiGian >= startOfMonth, cancellationToken);

        return new ThongKeLogHoatDongResponse
        {
            TongSoLog = tongSoLog,
            SoLogNhapKho = soLogNhapKho,
            SoLogXuatKho = soLogXuatKho,
            SoLogKiemKe = soLogKiemKe,
            SoLogCapLaiDangKy = soLogCapLai,
            SoLogHomNay = soLogHomNay,
            SoLogTrongTuan = soLogTrongTuan,
            SoLogTrongThang = soLogTrongThang
        };
    }

    /// <summary>
    /// Ghi nhận một hành động nghiệp vụ vào nhật ký hoạt động.
    /// </summary>
    public async Task GhiLogAsync(
        string loaiNghiepVu,
        string hanhDong,
        string maChungTu,
        string noiDung,
        string? chungTuId = null,
        string? bienKiemSoat = null,
        string? trangThai = null,
        string? trangThaiTen = null,
        string? chiTietThayDoi = null,
        string? customerId = null,
        string? ghiChu = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            customerId ??= _currentUserService.CustomerId;
            var userId = _currentUserService.UserId;
            var userName = !string.IsNullOrWhiteSpace(_currentUserService.FullName)
                ? _currentUserService.FullName
                : _currentUserService.Username;

            string customerName = string.Empty;
            if (!string.IsNullOrEmpty(customerId))
            {
                customerName = await _dbContext.Customers
                    .Where(c => c.Id == customerId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            }

            var loaiNghiepVuTen = LayTenLoaiNghiepVu(loaiNghiepVu);
            var hanhDongTen = LayTenHanhDong(loaiNghiepVu, hanhDong);

            var logEntry = new LogHoatDong
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                CustomerName = customerName,
                LoaiNghiepVu = loaiNghiepVu,
                LoaiNghiepVuTen = loaiNghiepVuTen,
                HanhDong = hanhDong,
                HanhDongTen = hanhDongTen,
                ChungTuId = chungTuId,
                MaChungTu = maChungTu,
                BienKiemSoat = bienKiemSoat,
                NoiDung = noiDung,
                ChiTietThayDoi = chiTietThayDoi,
                TrangThai = trangThai ?? string.Empty,
                TrangThaiTen = trangThaiTen ?? LayTenTrangThai(trangThai),
                ThoiGian = DateTime.UtcNow,
                NguoiThucHienId = userId,
                NguoiThucHien = userName,
                GhiChu = ghiChu,
                Created = DateTime.UtcNow,
                CreatedById = userId,
                CreatedByName = userName,
                Modified = DateTime.UtcNow,
                ModifileById = userId,
                ModifileByName = userName
            };

            _dbContext.LogHoatDongs.Add(logEntry);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi nhật ký hoạt động: {LoaiNghiepVu} - {HanhDong} - {MaChungTu}", loaiNghiepVu, hanhDong, maChungTu);
        }
    }

    /// <summary>
    /// Đồng bộ / nạp các giao dịch có sẵn từ 4 bảng nghiệp vụ vào bảng LogHoatDong nếu chưa có.
    /// </summary>
    public async Task DongBoLogTuDuLieuCoSanAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existingChungTuIds = await _dbContext.LogHoatDongs
                .Where(x => x.ChungTuId != null)
                .Select(x => x.ChungTuId!)
                .Distinct()
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<string>(existingChungTuIds);
            var logsToAdd = new List<LogHoatDong>();

            // 1. Phân hệ Nhập kho
            var dotNhapKhos = await _dbContext.DotNhapKhos.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var item in dotNhapKhos)
            {
                if (item.Id != null && !existingSet.Contains(item.Id))
                {
                    var countBien = await _dbContext.ChiTietDotNhapBienSos
                        .Where(x => x.DotNhapKhoId == item.Id)
                        .CountAsync(cancellationToken);

                    var thoiGian = item.Created != default ? item.Created : item.NgayNhapKho;

                    logsToAdd.Add(new LogHoatDong
                    {
                        Id = Guid.NewGuid().ToString(),
                        CustomerId = item.CustomerId,
                        LoaiNghiepVu = "NhapKho",
                        LoaiNghiepVuTen = "Nhập kho",
                        HanhDong = item.DaNhapKho ? "XacNhan" : "ThemMoi",
                        HanhDongTen = item.DaNhapKho ? "Xác nhận nhập kho" : "Tạo mới đợt nhập",
                        ChungTuId = item.Id,
                        MaChungTu = item.MaDotNhap,
                        BienKiemSoat = item.KyHieuDaiDauBien,
                        NoiDung = item.DaNhapKho 
                            ? $"Đợt nhập kho '{item.MaDotNhap}' đã xác nhận nhập kho với {countBien} biển số."
                            : $"Tạo mới đợt nhập kho '{item.MaDotNhap}' với {countBien} biển số (Bản nháp).",
                        TrangThai = item.DaNhapKho ? "DaNhapKho" : "LuuTam",
                        TrangThaiTen = item.DaNhapKho ? "Đã nhập kho" : "Bản nháp",
                        ThoiGian = thoiGian,
                        NguoiThucHienId = item.CreatedById,
                        NguoiThucHien = item.CreatedByName ?? item.CanBoTiepNhan,
                        GhiChu = item.GhiChu,
                        Created = thoiGian,
                        CreatedById = item.CreatedById,
                        CreatedByName = item.CreatedByName,
                        Modified = item.Modified != default ? item.Modified : thoiGian
                    });
                }
            }

            // 2. Phân hệ Xuất kho
            var xuatKhos = await _dbContext.XuatKhoBienSos.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var item in xuatKhos)
            {
                if (item.Id != null && !existingSet.Contains(item.Id))
                {
                    string hanhDong = item.TrangThai == "DaHuy" ? "Huy" : (item.DaXuatKho ? "XacNhan" : "ThemMoi");
                    string hanhDongTen = item.TrangThai == "DaHuy" ? "Hủy phiếu xuất kho" : (item.DaXuatKho ? "Xác nhận xuất kho" : "Lập phiếu xuất kho");
                    var thoiGian = item.Created != default ? item.Created : item.NgayThucHien;

                    logsToAdd.Add(new LogHoatDong
                    {
                        Id = Guid.NewGuid().ToString(),
                        CustomerId = item.CustomerId,
                        LoaiNghiepVu = "XuatKho",
                        LoaiNghiepVuTen = "Xuất kho",
                        HanhDong = hanhDong,
                        HanhDongTen = hanhDongTen,
                        ChungTuId = item.Id,
                        MaChungTu = item.SoBienLai,
                        BienKiemSoat = item.BienKiemSoat,
                        NoiDung = $"Xuất kho biển số {item.BienKiemSoat} - Biên lai {item.KyHieuQuyen}-{item.SoBienLai} cho {item.HoTenNguoiNop}, thu {item.SoTienThu:N0}đ.",
                        TrangThai = item.TrangThai,
                        TrangThaiTen = item.TrangThai switch
                        {
                            "HoanTat" => "Hoàn tất",
                            "LuuTam" => "Lưu tạm",
                            "DaHuy" => "Đã hủy",
                            _ => item.TrangThai
                        },
                        ThoiGian = thoiGian,
                        NguoiThucHienId = item.CreatedById,
                        NguoiThucHien = item.CreatedByName,
                        GhiChu = item.GhiChu,
                        Created = thoiGian,
                        CreatedById = item.CreatedById,
                        CreatedByName = item.CreatedByName,
                        Modified = item.Modified != default ? item.Modified : thoiGian
                    });
                }
            }

            // 3. Phân hệ Kiểm kê kho
            var kiemKes = await _dbContext.KiemKeKhos.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var item in kiemKes)
            {
                if (item.Id != null && !existingSet.Contains(item.Id))
                {
                    string hanhDong = item.XacNhanHoanTat ? "XacNhan" : "ThemMoi";
                    string hanhDongTen = item.XacNhanHoanTat ? "Xác nhận hoàn tất kiểm kê" : "Khởi tạo phiếu kiểm kê";
                    var thoiGian = item.Created != default ? item.Created : item.NgayKiemKe;

                    logsToAdd.Add(new LogHoatDong
                    {
                        Id = Guid.NewGuid().ToString(),
                        CustomerId = item.CustomerId,
                        LoaiNghiepVu = "KiemKe",
                        LoaiNghiepVuTen = "Kiểm kê kho",
                        HanhDong = hanhDong,
                        HanhDongTen = hanhDongTen,
                        ChungTuId = item.Id,
                        MaChungTu = item.MaPhieuKiemKe,
                        NoiDung = $"Phiếu kiểm kê {item.MaPhieuKiemKe} ({item.LoaiPhuongTien}): Tồn hệ thống {item.TongTonHeThong}, Thực tế {item.TongThucTe}, Chênh lệch {item.TongChenhLech}.",
                        TrangThai = item.TrangThai,
                        TrangThaiTen = item.XacNhanHoanTat ? "Hoàn tất" : "Lưu nháp",
                        ThoiGian = thoiGian,
                        NguoiThucHienId = item.CreatedById,
                        NguoiThucHien = item.CreatedByName ?? item.CanBoLapBienBan,
                        GhiChu = item.GhiChu,
                        Created = thoiGian,
                        CreatedById = item.CreatedById,
                        CreatedByName = item.CreatedByName,
                        Modified = item.Modified != default ? item.Modified : thoiGian
                    });
                }
            }

            // 4. Phân hệ Cấp lại đăng ký xe
            var capLais = await _dbContext.CapLaiDangKyXes.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var item in capLais)
            {
                if (item.Id != null && !existingSet.Contains(item.Id))
                {
                    string hanhDong = item.DaXacNhan ? "XacNhan" : "ThemMoi";
                    string hanhDongTen = item.DaXacNhan ? "Xác nhận phát hành biên lai" : "Lập biên lai cấp lại";
                    var thoiGian = item.Created != default ? item.Created : item.NgayBienLai;

                    logsToAdd.Add(new LogHoatDong
                    {
                        Id = Guid.NewGuid().ToString(),
                        CustomerId = item.CustomerId,
                        LoaiNghiepVu = "CapLaiDangKy",
                        LoaiNghiepVuTen = "Cấp lại đăng ký xe",
                        HanhDong = hanhDong,
                        HanhDongTen = hanhDongTen,
                        ChungTuId = item.Id,
                        MaChungTu = item.SoBienLai,
                        BienKiemSoat = item.BienKiemSoat,
                        NoiDung = $"Biên lai cấp lại đăng ký xe {item.KyHieuQuyen}-{item.SoBienLai} - Biển {item.BienKiemSoat} cho {item.HoTenChuXe}, thu {item.SoTienThu:N0}đ.",
                        TrangThai = item.TrangThai,
                        TrangThaiTen = item.DaXacNhan ? "Đã xác nhận (Đã phát hành)" : "Bản nháp",
                        ThoiGian = thoiGian,
                        NguoiThucHienId = item.CreatedById,
                        NguoiThucHien = item.CreatedByName,
                        Created = thoiGian,
                        CreatedById = item.CreatedById,
                        CreatedByName = item.CreatedByName,
                        Modified = item.Modified != default ? item.Modified : thoiGian
                    });
                }
            }

            if (logsToAdd.Count > 0)
            {
                await _dbContext.LogHoatDongs.AddRangeAsync(logsToAdd, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Đã đồng bộ {Count} bản ghi nhật ký hoạt động từ dữ liệu có sẵn.", logsToAdd.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đồng bộ nhật ký hoạt động từ dữ liệu có sẵn");
        }
    }

    #region Helper Mapping Methods

    private static LogHoatDongResponse MapToResponse(LogHoatDong entity)
    {
        return new LogHoatDongResponse
        {
            Id = entity.Id ?? string.Empty,
            LoaiNghiepVu = entity.LoaiNghiepVu,
            LoaiNghiepVuTen = entity.LoaiNghiepVuTen,
            HanhDong = entity.HanhDong,
            HanhDongTen = entity.HanhDongTen,
            ChungTuId = entity.ChungTuId,
            MaChungTu = entity.MaChungTu,
            BienKiemSoat = entity.BienKiemSoat,
            NoiDung = entity.NoiDung,
            ChiTietThayDoi = entity.ChiTietThayDoi,
            TrangThai = entity.TrangThai,
            TrangThaiTen = entity.TrangThaiTen,
            ThoiGian = entity.ThoiGian,
            NguoiThucHienId = entity.NguoiThucHienId,
            NguoiThucHien = entity.NguoiThucHien,
            CustomerId = entity.CustomerId,
            CustomerName = entity.CustomerName,
            IpAddress = entity.IpAddress,
            GhiChu = entity.GhiChu
        };
    }

    private static string LayTenLoaiNghiepVu(string loaiNghiepVu)
    {
        return loaiNghiepVu switch
        {
            "NhapKho" => "Nhập kho",
            "XuatKho" => "Xuất kho",
            "KiemKe" => "Kiểm kê kho",
            "CapLaiDangKy" => "Cấp lại đăng ký xe",
            _ => loaiNghiepVu
        };
    }

    private static string LayTenHanhDong(string loaiNghiepVu, string hanhDong)
    {
        return (loaiNghiepVu, hanhDong) switch
        {
            ("NhapKho", "ThemMoi") => "Tạo mới đợt nhập",
            ("NhapKho", "CapNhat") => "Cập nhật đợt nhập",
            ("NhapKho", "XacNhan") => "Xác nhận nhập kho",
            ("NhapKho", "Xoa") => "Xóa đợt nhập kho",

            ("XuatKho", "ThemMoi") => "Lập phiếu xuất kho",
            ("XuatKho", "CapNhat") => "Cập nhật phiếu xuất kho",
            ("XuatKho", "XacNhan") => "Xác nhận xuất kho",
            ("XuatKho", "Huy") => "Hủy phiếu xuất kho",
            ("XuatKho", "Xoa") => "Xóa phiếu xuất kho",

            ("KiemKe", "ThemMoi") => "Khởi tạo phiếu kiểm kê",
            ("KiemKe", "CapNhat") => "Cập nhật phiếu kiểm kê",
            ("KiemKe", "XacNhan") => "Xác nhận hoàn tất kiểm kê",
            ("KiemKe", "Xoa") => "Xóa phiếu kiểm kê",

            ("CapLaiDangKy", "ThemMoi") => "Lập biên lai cấp lại",
            ("CapLaiDangKy", "CapNhat") => "Cập nhật biên lai cấp lại",
            ("CapLaiDangKy", "XacNhan") => "Xác nhận phát hành biên lai",
            ("CapLaiDangKy", "Huy") => "Hủy biên lai cấp lại",
            ("CapLaiDangKy", "Xoa") => "Xóa biên lai cấp lại",

            _ => hanhDong switch
            {
                "ThemMoi" => "Thêm mới",
                "CapNhat" => "Cập nhật",
                "XacNhan" => "Xác nhận",
                "Huy" => "Hủy",
                "Xoa" => "Xóa",
                _ => hanhDong
            }
        };
    }

    private static string LayTenTrangThai(string? trangThai)
    {
        if (string.IsNullOrWhiteSpace(trangThai)) return string.Empty;

        return trangThai switch
        {
            "LuuTam" => "Bản nháp",
            "LuuNhap" => "Bản nháp",
            "DaNhapKho" => "Đã nhập kho",
            "HoanTat" => "Hoàn tất",
            "DaXacNhan" => "Đã xác nhận",
            "DaHuy" => "Đã hủy",
            "DaXoa" => "Đã xóa",
            _ => trangThai
        };
    }

    #endregion
}

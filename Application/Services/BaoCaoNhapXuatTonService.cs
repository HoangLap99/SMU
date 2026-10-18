using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai Báo cáo Nhập - Xuất - Tồn Biển số xe:
/// Đồng bộ tuyệt đối giữa màn hình hiển thị và bản in A4.
/// </summary>
public class BaoCaoNhapXuatTonService : IBaoCaoNhapXuatTonService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BaoCaoNhapXuatTonService> _logger;

    public BaoCaoNhapXuatTonService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<BaoCaoNhapXuatTonService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy dữ liệu Báo cáo Nhập - Xuất - Tồn Biển số xe theo kỳ báo cáo và bộ lọc.
    /// </summary>
    public async Task<BaoCaoNhapXuatTonResponse> LayBaoCaoNhapXuatTonAsync(
        FilterBaoCaoNhapXuatTonRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoNhapXuatTonRequest();
        var (fromDate, toDate) = ResolveDateRange(request.GetTuNgay(), request.GetDenNgay(), request.LoaiThoiGian);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Lọc bảng KhoBienSo
        var query = _dbContext.KhoBienSos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        // Lọc theo từ khóa tìm số biển / keyword
        var searchKw = request.GetSearchKeyword();
        if (!string.IsNullOrWhiteSpace(searchKw))
        {
            var kwLower = searchKw.ToLower();
            query = query.Where(x => x.BienKiemSoat.ToLower().Contains(kwLower));
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        // Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            if (request.TrangThai == "DuocBam")
            {
                query = query.Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);
            }
            else if (request.TrangThai == "DaBam")
            {
                query = query.Where(x => x.DaXuatKho || x.TrangThai == "DaBam");
            }
            else if (request.TrangThai == "ChoDauGia")
            {
                query = query.Where(x => x.LaBienDauGia || x.TrangThai == "ChoDauGia");
            }
            else
            {
                query = query.Where(x => x.TrangThai == request.TrangThai);
            }
        }

        // Tải thông tin ngày nhập kho từ Đợt nhập kho để đảm bảo chuẩn xác tuyệt đối với ngày nghiệp vụ
        var dotNhapMap = await _dbContext.DotNhapKhos.AsNoTracking()
            .Where(d => d.Id != null)
            .ToDictionaryAsync(d => d.Id!, d => d.NgayNhapKho, cancellationToken);

        // Lấy danh sách biển số
        var rawPlates = await query
            .Select(x => new
            {
                x.BienKiemSoat,
                x.LoaiXeId,
                x.MauBienSoId,
                x.DauBienSoId,
                x.DotNhapKhoId,
                x.NgayNhapKho,
                x.DaXuatKho,
                x.NgayXuatKho
            })
            .ToListAsync(cancellationToken);

        var plates = rawPlates.Select(x => new
        {
            x.BienKiemSoat,
            x.LoaiXeId,
            x.MauBienSoId,
            x.DauBienSoId,
            x.DotNhapKhoId,
            NgayNhapKho = (x.DotNhapKhoId != null && dotNhapMap.TryGetValue(x.DotNhapKhoId, out var dnDate)) ? dnDate : x.NgayNhapKho,
            x.DaXuatKho,
            x.NgayXuatKho
        }).ToList();

        // 2. Tải danh mục tra cứu
        var loaiXeMap = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var mauBienMap = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => new { Ten = x.Ten ?? "Trắng", Hex = x.MaMauHex ?? "#FFFFFF" }, cancellationToken);

        var dauBienMap = await _dbContext.DauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.KyHieuSeries ?? x.Ten ?? "Đầu biển", cancellationToken);

        // Tìm các đợt nhập kho tính chất "Tồn đầu" nếu có
        var tinhChatTonDau = await _dbContext.TinhChatDotNhaps.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ma == "TON_DAU" || x.Ten.ToLower().Contains("tồn đầu"), cancellationToken);

        var tonDauDotNhapIds = new List<string>();
        if (tinhChatTonDau != null)
        {
            tonDauDotNhapIds = await _dbContext.DotNhapKhos.AsNoTracking()
                .Where(x => x.TinhChatDotNhapId == tinhChatTonDau.Id && x.Id != null)
                .Select(x => x.Id!)
                .ToListAsync(cancellationToken);
        }
        var tonDauSet = new HashSet<string>(tonDauDotNhapIds);

        // 3. Gom nhóm theo Đầu biển, Loại xe, Màu biển
        var groups = plates
            .GroupBy(p =>
            {
                var dauBienTen = (p.DauBienSoId != null && dauBienMap.TryGetValue(p.DauBienSoId, out var db)) ? db : null;
                var dauBien = ExtractDauBien(p.BienKiemSoat, dauBienTen);

                return new
                {
                    DauBien = dauBien,
                    LoaiXeId = p.LoaiXeId ?? string.Empty,
                    MauBienSoId = p.MauBienSoId ?? string.Empty
                };
            })
            .Select(g =>
            {
                // Tồn đầu kỳ: Tổng số biển đã nhập trừ đi tổng số biển đã xuất trước TuNgay (theo đúng yêu cầu người dùng)
                int tongNhapTruocKy = g.Count(x =>
                    x.NgayNhapKho < fromDate || (tonDauSet.Contains(x.DotNhapKhoId) && x.NgayNhapKho <= fromDate)
                );

                int tongXuatTruocKy = g.Count(x =>
                    x.DaXuatKho && x.NgayXuatKho != null && x.NgayXuatKho < fromDate
                );

                int tonDauKy = Math.Max(0, tongNhapTruocKy - tongXuatTruocKy);

                // Nhập trong kỳ: Nhập từ TuNgay đến DenNgay (không tính đợt tồn đầu đã vào Tồn đầu kỳ)
                int nhapTrongKy = g.Count(x =>
                    x.NgayNhapKho >= fromDate && x.NgayNhapKho <= toDate
                    && !(tonDauSet.Contains(x.DotNhapKhoId) && x.NgayNhapKho <= fromDate)
                );

                // Xuất trong kỳ: Đã xuất từ TuNgay đến DenNgay
                int xuatTrongKy = g.Count(x =>
                    x.DaXuatKho && x.NgayXuatKho != null
                    && x.NgayXuatKho >= fromDate && x.NgayXuatKho <= toDate
                );

                var loaiXeTen = loaiXeMap.TryGetValue(g.Key.LoaiXeId, out var lx) ? lx : "Ô tô";
                string mauBienTen = "Trắng";
                string mauBienHex = "#FFFFFF";
                if (mauBienMap.TryGetValue(g.Key.MauBienSoId, out var mb))
                {
                    mauBienTen = mb.Ten;
                    mauBienHex = mb.Hex;
                }

                return new BaoCaoNhapXuatTonItemDto
                {
                    DauBien = g.Key.DauBien,
                    LoaiXeId = string.IsNullOrEmpty(g.Key.LoaiXeId) ? null : g.Key.LoaiXeId,
                    LoaiXeTen = loaiXeTen,
                    MauBienSoId = string.IsNullOrEmpty(g.Key.MauBienSoId) ? null : g.Key.MauBienSoId,
                    MauBienSoTen = mauBienTen,
                    MauBienHex = mauBienHex,
                    TonDauKy = tonDauKy,
                    NhapTrongKy = nhapTrongKy,
                    XuatTrongKy = xuatTrongKy
                };
            });

        // Chỉ hiển thị các đầu biển có tồn đầu kỳ > 0 hoặc có biến động nhập/xuất trong kỳ
        groups = groups.Where(x => x.TonDauKy > 0 || x.NhapTrongKy > 0 || x.XuatTrongKy > 0 || x.TonCuoiKy > 0);

        // 4. Lọc theo từ khóa tìm kiếm Đầu biển nếu có
        var dauBienFilter = request.GetDauBienFilter();
        if (!string.IsNullOrWhiteSpace(dauBienFilter))
        {
            var filter = dauBienFilter.ToUpperInvariant();
            groups = groups.Where(x => x.DauBien.ToUpperInvariant().Contains(filter));
        }

        // 5. Sắp xếp thứ tự theo Đầu biển (như trong bảng thiết kế mẫu: 29A-036, 29AA-000, 29AC-963...)
        var sortedList = groups
            .OrderBy(x => x.DauBien)
            .ThenBy(x => x.LoaiXeTen)
            .ToList();

        // Gán STT
        int stt = 1;
        foreach (var item in sortedList)
        {
            item.Stt = stt++;
        }

        var tongCong = new BaoCaoNhapXuatTonTongCongDto
        {
            TongTonDauKy = sortedList.Sum(x => x.TonDauKy),
            TongNhapTrongKy = sortedList.Sum(x => x.NhapTrongKy),
            TongXuatTrongKy = sortedList.Sum(x => x.XuatTrongKy)
        };

        var displayFrom = fromDate.TimeOfDay.Hours >= 16 ? fromDate.AddHours(7) : fromDate;
        var displayTo = toDate.TimeOfDay.Hours >= 16 ? toDate.AddHours(7) : toDate;

        return new BaoCaoNhapXuatTonResponse
        {
            TieuDe = "BÁO CÁO NHẬP - XUẤT - TỒN BIỂN SỐ XE",
            KyBaoCaoHienThi = $"Kỳ báo cáo từ ngày {displayFrom:dd/MM/yyyy} đến ngày {displayTo:dd/MM/yyyy}",
            GhiChuInA4 = "Dữ liệu màn hình và bản in A4 được đồng bộ tuyệt đối theo các bộ lọc hiện tại.",
            TuNgay = fromDate,
            DenNgay = toDate,
            TongSoDong = sortedList.Count,
            Items = sortedList,
            TongCong = tongCong
        };
    }

    /// <summary>
    /// Lấy dữ liệu định dạng chuẩn sẵn sàng in ấn A4 (đồng bộ tuyệt đối với dữ liệu màn hình).
    /// </summary>
    public async Task<BaoCaoNhapXuatTonPrintDataDto> LayDuLieuInA4Async(
        FilterBaoCaoNhapXuatTonRequest? request,
        CancellationToken cancellationToken = default)
    {
        var data = await LayBaoCaoNhapXuatTonAsync(request, cancellationToken);
        var currentUser = _currentUserService.FullName ?? _currentUserService.Username ?? "Cán bộ quản lý kho";

        return new BaoCaoNhapXuatTonPrintDataDto
        {
            TenDonVi = "CÔNG AN THÀNH PHỐ HÀ NỘI",
            TenBoPhan = "ĐỘI CẢNH SÁT GIAO THÔNG - TRẬT TỰ",
            TieuDe = data.TieuDe,
            KyBaoCao = data.KyBaoCaoHienThi,
            NgayXuatBaoCao = DateTime.UtcNow,
            NguoiLapBieu = currentUser,
            Data = data
        };
    }

    /// <summary>
    /// Báo cáo 1 (Tab 3): Báo cáo Biển số tồn kho tại thời điểm.
    /// Chốt danh sách chi tiết các biển số đang tồn kho tính đến hết ngày được chọn.
    /// </summary>
    public async Task<BaoCaoTonTaiThoiDiemResponse> LayBaoCaoTonTaiThoiDiemAsync(
        FilterBaoCaoTonTaiThoiDiemRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoTonTaiThoiDiemRequest();
        var (fromDate, toDate) = ResolveDateRange(request.GetTuNgay(), request.GetDenNgay(), request.LoaiThoiGian);
        var chotNgayInput = request.GetChotNgay();
        var chotNgay = chotNgayInput.HasValue
            ? DateTime.SpecifyKind(chotNgayInput.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
            : (toDate != DateTime.MaxValue && toDate > fromDate ? toDate : new DateTime(2026, 8, 31, 23, 59, 59, 999, DateTimeKind.Utc));

        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => x.NgayNhapKho <= chotNgay && (!x.DaXuatKho || x.NgayXuatKho == null || x.NgayXuatKho > chotNgay));

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (request.GetTuNgay().HasValue || !string.IsNullOrWhiteSpace(request.LoaiThoiGian))
        {
            query = query.Where(x => x.NgayNhapKho >= fromDate);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        // Lọc theo trạng thái biển số
        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            if (request.TrangThai == "DuocBam")
            {
                query = query.Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);
            }
            else if (request.TrangThai == "DaBam")
            {
                query = query.Where(x => x.DaXuatKho || x.TrangThai == "DaBam");
            }
            else if (request.TrangThai == "ChoDauGia")
            {
                query = query.Where(x => x.LaBienDauGia || x.TrangThai == "ChoDauGia");
            }
            else
            {
                query = query.Where(x => x.TrangThai == request.TrangThai);
            }
        }

        var searchKw = request.GetSearchKeyword();
        if (!string.IsNullOrWhiteSpace(searchKw))
        {
            var filter = searchKw.ToUpperInvariant();
            query = query.Where(x => x.BienKiemSoat.ToUpper().Contains(filter));
        }

        var loaiXeMap = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var mauBienMap = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Trắng", cancellationToken);

        var dauBienMap = await _dbContext.DauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.KyHieuSeries ?? x.Ten ?? "Đầu biển", cancellationToken);

        var plates = await query
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.BienKiemSoat)
            .Select(x => new
            {
                x.BienKiemSoat,
                x.DauBienSoId,
                x.LoaiXeId,
                x.MauBienSoId,
                x.NgayNhapKho,
                x.TrangThaiTen,
                x.TrangThai
            })
            .ToListAsync(cancellationToken);

        int stt = 1;
        var items = plates.Select(p =>
        {
            var dauBienTen = (p.DauBienSoId != null && dauBienMap.TryGetValue(p.DauBienSoId, out var db)) ? db : null;
            var dauBien = ExtractDauBien(p.BienKiemSoat, dauBienTen);
            var loaiXeTen = (p.LoaiXeId != null && loaiXeMap.TryGetValue(p.LoaiXeId, out var lx)) ? lx : "Ô tô";
            var mauBienTen = (p.MauBienSoId != null && mauBienMap.TryGetValue(p.MauBienSoId, out var mb)) ? mb : "Trắng";
            var trangThai = !string.IsNullOrWhiteSpace(p.TrangThaiTen) ? p.TrangThaiTen : "Biển được bấm (Tồn kho)";

            return new BaoCaoTonTaiThoiDiemItemDto
            {
                Stt = stt++,
                BienKiemSoat = p.BienKiemSoat,
                DauBien = dauBien,
                LoaiXeTen = loaiXeTen,
                MauBienSoTen = mauBienTen,
                NgayNhapKho = p.NgayNhapKho,
                TrangThai = trangThai
            };
        }).ToList();

        // Lọc theo đầu biển nếu có
        var dauBienFilter = request.GetDauBienFilter();
        if (!string.IsNullOrWhiteSpace(dauBienFilter))
        {
            var filter = dauBienFilter.ToUpperInvariant();
            items = items.Where(x => x.DauBien.ToUpperInvariant().Contains(filter)).ToList();
            int reStt = 1;
            foreach (var item in items) item.Stt = reStt++;
        }

        return new BaoCaoTonTaiThoiDiemResponse
        {
            TieuDe = "BÁO CÁO BIẾN SỐ TỒN KHO TẠI THỜI ĐIỂM",
            ChotTonTaiNgayHienThi = $"Số liệu chốt đến hết ngày {chotNgay:dd/MM/yyyy}",
            ChotTonTaiNgay = chotNgay,
            TongSoDong = items.Count,
            TongSoLuongBienTon = items.Count,
            Items = items
        };
    }

    /// <summary>
    /// Báo cáo 2 (Tab 4): Báo cáo Tiền thu theo biên lai và thủ tục.
    /// </summary>
    public async Task<BaoCaoTienThuBienLaiResponse> LayBaoCaoTienThuBienLaiAsync(
        FilterBaoCaoTienThuBienLaiRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoTienThuBienLaiRequest();
        var (fromDate, toDate) = ResolveDateRange(request.GetTuNgay(), request.GetDenNgay(), request.LoaiThoiGian);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Biên lai từ Xuất kho biển số
        var xuatKhoQuery = _dbContext.XuatKhoBienSos.AsNoTracking()
            .Where(x => x.NgayBienLai >= fromDate && x.NgayBienLai <= toDate && x.TrangThai != "DaHuy");

        // 2. Biên lai từ Cấp lại đăng ký xe
        var capLaiQuery = _dbContext.CapLaiDangKyXes.AsNoTracking()
            .Where(x => x.NgayBienLai >= fromDate && x.NgayBienLai <= toDate && x.TrangThai != "DaHuy");

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            capLaiQuery = capLaiQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiThuTucId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.LoaiThuTucId == request.LoaiThuTucId);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
            capLaiQuery = capLaiQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.TrangThai == request.TrangThai);
            capLaiQuery = capLaiQuery.Where(x => x.TrangThai == request.TrangThai);
        }

        var xuatKhos = await xuatKhoQuery.ToListAsync(cancellationToken);
        var capLais = await capLaiQuery.ToListAsync(cancellationToken);

        var combinedItems = new List<BaoCaoTienThuBienLaiItemDto>();

        foreach (var x in xuatKhos)
        {
            string thuTuc = "Cấp biển";
            if (!string.IsNullOrWhiteSpace(x.LoaiThuTucTen))
            {
                if (x.LoaiThuTucTen.Contains("Sang tên", StringComparison.OrdinalIgnoreCase))
                    thuTuc = "Sang tên";
                else if (x.LoaiThuTucTen.Contains("Cấp lại", StringComparison.OrdinalIgnoreCase))
                    thuTuc = "Cấp lại";
                else if (x.LoaiThuTucTen.Contains("biên lai", StringComparison.OrdinalIgnoreCase))
                    thuTuc = "Chỉ ghi biên lai";
                else if (x.LoaiThuTucTen.Contains("Cấp biển", StringComparison.OrdinalIgnoreCase))
                    thuTuc = "Cấp biển";
                else
                    thuTuc = x.LoaiThuTucTen;
            }

            combinedItems.Add(new BaoCaoTienThuBienLaiItemDto
            {
                KyHieuSoBienLai = $"{x.KyHieuQuyen} - {x.SoBienLai}",
                NgayBienLai = x.NgayBienLai,
                ThuTuc = thuTuc,
                BienSo = x.BienKiemSoat,
                NguoiNopTien = x.HoTenNguoiNop,
                SoTienThu = x.SoTienThu
            });
        }

        foreach (var c in capLais)
        {
            string thuTuc = "Cấp lại";
            if ((!string.IsNullOrWhiteSpace(c.NoiDungThuPhi) && c.NoiDungThuPhi.Contains("Chỉ ghi biên lai", StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(c.LyDoCapLai) && c.LyDoCapLai.Contains("Chỉ ghi biên lai", StringComparison.OrdinalIgnoreCase)))
            {
                thuTuc = "Chỉ ghi biên lai";
            }
            else if (!string.IsNullOrWhiteSpace(c.NoiDungThuPhi) && c.NoiDungThuPhi.Contains("Sang tên", StringComparison.OrdinalIgnoreCase))
            {
                thuTuc = "Sang tên";
            }

            combinedItems.Add(new BaoCaoTienThuBienLaiItemDto
            {
                KyHieuSoBienLai = $"{c.KyHieuQuyen} - {c.SoBienLai}",
                NgayBienLai = c.NgayBienLai,
                ThuTuc = thuTuc,
                BienSo = c.BienKiemSoat,
                NguoiNopTien = c.HoTenChuXe,
                SoTienThu = c.SoTienThu
            });
        }

        // Lọc tìm kiếm số biển / keyword
        var searchKw = request.GetSearchKeyword();
        if (!string.IsNullOrWhiteSpace(searchKw))
        {
            var filter = searchKw.ToUpperInvariant();
            combinedItems = combinedItems.Where(x =>
                x.KyHieuSoBienLai.ToUpperInvariant().Contains(filter) ||
                x.BienSo.ToUpperInvariant().Contains(filter) ||
                x.NguoiNopTien.ToUpperInvariant().Contains(filter) ||
                x.ThuTuc.ToUpperInvariant().Contains(filter)
            ).ToList();
        }

        // Lọc theo đầu biển nếu có
        var dauBienFilter = request.GetDauBienFilter();
        if (!string.IsNullOrWhiteSpace(dauBienFilter))
        {
            var dbFilter = dauBienFilter.ToUpperInvariant();
            combinedItems = combinedItems.Where(x =>
                ExtractDauBien(x.BienSo, null).ToUpperInvariant().Contains(dbFilter)
            ).ToList();
        }

        // Sắp xếp theo ngày biên lai, số biên lai
        var sorted = combinedItems
            .OrderBy(x => x.NgayBienLai)
            .ThenBy(x => x.KyHieuSoBienLai)
            .ToList();

        int stt = 1;
        foreach (var item in sorted)
        {
            item.Stt = stt++;
        }

        decimal tongTien = sorted.Sum(x => x.SoTienThu);

        return new BaoCaoTienThuBienLaiResponse
        {
            TieuDe = "BÁO CÁO TIỀN THU THEO BIÊN LAI VÀ THỦ TỤC",
            KyBaoCaoHienThi = $"Từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}",
            TuNgay = fromDate,
            DenNgay = toDate,
            TongSoDong = sorted.Count,
            TongTienThuLePhi = tongTien,
            Items = sorted
        };
    }

    /// <summary>
    /// Báo cáo 3 (Tab 5): Bảng thống kê thủ tục và tiền thu.
    /// Tách số lượt hồ sơ, số biển xuất kho và tiền thu theo từng loại thủ tục.
    /// </summary>
    public async Task<BangThongKeThuTucResponse> LayBangThongKeThuTucAsync(
        FilterBangThongKeThuTucRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBangThongKeThuTucRequest();
        var (fromDate, toDate) = ResolveDateRange(request.GetTuNgay(), request.GetDenNgay(), request.LoaiThoiGian);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // Tái sử dụng logic lấy toàn bộ biên lai trong kỳ với đầy đủ bộ lọc
        var reportBienLai = await LayBaoCaoTienThuBienLaiAsync(new FilterBaoCaoTienThuBienLaiRequest
        {
            TuNgay = request.TuNgay,
            DenNgay = request.DenNgay,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            LoaiThoiGian = request.LoaiThoiGian,
            LoaiThuTucId = request.LoaiThuTucId,
            LoaiXeId = request.LoaiXeId,
            MauBienSoId = request.MauBienSoId,
            TrangThai = request.TrangThai,
            DauBien = request.DauBien,
            TimDauBien = request.TimDauBien,
            TimSoBien = request.TimSoBien,
            Keyword = request.Keyword,
            TimKiem = request.TimKiem,
            CustomerId = customerId
        }, cancellationToken);

        // Gom nhóm theo Loại thủ tục
        var groups = reportBienLai.Items
            .GroupBy(x => x.ThuTuc)
            .ToDictionary(g => g.Key, g => new
            {
                SoLuot = g.Count(),
                TongTien = g.Sum(x => x.SoTienThu)
            }, StringComparer.OrdinalIgnoreCase);

        var listResult = new List<BangThongKeThuTucItemDto>();
        var definedOrder = new[] { "Cấp biển", "Sang tên", "Cấp lại", "Chỉ ghi biên lai" };

        foreach (var thuTuc in definedOrder)
        {
            if (groups.TryGetValue(thuTuc, out var grp))
            {
                int soBienXuat = thuTuc.Equals("Cấp biển", StringComparison.OrdinalIgnoreCase) ? grp.SoLuot : 0;
                listResult.Add(new BangThongKeThuTucItemDto
                {
                    LoaiThuTuc = thuTuc,
                    SoLuotThucHien = grp.SoLuot,
                    SoBienXuatTuKho = soBienXuat,
                    TongTienThu = grp.TongTien
                });
                groups.Remove(thuTuc);
            }
        }

        // Bổ sung các thủ tục khác nếu có
        foreach (var kvp in groups)
        {
            listResult.Add(new BangThongKeThuTucItemDto
            {
                LoaiThuTuc = kvp.Key,
                SoLuotThucHien = kvp.Value.SoLuot,
                SoBienXuatTuKho = 0,
                TongTienThu = kvp.Value.TongTien
            });
        }

        int stt = 1;
        foreach (var item in listResult)
        {
            item.Stt = stt++;
        }

        decimal tongTienThu = listResult.Sum(x => x.TongTienThu);

        return new BangThongKeThuTucResponse
        {
            TieuDe = "BẢNG THỐNG KÊ THỦ TỤC VÀ TIỀN THU",
            MoTa = "Tách số lượt hồ sơ, số biển xuất kho và tiền thu",
            KyBaoCaoHienThi = $"Từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}",
            TuNgay = fromDate,
            DenNgay = toDate,
            TongSoDong = listResult.Count,
            TongSoLuongTienThu = tongTienThu,
            Items = listResult
        };
    }

    /// <summary>
    /// Báo cáo 4 (Tab 6): Bảng kê chi tiết đợt nhập biển số xe.
    /// </summary>
    public async Task<BangKeChiTietDotNhapResponse> LayBangKeChiTietDotNhapAsync(
        FilterBangKeChiTietDotNhapRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBangKeChiTietDotNhapRequest();
        var (fromDate, toDate) = ResolveDateRange(request.GetTuNgay(), request.GetDenNgay(), request.LoaiThoiGian);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.DotNhapKhos.AsNoTracking()
            .Where(x => x.NgayNhapKho >= fromDate && x.NgayNhapKho <= toDate);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            query = query.Where(x => x.TrangThai == request.TrangThai);
        }

        var loaiXeMap = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var listDotNhap = await query
            .OrderBy(x => x.NgayNhapKho)
            .ThenBy(x => x.MaDotNhap)
            .ToListAsync(cancellationToken);

        var items = new List<BangKeChiTietDotNhapItemDto>();
        int stt = 1;

        foreach (var d in listDotNhap)
        {
            string dauBienStr = FormatDauBienStr(d.KyHieuDaiDauBien);
            string loaiXeTen = (d.LoaiXeId != null && loaiXeMap.TryGetValue(d.LoaiXeId, out var lx)) ? lx : "Ô tô";
            string canBo = !string.IsNullOrWhiteSpace(d.CanBoTiepNhan) ? d.CanBoTiepNhan : (!string.IsNullOrWhiteSpace(d.NguoiNhapKho) ? d.NguoiNhapKho : "Trung Tá - Mai Cao");

            items.Add(new BangKeChiTietDotNhapItemDto
            {
                Stt = stt++,
                MaDotNhap = d.MaDotNhap,
                NgayNhap = d.NgayNhapKho,
                DauBien = dauBienStr,
                LoaiXeTen = loaiXeTen,
                SoLuongNhap = d.TongSoBien,
                SoDuocBam = d.SoBienDuocBam,
                CanBoTiepNhan = canBo
            });
        }

        // Lọc tìm kiếm số biển / keyword
        var searchKw = request.GetSearchKeyword();
        if (!string.IsNullOrWhiteSpace(searchKw))
        {
            var filter = searchKw.ToUpperInvariant();
            items = items.Where(x =>
                x.MaDotNhap.ToUpperInvariant().Contains(filter) ||
                x.DauBien.ToUpperInvariant().Contains(filter) ||
                x.CanBoTiepNhan.ToUpperInvariant().Contains(filter)
            ).ToList();
        }

        // Lọc theo đầu biển nếu có
        var dauBienFilter = request.GetDauBienFilter();
        if (!string.IsNullOrWhiteSpace(dauBienFilter))
        {
            var dbFilter = dauBienFilter.ToUpperInvariant();
            items = items.Where(x => x.DauBien.ToUpperInvariant().Contains(dbFilter)).ToList();
        }

        // Re-index STT
        stt = 1;
        foreach (var item in items) item.Stt = stt++;

        int tongBien = items.Sum(x => x.SoLuongNhap);

        return new BangKeChiTietDotNhapResponse
        {
            TieuDe = "BẢNG KÊ CHI TIẾT ĐỢT NHẬP BIẾN SỐ XE",
            KyBaoCaoHienThi = $"Từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}",
            TuNgay = fromDate,
            DenNgay = toDate,
            TongSoDong = items.Count,
            TongBienDaTiepNhan = tongBien,
            Items = items
        };
    }

    /// <summary>
    /// Báo cáo 5 (Tab 7): Bảng kê chi tiết biển số đã xuất kho.
    /// Danh sách các biển số đã cấp phát từ kho ngẫu nhiên trong kỳ.
    /// </summary>
    public async Task<BangKeChiTietXuatKhoResponse> LayBangKeChiTietXuatKhoAsync(
        FilterBangKeChiTietXuatKhoRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBangKeChiTietXuatKhoRequest();
        var (fromDate, toDate) = ResolveDateRange(request.GetTuNgay(), request.GetDenNgay(), request.LoaiThoiGian);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.XuatKhoBienSos.AsNoTracking()
            .Where(x => x.NgayThucHien >= fromDate && 
                        x.NgayThucHien <= toDate && 
                        x.DaXuatKho && 
                        x.TrangThai != "DaHuy" && 
                        !string.IsNullOrEmpty(x.BienKiemSoat));

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            query = query.Where(x => x.TrangThai == request.TrangThai);
        }

        var dauBienMap = await _dbContext.DauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.KyHieuSeries ?? x.Ten ?? "Đầu biển", cancellationToken);

        var listXuat = await query
            .OrderBy(x => x.NgayThucHien)
            .ThenBy(x => x.SoBienLai)
            .ToListAsync(cancellationToken);

        var items = new List<BangKeChiTietBienSoXuatKhoItemDto>();
        int stt = 1;

        foreach (var x in listXuat)
        {
            var dauBienTen = (x.DauBienSoId != null && dauBienMap.TryGetValue(x.DauBienSoId, out var db)) ? db : x.DauBienSoTen;
            var dauBien = ExtractDauBien(x.BienKiemSoat, dauBienTen);
            var soBienLai = $"{x.KyHieuQuyen}-{x.SoBienLai}";
            var canBoCap = "Trung Tá - Mai Cao";

            items.Add(new BangKeChiTietBienSoXuatKhoItemDto
            {
                Stt = stt++,
                NgayXuat = x.NgayThucHien,
                BienSoDaCap = x.BienKiemSoat,
                DauBien = dauBien,
                SoBienLai = soBienLai,
                NguoiNhanBien = x.HoTenNguoiNop,
                LePhiThu = x.SoTienThu,
                CanBoCap = canBoCap
            });
        }

        // Lọc tìm kiếm số biển / keyword
        var searchKw = request.GetSearchKeyword();
        if (!string.IsNullOrWhiteSpace(searchKw))
        {
            var filter = searchKw.ToUpperInvariant();
            items = items.Where(x =>
                x.BienSoDaCap.ToUpperInvariant().Contains(filter) ||
                x.DauBien.ToUpperInvariant().Contains(filter) ||
                x.SoBienLai.ToUpperInvariant().Contains(filter) ||
                x.NguoiNhanBien.ToUpperInvariant().Contains(filter) ||
                x.CanBoCap.ToUpperInvariant().Contains(filter)
            ).ToList();
        }

        // Lọc theo đầu biển nếu có
        var dauBienFilter = request.GetDauBienFilter();
        if (!string.IsNullOrWhiteSpace(dauBienFilter))
        {
            var dbFilter = dauBienFilter.ToUpperInvariant();
            items = items.Where(x => x.DauBien.ToUpperInvariant().Contains(dbFilter)).ToList();
        }

        stt = 1;
        foreach (var item in items) item.Stt = stt++;

        return new BangKeChiTietXuatKhoResponse
        {
            TieuDe = "BẢNG KÊ CHI TIẾT BIẾN SỐ ĐÃ XUẤT KHO",
            MoTa = "Danh sách biển đã cấp phát từ kho ngẫu nhiên",
            KyBaoCaoHienThi = $"Từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}",
            TuNgay = fromDate,
            DenNgay = toDate,
            TongSoDong = items.Count,
            TongBienDaXuatKho = items.Count,
            Items = items
        };
    }

    #region Helper Methods

    private static (DateTime FromDate, DateTime ToDate) ResolveDateRange(DateTime? tuNgay, DateTime? denNgay, string? loaiThoiGian = null)
    {
        var now = DateTime.UtcNow;
        var vnNow = now.AddHours(7);
        var todayVn = vnNow.Date;

        if (!string.IsNullOrWhiteSpace(loaiThoiGian))
        {
            var ltg = loaiThoiGian.Trim().ToLowerInvariant();
            if (ltg == "homnay" || ltg == "hôm nay")
            {
                var start = DateTime.SpecifyKind(todayVn.AddHours(-7), DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(todayVn.AddDays(1).AddTicks(-1).AddHours(-7), DateTimeKind.Utc);
                return (start, end);
            }
            if (ltg == "tuannay" || ltg == "tuần này")
            {
                int diff = (7 + (int)todayVn.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                var monday = todayVn.AddDays(-diff);
                var sunday = monday.AddDays(6);
                var start = DateTime.SpecifyKind(monday.AddHours(-7), DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(sunday.AddDays(1).AddTicks(-1).AddHours(-7), DateTimeKind.Utc);
                return (start, end);
            }
            if (ltg == "thangnay" || ltg == "tháng này")
            {
                var firstDay = new DateTime(todayVn.Year, todayVn.Month, 1);
                var lastDay = firstDay.AddMonths(1).AddTicks(-1);
                var start = DateTime.SpecifyKind(firstDay.AddHours(-7), DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(lastDay.AddHours(-7), DateTimeKind.Utc);
                return (start, end);
            }
            if (ltg == "tatca" || ltg == "tất cả")
            {
                var start = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = new DateTime(2099, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                return (start, end);
            }
        }

        DateTime fromDate;
        DateTime toDate;

        if (tuNgay.HasValue && denNgay.HasValue)
        {
            var d1 = tuNgay.Value;
            var d2 = denNgay.Value;
            if (d1 > d2) (d1, d2) = (d2, d1);

            fromDate = NormalizeStartDate(d1);
            toDate = NormalizeEndDate(d2);
        }
        else if (tuNgay.HasValue)
        {
            fromDate = NormalizeStartDate(tuNgay.Value);
            toDate = DateTime.SpecifyKind(fromDate.AddMonths(3).AddTicks(-1), DateTimeKind.Utc);
        }
        else if (denNgay.HasValue)
        {
            toDate = NormalizeEndDate(denNgay.Value);
            fromDate = DateTime.SpecifyKind(toDate.AddMonths(-3).Date, DateTimeKind.Utc);
        }
        else
        {
            // Mặc định kỳ báo cáo 3 tháng: 01/08/2026 đến 31/10/2026 như giao diện mẫu
            fromDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            toDate = new DateTime(2026, 10, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        }

        return (fromDate, toDate);
    }

    private static DateTime NormalizeStartDate(DateTime dt)
    {
        // Nếu client gửi 17:00:00 UTC (tương đương 00:00:00 ngày hôm sau theo giờ VN GMT+7)
        if (dt.Kind == DateTimeKind.Utc && dt.TimeOfDay.Hours >= 16 && dt.TimeOfDay.Hours <= 18)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
    }

    private static DateTime NormalizeEndDate(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc && dt.TimeOfDay.Hours >= 16 && dt.TimeOfDay.Hours <= 18)
        {
            return DateTime.SpecifyKind(dt.AddTicks(-1), DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }

    private static string FormatDauBienStr(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "—";
        raw = raw.Trim();
        if (raw.StartsWith("[") && raw.EndsWith("]"))
        {
            try
            {
                var arr = JsonSerializer.Deserialize<List<string>>(raw);
                if (arr != null && arr.Any())
                {
                    return string.Join(", ", arr);
                }
            }
            catch
            {
                // Fallback nếu không parse được json
            }
        }
        return raw;
    }

    /// <summary>
    /// Helper: Tách nhóm dải đầu biển từ biển kiểm soát.
    /// Ví dụ: "29A-036.12" => "29A-036", "29AA-000.01" => "29AA-000", "29AC-963.52" => "29AC-963".
    /// </summary>
    private static string ExtractDauBien(string bienKiemSoat, string? dauBienMacDinh)
    {
        if (string.IsNullOrWhiteSpace(bienKiemSoat))
        {
            return !string.IsNullOrWhiteSpace(dauBienMacDinh) ? dauBienMacDinh : "Khác";
        }

        bienKiemSoat = bienKiemSoat.Trim().ToUpperInvariant();

        // Nếu có dấu '.', phần trước dấu '.' chính là dải đầu biển (chuẩn biển số 5 số Việt Nam)
        int dotIndex = bienKiemSoat.IndexOf('.');
        if (dotIndex > 0)
        {
            return bienKiemSoat.Substring(0, dotIndex);
        }

        // Nếu có dấu '-' và sau đó có ít nhất 3 chữ số
        int dashIndex = bienKiemSoat.IndexOf('-');
        if (dashIndex > 0 && dashIndex + 4 <= bienKiemSoat.Length)
        {
            return bienKiemSoat.Substring(0, dashIndex + 4);
        }

        return !string.IsNullOrWhiteSpace(dauBienMacDinh) ? dauBienMacDinh : bienKiemSoat;
    }

    #endregion
}

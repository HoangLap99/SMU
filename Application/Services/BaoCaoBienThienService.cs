using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai các API Báo cáo Phân tích Biến thiên Nhập - Xuất - Chi phí lệ phí:
/// Dữ liệu tổng hợp từ Bảng KhoBienSo, DotNhapKho, XuatKhoBienSo và CapLaiDangKyXe.
/// </summary>
public class BaoCaoBienThienService : IBaoCaoBienThienService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BaoCaoBienThienService> _logger;

    public BaoCaoBienThienService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<BaoCaoBienThienService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    #region API 1: Thẻ tóm tắt KPI / Thống kê tổng quan (Khoanh đỏ số 2)

    public async Task<BaoCaoKpiTongQuanResponse> LayKpiTongQuanAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoBienThienRequest();
        var (fromDate, toDate, _) = ResolveDateRange(request);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Tổng số biển nhập kho & Số đợt tiếp nhận dải số
        var nhapKhoQuery = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => x.NgayNhapKho >= fromDate && x.NgayNhapKho <= toDate);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            nhapKhoQuery = nhapKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            nhapKhoQuery = nhapKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        var tongSoBienNhapKho = await nhapKhoQuery.CountAsync(cancellationToken);
        var soDotTiepNhan = await nhapKhoQuery
            .Select(x => x.DotNhapKhoId)
            .Distinct()
            .CountAsync(cancellationToken);

        // 2. Tổng số biển xuất kho (Đã bấm ngẫu nhiên & cấp phát)
        var xuatKhoQuery = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => x.DaXuatKho && x.NgayXuatKho != null && x.NgayXuatKho >= fromDate && x.NgayXuatKho <= toDate);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        var tongSoBienXuatKho = await xuatKhoQuery.CountAsync(cancellationToken);

        // 3. Tiền thu lệ phí xuất biển & thủ tục (XuatKhoBienSo + CapLaiDangKyXe)
        var tienXuatKhoQuery = _dbContext.XuatKhoBienSos.AsNoTracking()
            .Where(x => x.TrangThai != "DaHuy" && x.NgayThucHien >= fromDate && x.NgayThucHien <= toDate);

        var tienCapLaiQuery = _dbContext.CapLaiDangKyXes.AsNoTracking()
            .Where(x => x.TrangThai != "DaHuy" && x.NgayBienLai >= fromDate && x.NgayBienLai <= toDate);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            tienXuatKhoQuery = tienXuatKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            tienCapLaiQuery = tienCapLaiQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            tienXuatKhoQuery = tienXuatKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
            tienCapLaiQuery = tienCapLaiQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        var tongTienXuatKho = await tienXuatKhoQuery.SumAsync(x => (decimal?)x.SoTienThu, cancellationToken) ?? 0m;
        var tongTienCapLai = await tienCapLaiQuery.SumAsync(x => (decimal?)x.SoTienThu, cancellationToken) ?? 0m;
        var tienThuLePhi = tongTienXuatKho + tongTienCapLai;

        // 4. Tỷ lệ xuất / nhập kho (%)
        double tyLeXuatNhap = tongSoBienNhapKho > 0
            ? Math.Round((double)tongSoBienXuatKho / tongSoBienNhapKho * 100, 1)
            : 0;

        return new BaoCaoKpiTongQuanResponse
        {
            TongSoBienNhapKho = tongSoBienNhapKho,
            ChuThichNhapKho = $"Qua {soDotTiepNhan} đợt tiếp nhận dải số",
            TongSoBienXuatKho = tongSoBienXuatKho,
            ChuThichXuatKho = "Đã bấm ngẫu nhiên & cấp phát",
            TienThuLePhiXuatBien = tienThuLePhi,
            ChuThichTienThu = "Thu theo biên lai điện tử",
            TyLeXuatNhapKho = tyLeXuatNhap,
            ChuThichTyLe = "Tốc độ luân chuyển biển số"
        };
    }

    #endregion

    #region API 2: Biểu đồ 1 - Biến thiên Số Lượng Biển Nhập và Biển Xuất theo thời gian (Khoanh đỏ số 3)

    public async Task<BieuDoBienThienNhapXuatResponse> LayBieuDo1NhapXuatAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoBienThienRequest();
        var (fromDate, toDate, gomTheo) = ResolveDateRange(request);
        var buckets = GenerateTimeBuckets(fromDate, toDate, gomTheo);

        var dataRows = await LayDuLieuTongHopTheoMocAsync(request, fromDate, toDate, gomTheo, buckets, cancellationToken);

        var items = dataRows.Select(x => new BieuDoNhapXuatItemDto
        {
            ThoiGian = x.ThoiGian,
            MocThoiGian = x.MocThoiGian,
            SoLuongNhap = x.BienNhapKho,
            SoLuongXuat = x.BienXuatKho
        }).ToList();

        return new BieuDoBienThienNhapXuatResponse
        {
            TieuDe = "Biểu đồ 1: Biến thiên Số Lượng Biển Nhập và Biển Xuất theo thời gian",
            MoTa = "So sánh khối lượng biển tiếp nhận vào kho (Nhập) và khối lượng biển cấp phát ngẫu nhiên (Xuất)",
            DonViTinh = "Biển kiểm soát",
            GomTheo = gomTheo,
            TongNhap = items.Sum(x => x.SoLuongNhap),
            TongXuat = items.Sum(x => x.SoLuongXuat),
            Items = items
        };
    }

    #endregion

    #region API 3: Biểu đồ 2 - Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất theo thời gian (Khoanh đỏ số 4)

    public async Task<BieuDoBienThienLePhiResponse> LayBieuDo2LePhiAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoBienThienRequest();
        var (fromDate, toDate, gomTheo) = ResolveDateRange(request);
        var buckets = GenerateTimeBuckets(fromDate, toDate, gomTheo);

        var dataRows = await LayDuLieuTongHopTheoMocAsync(request, fromDate, toDate, gomTheo, buckets, cancellationToken);

        var items = dataRows.Select(x => new BieuDoLePhiItemDto
        {
            ThoiGian = x.ThoiGian,
            MocThoiGian = x.MocThoiGian,
            TienThu = x.TienThuLePhi,
            TienThuHienThi = FormatCurrencyShort(x.TienThuLePhi),
            SoHoSo = x.SoHoSo
        }).ToList();

        var tongKy = items.Sum(x => x.TienThu);

        return new BieuDoBienThienLePhiResponse
        {
            TieuDe = "Biểu đồ 2: Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất theo thời gian",
            MoTa = "Tổng số tiền lệ phí thu ngân sách nhà nước từ các hồ sơ xuất biển và đăng ký xe (VNĐ)",
            DonViTinh = "Việt Nam đồng (VNĐ)",
            GomTheo = gomTheo,
            TongKy = tongKy,
            Items = items
        };
    }

    #endregion

    #region API 4: Cơ cấu biển xuất theo thủ tục & Loại phương tiện (Khoanh đỏ số 5)

    public async Task<CoCauBienXuatResponse> LayCoCauBienXuatAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoBienThienRequest();
        var (fromDate, toDate, _) = ResolveDateRange(request);
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Lấy danh sách phiếu XuatKhoBienSo trong kỳ
        var xuatKhoQuery = _dbContext.XuatKhoBienSos.AsNoTracking()
            .Where(x => x.TrangThai != "DaHuy" && x.NgayThucHien >= fromDate && x.NgayThucHien <= toDate);

        var capLaiQuery = _dbContext.CapLaiDangKyXes.AsNoTracking()
            .Where(x => x.TrangThai != "DaHuy" && x.NgayBienLai >= fromDate && x.NgayBienLai <= toDate);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            capLaiQuery = capLaiQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            xuatKhoQuery = xuatKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
            capLaiQuery = capLaiQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        var xuatKhos = await xuatKhoQuery.ToListAsync(cancellationToken);
        var capLais = await capLaiQuery.ToListAsync(cancellationToken);

        int tongLuot = xuatKhos.Count + capLais.Count;
        decimal tongTien = xuatKhos.Sum(x => x.SoTienThu) + capLais.Sum(x => x.SoTienThu);

        // 2. Nhóm theo Thủ tục
        var theoThuTucList = new List<CoCauThuTucItemDto>();

        // 2.1. Cấp biển (Có xuất kho biển số)
        var listCapBien = xuatKhos.Where(x => string.IsNullOrWhiteSpace(x.LoaiThuTucTen) || x.LoaiThuTucTen.Contains("Cấp biển", StringComparison.OrdinalIgnoreCase)).ToList();
        var listSangTen = xuatKhos.Where(x => x.LoaiThuTucTen != null && (x.LoaiThuTucTen.Contains("Sang tên", StringComparison.OrdinalIgnoreCase) || x.LoaiThuTucTen.Contains("Cấp đổi", StringComparison.OrdinalIgnoreCase))).ToList();
        var listKhacXuatKho = xuatKhos.Except(listCapBien).Except(listSangTen).ToList();

        // Thêm nhóm "Cấp biển"
        if (listCapBien.Any() || tongLuot == 0)
        {
            var luot = listCapBien.Count;
            var tien = listCapBien.Sum(x => x.SoTienThu);
            theoThuTucList.Add(new CoCauThuTucItemDto
            {
                TenThuTuc = "Cấp biển",
                SoLuot = luot,
                TyLeLuot = tongLuot > 0 ? Math.Round((double)luot / tongLuot * 100, 0) : 0,
                SoBienXuatKho = luot, // Có xuất kho biển
                TienThu = tien
            });
        }

        // Thêm nhóm "Sang tên"
        if (listSangTen.Any())
        {
            var luot = listSangTen.Count;
            var tien = listSangTen.Sum(x => x.SoTienThu);
            theoThuTucList.Add(new CoCauThuTucItemDto
            {
                TenThuTuc = "Sang tên",
                SoLuot = luot,
                TyLeLuot = tongLuot > 0 ? Math.Round((double)luot / tongLuot * 100, 0) : 0,
                SoBienXuatKho = 0, // Sang tên không xuất biển mới
                TienThu = tien
            });
        }

        // Thêm nhóm "Cấp lại" (từ CapLaiDangKyXe)
        if (capLais.Any())
        {
            var luot = capLais.Count;
            var tien = capLais.Sum(x => x.SoTienThu);
            theoThuTucList.Add(new CoCauThuTucItemDto
            {
                TenThuTuc = "Cấp lại",
                SoLuot = luot,
                TyLeLuot = tongLuot > 0 ? Math.Round((double)luot / tongLuot * 100, 0) : 0,
                SoBienXuatKho = 0, // Cấp lại không xuất kho biển
                TienThu = tien
            });
        }

        // Các thủ tục khác nếu có
        if (listKhacXuatKho.Any())
        {
            var luot = listKhacXuatKho.Count;
            var tien = listKhacXuatKho.Sum(x => x.SoTienThu);
            theoThuTucList.Add(new CoCauThuTucItemDto
            {
                TenThuTuc = "Thủ tục khác",
                SoLuot = luot,
                TyLeLuot = tongLuot > 0 ? Math.Round((double)luot / tongLuot * 100, 0) : 0,
                SoBienXuatKho = luot,
                TienThu = tien
            });
        }

        // 3. Nhóm theo Loại phương tiện (Ô tô, Xe máy, Rơ moóc, Khác...)
        var phuongTienDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void AddPhuongTien(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) ten = "Khác";
            else if (ten.Contains("Ô tô", StringComparison.OrdinalIgnoreCase)) ten = "Ô tô";
            else if (ten.Contains("Mô tô", StringComparison.OrdinalIgnoreCase) || ten.Contains("Xe máy", StringComparison.OrdinalIgnoreCase)) ten = "Xe máy";
            else if (ten.Contains("Rơ moóc", StringComparison.OrdinalIgnoreCase) || ten.Contains("Rơ móc", StringComparison.OrdinalIgnoreCase)) ten = "Rơ moóc";
            else ten = "Khác";

            phuongTienDict[ten] = phuongTienDict.GetValueOrDefault(ten, 0) + 1;
        }

        foreach (var x in xuatKhos) AddPhuongTien(x.LoaiXeTen);
        foreach (var c in capLais) AddPhuongTien(c.LoaiPhuongTien);

        var theoLoaiPhuongTienList = new List<CoCauLoaiPhuongTienItemDto>();
        // Ưu tiên thứ tự: Ô tô, Xe máy, Rơ moóc, Khác
        var standardTypes = new[] { "Ô tô", "Xe máy", "Rơ moóc", "Khác" };
        foreach (var t in standardTypes)
        {
            var sl = phuongTienDict.GetValueOrDefault(t, 0);
            if (sl > 0 || tongLuot == 0)
            {
                theoLoaiPhuongTienList.Add(new CoCauLoaiPhuongTienItemDto
                {
                    TenLoaiXe = t,
                    SoLuong = sl,
                    TyLePhanTram = tongLuot > 0 ? Math.Round((double)sl / tongLuot * 100, 0) : 0
                });
            }
        }

        return new CoCauBienXuatResponse
        {
            TieuDe = "Cơ cấu biển xuất theo thủ tục",
            MoTa = "Phân bổ lượt giải quyết hồ sơ",
            TongLuotGiaiQuyet = tongLuot,
            TongTienThu = tongTien,
            TheoThuTuc = theoThuTucList,
            TheoLoaiPhuongTien = theoLoaiPhuongTienList
        };
    }

    #endregion

    #region API 5: Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết (Khoanh đỏ số 6)

    public async Task<BangSoLieuBienThienResponse> LayBangSoLieuChiTietAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBaoCaoBienThienRequest();
        var (fromDate, toDate, gomTheo) = ResolveDateRange(request);
        var buckets = GenerateTimeBuckets(fromDate, toDate, gomTheo);

        var dataRows = await LayDuLieuTongHopTheoMocAsync(request, fromDate, toDate, gomTheo, buckets, cancellationToken);

        int stt = 1;
        var items = dataRows.Select(x =>
        {
            x.Stt = stt++;
            return x;
        }).ToList();

        var tongCong = new BangSoLieuTongCongDto
        {
            TongBienNhapKho = items.Sum(x => x.BienNhapKho),
            TongBienXuatKho = items.Sum(x => x.BienXuatKho),
            TongSoHoSo = items.Sum(x => x.SoHoSo),
            TongTienThuLePhi = items.Sum(x => x.TienThuLePhi)
        };

        return new BangSoLieuBienThienResponse
        {
            TieuDe = "Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết",
            TongMocThoiGianHienThi = $"{items.Count} mốc thời gian",
            GomTheo = gomTheo,
            Items = items,
            TongCong = tongCong
        };
    }

    #endregion

    #region API 6: Toàn bộ Dashboard All-in-One

    public async Task<BaoCaoBienThienDashboardResponse> LayToanBoBaoCaoAsync(
        FilterBaoCaoBienThienRequest? request,
        CancellationToken cancellationToken = default)
    {
        var kpi = await LayKpiTongQuanAsync(request, cancellationToken);
        var bieuDo1 = await LayBieuDo1NhapXuatAsync(request, cancellationToken);
        var bieuDo2 = await LayBieuDo2LePhiAsync(request, cancellationToken);
        var coCau = await LayCoCauBienXuatAsync(request, cancellationToken);
        var bangChiTiet = await LayBangSoLieuChiTietAsync(request, cancellationToken);

        return new BaoCaoBienThienDashboardResponse
        {
            KpiTongQuan = kpi,
            BieuDoNhapXuat = bieuDo1,
            BieuDoLePhi = bieuDo2,
            CoCauBienXuat = coCau,
            BangChiTiet = bangChiTiet
        };
    }

    #endregion

    #region API Bộ lọc: Gom theo & Mốc thời gian

    /// <summary>
    /// Lấy danh sách các tùy chọn cho bộ lọc 'Gom theo' (Theo tháng, Theo ngày).
    /// </summary>
    public List<BoLocGomTheoItemDto> LayBoLocGomTheo()
    {
        return new List<BoLocGomTheoItemDto>
        {
            new()
            {
                Ma = "Thang",
                Ten = "Theo tháng",
                IsDefault = true,
                MoTa = "Gom nhóm dữ liệu báo cáo theo từng tháng (Tháng 8/2026, Tháng 9/2026...)"
            },
            new()
            {
                Ma = "Ngay",
                Ten = "Theo ngày",
                IsDefault = false,
                MoTa = "Gom nhóm dữ liệu báo cáo theo từng ngày chi tiết (01/08/2026, 02/08/2026...)"
            }
        };
    }

    /// <summary>
    /// Lấy danh sách các tùy chọn cho bộ lọc 'Mốc thời gian' chọn nhanh ([Toàn bộ], [Tháng 8/2026], [Tháng 9/2026], [Tháng 10/2026], [Quý 3/2026]).
    /// </summary>
    public async Task<List<BoLocMocThoiGianItemDto>> LayBoLocMocThoiGianAsync(
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        customerId ??= _currentUserService.CustomerId;

        // Truy vấn dữ liệu từ bảng KhoBienSo để xác định khoảng thời gian có dữ liệu
        var query = _dbContext.KhoBienSos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        var minDate = await query.Select(x => (DateTime?)x.NgayNhapKho).MinAsync(cancellationToken)
                      ?? new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = await query.Select(x => (DateTime?)x.NgayNhapKho).MaxAsync(cancellationToken)
                      ?? new DateTime(2026, 10, 31, 23, 59, 59, 999, DateTimeKind.Utc);

        // Mặc định khoảng toàn bộ bao phủ từ đầu tháng nhỏ nhất đến cuối tháng lớn nhất
        var startAll = new DateTime(minDate.Year, minDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endAllMonth = new DateTime(maxDate.Year, maxDate.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var endAll = endAllMonth.AddTicks(-1);

        var result = new List<BoLocMocThoiGianItemDto>
        {
            new()
            {
                Ma = "ToanBo",
                Ten = "Toàn bộ",
                IsDefault = true,
                TuNgay = startAll,
                DenNgay = endAll,
                LoaiMoc = "All"
            }
        };

        // Danh sách các tháng có dữ liệu
        var cur = startAll;
        while (cur < endAllMonth)
        {
            var next = cur.AddMonths(1);
            result.Add(new BoLocMocThoiGianItemDto
            {
                Ma = $"Thang{cur.Month}",
                Ten = $"Tháng {cur.Month}/{cur.Year}",
                IsDefault = false,
                TuNgay = cur,
                DenNgay = next.AddTicks(-1),
                LoaiMoc = "Month"
            });
            cur = next;
        }

        // Bổ sung nút chọn Quý 3/2026 như giao diện thiết kế mẫu
        var q3Start = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var q3End = new DateTime(2026, 9, 30, 23, 59, 59, 999, DateTimeKind.Utc);
        result.Add(new BoLocMocThoiGianItemDto
        {
            Ma = "Quy3",
            Ten = "Quý 3/2026",
            IsDefault = false,
            TuNgay = q3Start,
            DenNgay = q3End,
            LoaiMoc = "Quarter"
        });

        return result;
    }

    #endregion

    #region Phương thức tính toán cốt lõi & Helper

    /// <summary>
    /// Hàm gom và tổng hợp số liệu theo từng bucket thời gian (Tháng hoặc Ngày) từ các bảng:
    /// - KhoBienSo (Biển nhập &amp; Biển xuất)
    /// - XuatKhoBienSo (Số hồ sơ xuất &amp; Tiền thu)
    /// - CapLaiDangKyXe (Số hồ sơ cấp lại &amp; Tiền thu)
    /// </summary>
    private async Task<List<BangSoLieuItemDto>> LayDuLieuTongHopTheoMocAsync(
        FilterBaoCaoBienThienRequest request,
        DateTime fromDate,
        DateTime toDate,
        string gomTheo,
        List<(string Label, DateTime Start, DateTime End, DateTime KeyDate)> buckets,
        CancellationToken cancellationToken)
    {
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Lấy dữ liệu Biển nhập kho (KhoBienSo)
        var nhapKhoQuery = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => x.NgayNhapKho >= fromDate && x.NgayNhapKho <= toDate);

        // 2. Lấy dữ liệu Biển xuất kho (KhoBienSo có DaXuatKho == true)
        var xuatKhoQuery = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => x.DaXuatKho && x.NgayXuatKho != null && x.NgayXuatKho >= fromDate && x.NgayXuatKho <= toDate);

        // 3. Lấy dữ liệu hồ sơ xuất kho & biên lai
        var phieuXuatKhoQuery = _dbContext.XuatKhoBienSos.AsNoTracking()
            .Where(x => x.TrangThai != "DaHuy" && x.NgayThucHien >= fromDate && x.NgayThucHien <= toDate);

        // 4. Lấy dữ liệu hồ sơ cấp lại đăng ký xe & biên lai
        var phieuCapLaiQuery = _dbContext.CapLaiDangKyXes.AsNoTracking()
            .Where(x => x.TrangThai != "DaHuy" && x.NgayBienLai >= fromDate && x.NgayBienLai <= toDate);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            nhapKhoQuery = nhapKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            xuatKhoQuery = xuatKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            phieuXuatKhoQuery = phieuXuatKhoQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            phieuCapLaiQuery = phieuCapLaiQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            nhapKhoQuery = nhapKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
            xuatKhoQuery = xuatKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
            phieuXuatKhoQuery = phieuXuatKhoQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
            phieuCapLaiQuery = phieuCapLaiQuery.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        // Tải danh sách mốc thời gian và số liệu về bộ nhớ
        var dsNhap = await nhapKhoQuery
            .Select(x => x.NgayNhapKho)
            .ToListAsync(cancellationToken);

        var dsXuat = await xuatKhoQuery
            .Select(x => x.NgayXuatKho!.Value)
            .ToListAsync(cancellationToken);

        var dsPhieuXuat = await phieuXuatKhoQuery
            .Select(x => new { x.NgayThucHien, x.SoTienThu })
            .ToListAsync(cancellationToken);

        var dsPhieuCapLai = await phieuCapLaiQuery
            .Select(x => new { x.NgayBienLai, x.SoTienThu })
            .ToListAsync(cancellationToken);

        var result = new List<BangSoLieuItemDto>();

        foreach (var b in buckets)
        {
            int soNhap = dsNhap.Count(d => d >= b.Start && d <= b.End);
            int soXuat = dsXuat.Count(d => d >= b.Start && d <= b.End);

            var hsXuat = dsPhieuXuat.Where(d => d.NgayThucHien >= b.Start && d.NgayThucHien <= b.End).ToList();
            var hsCapLai = dsPhieuCapLai.Where(d => d.NgayBienLai >= b.Start && d.NgayBienLai <= b.End).ToList();

            // Nếu KhoBienSo có biển xuất nhưng chưa gắn XuatKhoBienSo, số hồ sơ ít nhất bằng số biển xuất
            int soHoSo = hsXuat.Count + hsCapLai.Count;
            if (soHoSo < soXuat) soHoSo = soXuat;

            decimal tienThu = hsXuat.Sum(x => x.SoTienThu) + hsCapLai.Sum(x => x.SoTienThu);

            int chenhLech = soNhap - soXuat;

            result.Add(new BangSoLieuItemDto
            {
                ThoiGian = b.Label,
                MocThoiGian = b.KeyDate,
                BienNhapKho = soNhap,
                BienXuatKho = soXuat,
                SoHoSo = soHoSo,
                TienThuLePhi = tienThu,
                ChenhLech = chenhLech
            });
        }

        return result;
    }

    /// <summary>
    /// Xác định khoảng ngày và chế độ gom nhóm theo request:
    /// </summary>
    private static (DateTime FromDate, DateTime ToDate, string GomTheo) ResolveDateRange(FilterBaoCaoBienThienRequest request)
    {
        string gomTheo = (string.Equals(request.GomTheo?.Trim(), "Ngay", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(request.GomTheo?.Trim(), "TheoNgay", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(request.GomTheo?.Trim(), "Day", StringComparison.OrdinalIgnoreCase))
            ? "Ngay"
            : "Thang";

        DateTime fromDate;
        DateTime toDate;

        // Xử lý mốc thời gian nhanh
        if (!string.IsNullOrWhiteSpace(request.MocThoiGianNhanh))
        {
            var moc = request.MocThoiGianNhanh.Trim().ToLowerInvariant();

            if (moc == "toanbo" || moc == "all" || moc == "toàn bộ")
            {
                fromDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 10, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }

            if (moc.Contains("thang8") || moc.Contains("thang 8") || moc.Contains("08/2026") || moc.Contains("8/2026"))
            {
                fromDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 8, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
            if (moc.Contains("thang9") || moc.Contains("thang 9") || moc.Contains("09/2026") || moc.Contains("9/2026"))
            {
                fromDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 9, 30, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
            if (moc.Contains("thang10") || moc.Contains("thang 10") || moc.Contains("10/2026"))
            {
                fromDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 10, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
            if (moc.Contains("quy3") || moc.Contains("quy 3") || moc.Contains("q3"))
            {
                fromDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 9, 30, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
            if (moc.Contains("quy1") || moc.Contains("quy 1") || moc.Contains("q1"))
            {
                fromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 3, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
            if (moc.Contains("quy2") || moc.Contains("quy 2") || moc.Contains("q2"))
            {
                fromDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 6, 30, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
            if (moc.Contains("quy4") || moc.Contains("quy 4") || moc.Contains("q4"))
            {
                fromDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
                toDate = new DateTime(2026, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                return (fromDate, toDate, gomTheo);
            }
        }

        if (request.TuNgay.HasValue && request.DenNgay.HasValue)
        {
            fromDate = DateTime.SpecifyKind(request.TuNgay.Value.Date, DateTimeKind.Utc);
            toDate = DateTime.SpecifyKind(request.DenNgay.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        }
        else if (request.TuNgay.HasValue)
        {
            fromDate = DateTime.SpecifyKind(request.TuNgay.Value.Date, DateTimeKind.Utc);
            toDate = DateTime.SpecifyKind(fromDate.AddMonths(3).AddTicks(-1), DateTimeKind.Utc);
        }
        else if (request.DenNgay.HasValue)
        {
            toDate = DateTime.SpecifyKind(request.DenNgay.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            fromDate = DateTime.SpecifyKind(toDate.AddMonths(-3).Date, DateTimeKind.Utc);
        }
        else
        {
            // Mặc định kỳ báo cáo 3 tháng trọng điểm: Từ 01/08/2026 đến 31/10/2026 như giao diện mẫu
            fromDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            toDate = new DateTime(2026, 10, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        }

        return (fromDate, toDate, gomTheo);
    }

    /// <summary>
    /// Sinh các bucket khoảng thời gian (Tháng hoặc Ngày) liên tục từ fromDate đến toDate
    /// </summary>
    private static List<(string Label, DateTime Start, DateTime End, DateTime KeyDate)> GenerateTimeBuckets(
        DateTime fromDate,
        DateTime toDate,
        string gomTheo)
    {
        var buckets = new List<(string Label, DateTime Start, DateTime End, DateTime KeyDate)>();

        if (gomTheo.Equals("Ngay", StringComparison.OrdinalIgnoreCase))
        {
            var cur = fromDate.Date;
            while (cur <= toDate.Date)
            {
                var start = DateTime.SpecifyKind(cur, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(cur.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                var label = cur.ToString("dd/MM/yyyy");
                buckets.Add((label, start, end, start));
                cur = cur.AddDays(1);
            }
        }
        else
        {
            // Mặc định theo Tháng
            var curMonth = new DateTime(fromDate.Year, fromDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endMonth = new DateTime(toDate.Year, toDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (curMonth <= endMonth)
            {
                var nextMonth = curMonth.AddMonths(1);
                var start = curMonth;
                var end = nextMonth.AddTicks(-1);
                var label = $"Tháng {curMonth.Month}/{curMonth.Year}";
                buckets.Add((label, start, end, start));
                curMonth = nextMonth;
            }
        }

        return buckets;
    }

    /// <summary>
    /// Định dạng số tiền rút gọn trên biểu đồ cột: Ví dụ "2.3 tr", "40.9 tr", "550 k"
    /// </summary>
    private static string FormatCurrencyShort(decimal amount)
    {
        if (amount >= 1_000_000m)
        {
            return $"{(amount / 1_000_000m):0.#} tr";
        }
        if (amount >= 1_000m)
        {
            return $"{(amount / 1_000m):0.#} k";
        }
        if (amount > 0)
        {
            return $"{amount:N0} đ";
        }
        return "0 đ";
    }

    #endregion
}

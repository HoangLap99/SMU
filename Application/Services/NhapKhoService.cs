using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Dịch vụ quản lý nghiệp vụ Đợt nhập kho và Kho biển số xe.
/// Cung cấp 6 chức năng cốt lõi:
/// 1. (POST) Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
/// 2. (POST) Thêm mới và update đợt nhập kho. Nếu update và DaNhapKho == true thì sẽ lưu kho (bỏ qua biển số LaBienDauGia == true).
/// 3. (GET) Lấy thông tin 1 đợt nhập kho theo Id.
/// 4. (GET) Xóa đợt nhập kho theo Id.
/// 5. (GET) Xuất / Tải tệp tin Excel mẫu nhập kho biển số xe (MauBaoCao/NhapKhoBienSo.xlsx).
/// 6. (POST) Đọc dữ liệu danh sách biển số xe từ file Excel mẫu theo FileName trong StorageFolder.
/// </summary>
public class NhapKhoService : INhapKhoService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NhapKhoService> _logger;
    private readonly ILogHoatDongService _logHoatDongService;

    public NhapKhoService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<NhapKhoService> logger,
        ILogHoatDongService logHoatDongService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
        _logHoatDongService = logHoatDongService;
    }

    /// <summary>
    /// 1. (POST) Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
    /// </summary>
    public async Task<PagedResult<DotNhapKhoResponse>> LayDanhSachDotNhapAsync(PagedDotNhapKhoRequest request, CancellationToken cancellationToken = default)
    {
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;
        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            customerId = await _dbContext.Users
                .Where(u => u.Id == _currentUserService.UserId)
                .Select(u => u.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var query = _dbContext.DotNhapKhos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId);
        }

        var keyword = request.Keyword;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            var listId= _dbContext.ChiTietDotNhapBienSos.AsNoTracking().Where(a=>a.BienKiemSoat.ToLower().Contains(kw)).Select(a=>a.DotNhapKhoId).ToList();
            query = query.Where(x =>
                x.MaDotNhap.ToLower().Contains(kw) ||
                x.KyHieuDaiDauBien.ToLower().Contains(kw) ||
                listId.Contains(x.Id) ||
                (x.SoCongVan != null && x.SoCongVan.ToLower().Contains(kw)) ||
                (x.CanBoTiepNhan != null && x.CanBoTiepNhan.ToLower().Contains(kw)) ||
                (x.GhiChu != null && x.GhiChu.ToLower().Contains(kw)));

        }

        if (!string.IsNullOrEmpty(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrEmpty(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        if (!string.IsNullOrEmpty(request.TinhChatDotNhapId))
        {
            query = query.Where(x => x.TinhChatDotNhapId == request.TinhChatDotNhapId);
        }

        if (request.DaNhapKho.HasValue)
        {
            query = query.Where(x => x.DaNhapKho == request.DaNhapKho.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc)
                : request.FromDate.Value.ToUniversalTime();
            query = query.Where(x => x.NgayNhapKho >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc)
                : request.ToDate.Value.ToUniversalTime();
            query = query.Where(x => x.NgayNhapKho <= to);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        int pageIndex = request.CurrentPage > 0 ? request.CurrentPage : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var items = await query
            .OrderByDescending(x => x.NgayNhapKho)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var listResponses = new List<DotNhapKhoResponse>();
        foreach (var item in items)
        {
            var detail = await LayChiTietDotNhapAsync(item.Id!, cancellationToken);
            if (detail != null)
            {
                detail.DanhSachBienSo = new(); // Không tải toàn bộ biển số chi tiết trong danh sách tóm tắt
                listResponses.Add(detail);
            }
        }

        return new PagedResult<DotNhapKhoResponse>
        {
            Items = listResponses,
            TotalCount = totalItems,
            CurrentPage = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 2. (POST) Thêm mới và update đợt nhập kho.
    /// Nếu update và DaNhapKho == true thì sẽ lưu danh sách biển số vào bảng KhoBienSo (bỏ qua biển số LaBienDauGia == true).
    /// </summary>
    public async Task<DotNhapKhoResponse> SetDotNhapKhoAsync(SetDotNhapKhoRequest request, CancellationToken cancellationToken = default)
    {
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;
        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            customerId = await _dbContext.Users
                .Where(u => u.Id == _currentUserService.UserId)
                .Select(u => u.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Lấy danh mục trạng thái biển số mặc định
        var defaultStatus = await _dbContext.TrangThaiBienSos
            .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.ToLower().Contains("được bấm") || x.Ten.ToLower().Contains("bấm") || x.Ten.ToLower().Contains("tồn kho"), cancellationToken);
        var statusDauGia = await _dbContext.TrangThaiBienSos
            .FirstOrDefaultAsync(x => x.Ma == "CHO_DAU_GIA" || x.Ten.ToLower().Contains("đấu giá"), cancellationToken);

        // Kiểm tra xem là Thêm mới hay Update
        DotNhapKho? dotNhap = null;
        bool isUpdate = false;

        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            dotNhap = await _dbContext.DotNhapKhos.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            isUpdate = (dotNhap != null);
        }

        // Nếu là update của đợt nhập đã xác nhận/đã nhập kho trước đó, duy trì trạng thái đã nhập kho
        bool isDaNhapKho = request.DaNhapKho || (isUpdate && dotNhap != null && dotNhap.DaNhapKho);

        var maDotNhapChuanHoa = request.MaDotNhap.Trim().ToUpperInvariant();

        if (!isUpdate)
        {
            bool existsMa = await _dbContext.DotNhapKhos.AnyAsync(x => x.MaDotNhap.ToLower() == maDotNhapChuanHoa.ToLower(), cancellationToken);
            if (existsMa)
            {
                throw new ArgumentException($"Mã đợt nhập '{request.MaDotNhap}' đã tồn tại trong hệ thống.");
            }
        }
        else
        {
            bool duplicateMa = await _dbContext.DotNhapKhos.AnyAsync(x => x.Id != dotNhap!.Id && x.MaDotNhap.ToLower() == maDotNhapChuanHoa.ToLower(), cancellationToken);
            if (duplicateMa)
            {
                throw new ArgumentException($"Mã đợt nhập '{request.MaDotNhap}' đã tồn tại trên một phiếu khác.");
            }
        }

        var kyHieuDaiDauBienJson = request.KyHieuDaiDauBien != null && request.KyHieuDaiDauBien.Count > 0
            ? JsonSerializer.Serialize(request.KyHieuDaiDauBien)
            : null;

        // Chuẩn bị danh sách biển số chi tiết của đợt nhập
        var listBienSoItems = new List<ChiTietDotNhapBienSo>();
        int stt = 1;

        if (request.DanhSachBienSo != null && request.DanhSachBienSo.Count > 0)
        {
            foreach (var item in request.DanhSachBienSo.Where(x => !string.IsNullOrWhiteSpace(x.BienKiemSoat)))
            {
                var isDauGia = item.LaBienDauGia || (item.TrangThaiBienSoTen != null && item.TrangThaiBienSoTen.Contains("đấu giá", StringComparison.OrdinalIgnoreCase));
                
                string? ttId;
                string ttTen;
                string ttMau;

                if (isDauGia)
                {
                    ttId = statusDauGia?.Id ?? item.TrangThaiBienSoId;
                    ttTen = statusDauGia?.Ten ?? "Chờ đấu giá";
                    ttMau = statusDauGia?.MauSacHienThi ?? "warning";
                }
                else if (isDaNhapKho)
                {
                    // KHI XÁC NHẬN NHẬP KHO: Trạng thái biển luôn là "Được bấm"
                    ttId = defaultStatus?.Id ?? item.TrangThaiBienSoId;
                    ttTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)";
                    ttMau = defaultStatus?.MauSacHienThi ?? "success";
                }
                else
                {
                    // Lưu tạm / Bản nháp
                    ttId = item.TrangThaiBienSoId ?? defaultStatus?.Id;
                    ttTen = item.TrangThaiBienSoTen ?? "Lưu tạm";
                    ttMau = item.MauSacHienThi ?? "secondary";
                }

                listBienSoItems.Add(new ChiTietDotNhapBienSo
                {
                    CustomerId = customerId,
                    ThuTu = item.ThuTu > 0 ? item.ThuTu : stt++,
                    BienKiemSoat = item.BienKiemSoat.Trim().ToUpperInvariant(),
                    TrangThaiBienSoId = ttId,
                    TrangThaiBienSoTen = ttTen,
                    MauSacHienThi = ttMau,
                    LaBienDauGia = isDauGia,
                    LoaiXeId = request.LoaiXeId,
                    MauBienSoId = request.MauBienSoId,
                    GhiChu = item.GhiChu ?? request.GhiChu ?? (isDauGia ? "Biển số phục vụ đấu giá" : "Khai báo nhập kho")
                });
            }
        }
        else if (request.TuSo.HasValue && request.DenSo.HasValue && request.DenSo.Value >= request.TuSo.Value)
        {
            var prefix = string.IsNullOrWhiteSpace(request.TienToDaiSo) ? "30B" : request.TienToDaiSo.Trim().ToUpperInvariant();
            for (int i = request.TuSo.Value; i <= request.DenSo.Value; i++)
            {
                var numStr = i.ToString().PadLeft(5, '0');
                var formatted = $"{prefix}-{numStr.Substring(0, 3)}.{numStr.Substring(3)}";

                listBienSoItems.Add(new ChiTietDotNhapBienSo
                {
                    CustomerId = customerId,
                    ThuTu = stt++,
                    BienKiemSoat = formatted,
                    TrangThaiBienSoId = defaultStatus?.Id,
                    TrangThaiBienSoTen = isDaNhapKho ? (defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)") : "Lưu tạm",
                    MauSacHienThi = isDaNhapKho ? (defaultStatus?.MauSacHienThi ?? "success") : "secondary",
                    LaBienDauGia = false,
                    LoaiXeId = request.LoaiXeId,
                    MauBienSoId = request.MauBienSoId,
                    GhiChu = request.GhiChu ?? "Khai báo nhập kho tự động"
                });
            }
        }
        else if (isUpdate && dotNhap != null)
        {
            // Trường hợp cập nhật nhưng không gửi kèm DanhSachBienSo: Giữ nguyên danh sách biển hiện có của đợt nhập
            var existingChiTiets = await _dbContext.ChiTietDotNhapBienSos
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .OrderBy(x => x.ThuTu)
                .ToListAsync(cancellationToken);

            foreach (var ct in existingChiTiets)
            {
                if (!string.IsNullOrEmpty(request.LoaiXeId)) ct.LoaiXeId = request.LoaiXeId;
                if (!string.IsNullOrEmpty(request.MauBienSoId)) ct.MauBienSoId = request.MauBienSoId;
                listBienSoItems.Add(ct);
            }
        }

        // 1. Kiểm tra trùng lặp trong nội bộ danh sách gửi lên
        var duplicateInternal = listBienSoItems
            .GroupBy(x => x.BienKiemSoat, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateInternal.Count > 0)
        {
            var duplicatesStr = string.Join(", ", duplicateInternal.Take(5));
            if (duplicateInternal.Count > 5) duplicatesStr += $" và {duplicateInternal.Count - 5} biển khác";
            throw new ArgumentException($"Danh sách biển số nhập kho có các số bị trùng lặp: {duplicatesStr}. Vui lòng kiểm tra lại.");
        }

        // 2. Kiểm tra các biển kiểm soát đã tồn tại trong kho (KhoBienSo) hay chưa
        if (listBienSoItems.Count > 0)
        {
            var plateNumbers = listBienSoItems
                .Select(x => x.BienKiemSoat.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var queryKho = _dbContext.KhoBienSos.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(customerId))
            {
                queryKho = queryKho.Where(x => x.CustomerId == customerId || x.CustomerId == null);
            }

            // Nếu là cập nhật đợt nhập đang có, loại trừ các biển thuộc về chính đợt nhập này
            if (isUpdate && dotNhap != null && !string.IsNullOrEmpty(dotNhap.Id))
            {
                queryKho = queryKho.Where(x => x.DotNhapKhoId != dotNhap.Id);
            }

            var existingInKho = await queryKho
                .Where(x => plateNumbers.Contains(x.BienKiemSoat))
                .Select(x => new { x.BienKiemSoat, x.DotNhapKhoId })
                .ToListAsync(cancellationToken);

            if (existingInKho.Count > 0)
            {
                var existingDotNhapIds = existingInKho.Select(x => x.DotNhapKhoId).Distinct().ToList();
                var dotNhapMap = await _dbContext.DotNhapKhos.AsNoTracking()
                    .Where(d => existingDotNhapIds.Contains(d.Id!))
                    .ToDictionaryAsync(d => d.Id!, d => d.MaDotNhap, cancellationToken);

                if (existingInKho.Count == 1)
                {
                    var item = existingInKho[0];
                    var maDot = dotNhapMap.TryGetValue(item.DotNhapKhoId, out var md) ? $" (thuộc đợt nhập '{md}')" : "";
                    throw new ArgumentException($"Biển kiểm soát '{item.BienKiemSoat}' đã tồn tại trong kho{maDot}. Không thể nhập trùng!");
                }
                else
                {
                    var distinctPlates = existingInKho.Select(x => x.BienKiemSoat).Distinct().ToList();
                    var samplePlates = string.Join(", ", distinctPlates.Take(5));
                    var moreCount = distinctPlates.Count - 5;
                    var moreText = moreCount > 0 ? $" và {moreCount} biển khác" : "";
                    throw new ArgumentException($"Phát hiện {distinctPlates.Count} biển kiểm soát đã tồn tại trong kho ({samplePlates}{moreText}). Vui lòng kiểm tra lại!");
                }
            }
        }

        int tongSoBien = listBienSoItems.Count;
        int soBienDuocBam = listBienSoItems.Count(x => !x.LaBienDauGia);
        int soBienChoDauGia = listBienSoItems.Count(x => x.LaBienDauGia);

        var ngayNhapKhoUtc = request.NgayNhapKho != default
            ? (request.NgayNhapKho.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.NgayNhapKho, DateTimeKind.Utc)
                : request.NgayNhapKho.ToUniversalTime())
            : DateTime.UtcNow;

        // Lưu / Cập nhật bản ghi DotNhapKho
        if (!isUpdate)
        {
            dotNhap = new DotNhapKho
            {
                CustomerId = customerId,
                MaDotNhap = maDotNhapChuanHoa,
                NgayNhapKho = ngayNhapKhoUtc,
                LoaiXeId = request.LoaiXeId,
                MauBienSoId = request.MauBienSoId,
                KyHieuDaiDauBien = kyHieuDaiDauBienJson,
                CanBoTiepNhan = request.CanBoTiepNhan?.Trim(),
                TinhChatDotNhapId = request.TinhChatDotNhapId,
                SoCongVan = request.SoCongVan?.Trim(),
                GhiChu = request.GhiChu?.Trim(),
                TrangThai = isDaNhapKho ? "DaNhapKho" : "LuuTam",
                DaNhapKho = isDaNhapKho,
                NgayNhapKhoThucTe = isDaNhapKho ? DateTime.UtcNow : null,
                NguoiNhapKho = isDaNhapKho ? (_currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên") : null,
                TongSoBien = tongSoBien,
                SoBienDuocBam = soBienDuocBam,
                SoBienDaBam = 0,
                SoBienChoDauGia = soBienChoDauGia
            };
            _dbContext.DotNhapKhos.Add(dotNhap);
        }
        else
        {
            dotNhap!.MaDotNhap = maDotNhapChuanHoa;
            dotNhap.NgayNhapKho = request.NgayNhapKho != default ? ngayNhapKhoUtc : dotNhap.NgayNhapKho;
            dotNhap.LoaiXeId = request.LoaiXeId;
            dotNhap.MauBienSoId = request.MauBienSoId;
            dotNhap.KyHieuDaiDauBien = kyHieuDaiDauBienJson;
            dotNhap.CanBoTiepNhan = request.CanBoTiepNhan?.Trim();
            dotNhap.TinhChatDotNhapId = request.TinhChatDotNhapId;
            dotNhap.SoCongVan = request.SoCongVan?.Trim();
            dotNhap.GhiChu = request.GhiChu?.Trim();
            dotNhap.TongSoBien = tongSoBien;
            dotNhap.SoBienDuocBam = soBienDuocBam;
            dotNhap.SoBienChoDauGia = soBienChoDauGia;
            if (isDaNhapKho)
            {
                dotNhap.DaNhapKho = true;
                dotNhap.TrangThai = "DaNhapKho";
                dotNhap.NgayNhapKhoThucTe ??= DateTime.UtcNow;
                dotNhap.NguoiNhapKho ??= (_currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên");
            }
            dotNhap.Modified = DateTime.UtcNow;
        }

        // 1. Tự động trích xuất các đầu biển số từ danh sách biển số nhập kho và lưu vào danh mục DauBienSo
        var dauBienMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var distinctDauBiens = listBienSoItems
            .Select(x => TrichXuatDauBienSo(x.BienKiemSoat))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctDauBiens.Count > 0)
        {
            var existingDauBiens = await _dbContext.DauBienSos
                .Where(x => (dotNhap.CustomerId == null ? x.CustomerId == null : x.CustomerId == dotNhap.CustomerId))
                .ToListAsync(cancellationToken);

            var existingMap = existingDauBiens
                .Where(x => distinctDauBiens.Contains(x.Ma, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(x => x.Ma, x => x.Id!, StringComparer.OrdinalIgnoreCase);

            foreach (var dbCode in distinctDauBiens)
            {
                if (existingMap.TryGetValue(dbCode, out var existingId))
                {
                    dauBienMap[dbCode] = existingId;
                }
                else
                {
                    var newDauBien = new DauBienSo
                    {
                        CustomerId = dotNhap.CustomerId,
                        Ma = dbCode,
                        Ten = dbCode,
                        KyHieuSeries = dbCode,
                        ThuTu = 0,
                        IsActive = true
                    };
                    _dbContext.DauBienSos.Add(newDauBien);
                    dauBienMap[dbCode] = newDauBien.Id!;
                    _logger.LogInformation("Tự động thêm mới Đầu biển số '{Ma}' vào danh mục DauBienSo cho CustomerId: '{CustomerId}'", dbCode, dotNhap.CustomerId);
                }
            }

            if (string.IsNullOrWhiteSpace(dotNhap.KyHieuDaiDauBien))
            {
                dotNhap.KyHieuDaiDauBien = JsonSerializer.Serialize(distinctDauBiens);
            }
        }

        // Gán DauBienSoId cho từng ChiTietDotNhapBienSo
        foreach (var item in listBienSoItems)
        {
            var code = TrichXuatDauBienSo(item.BienKiemSoat);
            if (code != null && dauBienMap.TryGetValue(code, out var dbId))
            {
                item.DauBienSoId ??= dbId;
            }
        }

        // 2. Đồng bộ bảng ChiTietDotNhapBienSo và bảng KhoBienSo
        var oldChiTiets = await _dbContext.ChiTietDotNhapBienSos
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .ToListAsync(cancellationToken);

        var oldKhoEntities = await _dbContext.KhoBienSos
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .ToListAsync(cancellationToken);

        var matchedChiTietIds = new HashSet<string>();
        var matchedKhoIds = new HashSet<string>();

        // Đồng bộ ChiTietDotNhapBienSo
        foreach (var item in listBienSoItems)
        {
            item.DotNhapKhoId = dotNhap.Id!;

            var oldChiTiet = oldChiTiets.FirstOrDefault(x => !matchedChiTietIds.Contains(x.Id!) && !string.IsNullOrEmpty(item.Id) && x.Id == item.Id)
                          ?? oldChiTiets.FirstOrDefault(x => !matchedChiTietIds.Contains(x.Id!) && string.Equals(x.BienKiemSoat.Trim(), item.BienKiemSoat.Trim(), StringComparison.OrdinalIgnoreCase));

            if (oldChiTiet != null)
            {
                matchedChiTietIds.Add(oldChiTiet.Id!);
                oldChiTiet.ThuTu = item.ThuTu;
                oldChiTiet.BienKiemSoat = item.BienKiemSoat;

                if (item.LaBienDauGia)
                {
                    oldChiTiet.TrangThaiBienSoId = statusDauGia?.Id ?? item.TrangThaiBienSoId;
                    oldChiTiet.TrangThaiBienSoTen = statusDauGia?.Ten ?? "Chờ đấu giá";
                    oldChiTiet.MauSacHienThi = statusDauGia?.MauSacHienThi ?? "warning";
                }
                else if (isDaNhapKho)
                {
                    // Khi xác nhận nhập kho thì trạng thái biển là ĐƯỢC BẤM
                    oldChiTiet.TrangThaiBienSoId = defaultStatus?.Id ?? item.TrangThaiBienSoId;
                    oldChiTiet.TrangThaiBienSoTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)";
                    oldChiTiet.MauSacHienThi = defaultStatus?.MauSacHienThi ?? "success";
                }
                else
                {
                    oldChiTiet.TrangThaiBienSoId = item.TrangThaiBienSoId;
                    oldChiTiet.TrangThaiBienSoTen = item.TrangThaiBienSoTen ?? "Lưu tạm";
                    oldChiTiet.MauSacHienThi = item.MauSacHienThi ?? "secondary";
                }

                oldChiTiet.LaBienDauGia = item.LaBienDauGia;
                oldChiTiet.LoaiXeId = item.LoaiXeId ?? dotNhap.LoaiXeId;
                oldChiTiet.MauBienSoId = item.MauBienSoId ?? dotNhap.MauBienSoId;
                oldChiTiet.DauBienSoId = item.DauBienSoId;
                oldChiTiet.GhiChu = item.GhiChu ?? dotNhap.GhiChu;
                oldChiTiet.Modified = DateTime.UtcNow;

                item.Id = oldChiTiet.Id;
                item.TrangThaiBienSoId = oldChiTiet.TrangThaiBienSoId;
                item.TrangThaiBienSoTen = oldChiTiet.TrangThaiBienSoTen;
                item.MauSacHienThi = oldChiTiet.MauSacHienThi;
            }
            else
            {
                await _dbContext.ChiTietDotNhapBienSos.AddAsync(item, cancellationToken);
            }
        }

        // Xóa các ChiTiet thừa (những biển không còn trong danh sách gửi lên)
        var unMatchedChiTiets = oldChiTiets.Where(x => !matchedChiTietIds.Contains(x.Id!)).ToList();
        foreach (var un in unMatchedChiTiets)
        {
            var linkedKho = oldKhoEntities.FirstOrDefault(k => k.ChiTietDotNhapBienSoId == un.Id || string.Equals(k.BienKiemSoat.Trim(), un.BienKiemSoat.Trim(), StringComparison.OrdinalIgnoreCase));
            if (linkedKho != null && linkedKho.DaXuatKho)
            {
                throw new InvalidOperationException($"Biển kiểm soát '{linkedKho.BienKiemSoat}' đã được xuất kho, không thể xóa khỏi đợt nhập.");
            }
            _dbContext.ChiTietDotNhapBienSos.Remove(un);
        }

        // 3. Đồng bộ KhoBienSo
        if (isDaNhapKho)
        {
            dotNhap.DaNhapKho = true;
            dotNhap.TrangThai = "DaNhapKho";
            dotNhap.NgayNhapKhoThucTe ??= DateTime.UtcNow;
            dotNhap.NguoiNhapKho ??= (_currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên");

            var loaiXeEntity = !string.IsNullOrEmpty(dotNhap.LoaiXeId)
                ? await _dbContext.LoaiXes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dotNhap.LoaiXeId || x.Ma == dotNhap.LoaiXeId, cancellationToken)
                : null;
            var mauBienEntity = !string.IsNullOrEmpty(dotNhap.MauBienSoId)
                ? await _dbContext.MauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dotNhap.MauBienSoId || x.Ma == dotNhap.MauBienSoId, cancellationToken)
                : null;
            var loaiXeTen = loaiXeEntity?.Ten ?? "Ô tô";
            var mauBienTen = mauBienEntity?.Ten ?? "Biển Trắng";

            var bienSoLuuKho = listBienSoItems.Where(x => !x.LaBienDauGia).ToList();

            foreach (var item in bienSoLuuKho)
            {
                var code = TrichXuatDauBienSo(item.BienKiemSoat);
                var dauBienId = (code != null && dauBienMap.TryGetValue(code, out var dbId)) ? dbId : item.DauBienSoId;

                var existingKho = oldKhoEntities.FirstOrDefault(x => !matchedKhoIds.Contains(x.Id!) && !string.IsNullOrEmpty(item.Id) && (x.ChiTietDotNhapBienSoId == item.Id || x.Id == item.Id))
                               ?? oldKhoEntities.FirstOrDefault(x => !matchedKhoIds.Contains(x.Id!) && string.Equals(x.BienKiemSoat.Trim(), item.BienKiemSoat.Trim(), StringComparison.OrdinalIgnoreCase));

                if (existingKho != null)
                {
                    matchedKhoIds.Add(existingKho.Id!);

                    existingKho.CustomerId = dotNhap.CustomerId;
                    existingKho.ChiTietDotNhapBienSoId = item.Id!;
                    existingKho.ThuTu = item.ThuTu;
                    existingKho.BienKiemSoat = item.BienKiemSoat;
                    existingKho.LoaiXeId = item.LoaiXeId ?? dotNhap.LoaiXeId;
                    existingKho.MauBienSoId = item.MauBienSoId ?? dotNhap.MauBienSoId;
                    existingKho.DauBienSoId = dauBienId;
                    existingKho.GhiChu = item.GhiChu ?? dotNhap.GhiChu ?? existingKho.GhiChu;
                    existingKho.NgayNhapKho = dotNhap.NgayNhapKho;
                    existingKho.Modified = DateTime.UtcNow;

                    if (existingKho.DaXuatKho)
                    {
                        // ĐÃ XUẤT KHO: Giữ nguyên trạng thái DaBam
                        existingKho.TrangThai = "DaBam";
                        existingKho.TrangThaiTen = "Đã bấm (Đã cấp)";

                        // Đồng bộ toàn bộ thông tin mới sang phiếu Xuất kho liên kết
                        var xuatKho = await _dbContext.XuatKhoBienSos
                            .FirstOrDefaultAsync(x => x.KhoBienSoId == existingKho.Id || (!string.IsNullOrEmpty(existingKho.XuatKhoId) && x.Id == existingKho.XuatKhoId), cancellationToken);
                        if (xuatKho != null)
                        {
                            xuatKho.BienKiemSoat = existingKho.BienKiemSoat;
                            xuatKho.LoaiXeId = existingKho.LoaiXeId;
                            xuatKho.LoaiXeTen = loaiXeTen;
                            xuatKho.MauBienSoId = existingKho.MauBienSoId;
                            xuatKho.MauBienSoTen = mauBienTen;
                            xuatKho.DauBienSoId = existingKho.DauBienSoId;
                            var dbTen = distinctDauBiens.FirstOrDefault(d => TrichXuatDauBienSo(existingKho.BienKiemSoat) == d);
                            if (!string.IsNullOrEmpty(dbTen))
                            {
                                xuatKho.DauBienSoTen = dbTen;
                            }
                            xuatKho.Modified = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // CHƯA XUẤT KHO: Đảm bảo trạng thái 'DuocBam' (Biển được bấm / Tồn kho)
                        existingKho.TrangThai = "DuocBam";
                        existingKho.TrangThaiTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)";
                        existingKho.TrangThaiBienSoId = defaultStatus?.Id ?? item.TrangThaiBienSoId;
                    }
                }
                else
                {
                    // Biển mới bổ sung vào đợt nhập: Thêm vào KhoBienSo với trạng thái 'DuocBam'
                    var newKho = new KhoBienSo
                    {
                        CustomerId = dotNhap.CustomerId,
                        DotNhapKhoId = dotNhap.Id!,
                        ChiTietDotNhapBienSoId = item.Id!,
                        ThuTu = item.ThuTu,
                        BienKiemSoat = item.BienKiemSoat,
                        TrangThaiBienSoId = defaultStatus?.Id ?? item.TrangThaiBienSoId,
                        TrangThai = "DuocBam",
                        TrangThaiTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)",
                        LaBienDauGia = false,
                        LoaiXeId = item.LoaiXeId ?? dotNhap.LoaiXeId,
                        MauBienSoId = item.MauBienSoId ?? dotNhap.MauBienSoId,
                        DauBienSoId = dauBienId,
                        GhiChu = item.GhiChu ?? dotNhap.GhiChu ?? "Nhập kho chính thức",
                        NgayNhapKho = dotNhap.NgayNhapKho,
                        DaXuatKho = false
                    };
                    await _dbContext.KhoBienSos.AddAsync(newKho, cancellationToken);
                }
            }

            // Xử lý các bản ghi KhoBienSo cũ không còn trong danh sách lưu kho
            var unmatchedKhos = oldKhoEntities.Where(x => !matchedKhoIds.Contains(x.Id!)).ToList();
            foreach (var unKho in unmatchedKhos)
            {
                if (unKho.DaXuatKho)
                {
                    throw new InvalidOperationException($"Biển kiểm soát '{unKho.BienKiemSoat}' đã được xuất kho, không thể xóa khỏi đợt nhập.");
                }
                _dbContext.KhoBienSos.Remove(unKho);
            }

            // Đảm bảo đồng bộ tuyệt đối NgayNhapKho và danh mục liên quan cho toàn bộ KhoBienSo của đợt nhập
            var allKhoOfDotNhap = await _dbContext.KhoBienSos
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .ToListAsync(cancellationToken);
            foreach (var k in allKhoOfDotNhap)
            {
                k.NgayNhapKho = dotNhap.NgayNhapKho;
                if (!string.IsNullOrEmpty(dotNhap.LoaiXeId)) k.LoaiXeId = dotNhap.LoaiXeId;
                if (!string.IsNullOrEmpty(dotNhap.MauBienSoId)) k.MauBienSoId = dotNhap.MauBienSoId;
                k.Modified = DateTime.UtcNow;
            }
        }
        else
        {
            // DaNhapKho == false: Đưa về bản nháp
            dotNhap.DaNhapKho = false;
            dotNhap.TrangThai = "LuuTam";

            var exportedInBatch = oldKhoEntities.Where(x => x.DaXuatKho).ToList();
            if (exportedInBatch.Count > 0)
            {
                throw new InvalidOperationException($"Đợt nhập đã có {exportedInBatch.Count} biển số đã xuất kho ({exportedInBatch[0].BienKiemSoat}), không thể chuyển về lưu tạm.");
            }

            if (oldKhoEntities.Count > 0)
            {
                _dbContext.KhoBienSos.RemoveRange(oldKhoEntities);
            }
        }

        dotNhap.TongSoBien = listBienSoItems.Count;
        dotNhap.SoBienDuocBam = listBienSoItems.Count(x => !x.LaBienDauGia);
        dotNhap.SoBienChoDauGia = listBienSoItems.Count(x => x.LaBienDauGia);
        dotNhap.SoBienDaBam = oldKhoEntities.Count(x => x.DaXuatKho);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "NhapKho",
            isUpdate ? "CapNhat" : (dotNhap.DaNhapKho ? "XacNhan" : "ThemMoi"),
            dotNhap.MaDotNhap,
            isUpdate 
                ? $"Cập nhật đợt nhập kho '{dotNhap.MaDotNhap}' ({dotNhap.TongSoBien} biển số)."
                : (dotNhap.DaNhapKho 
                    ? $"Tạo mới và xác nhận nhập kho đợt '{dotNhap.MaDotNhap}' ({dotNhap.TongSoBien} biển số)."
                    : $"Tạo mới đợt nhập kho '{dotNhap.MaDotNhap}' ({dotNhap.TongSoBien} biển số, bản nháp)."),
            dotNhap.Id,
            dotNhap.KyHieuDaiDauBien,
            dotNhap.TrangThai,
            dotNhap.DaNhapKho ? "Đã nhập kho" : "Bản nháp",
            null,
            dotNhap.CustomerId,
            dotNhap.GhiChu,
            cancellationToken);

        _logger.LogInformation("Đã lưu đợt nhập kho '{MaDotNhap}' thành công (DaNhapKho = {DaNhapKho}, Tổng biển = {Tong})",
            dotNhap.MaDotNhap, dotNhap.DaNhapKho, dotNhap.TongSoBien);

        return (await LayChiTietDotNhapAsync(dotNhap.Id!, cancellationToken))
            ?? throw new InvalidOperationException("Không thể tải thông tin đợt nhập sau khi lưu.");
    }

    /// <summary>
    /// Cập nhật dữ liệu đợt nhập kho và đồng bộ toàn bộ bảng KhoBienSo theo dữ liệu mới.
    /// Đối với các biển số đã xuất kho trước đó, giữ nguyên trạng thái xuất kho và đồng bộ dữ liệu sang phiếu xuất kho liên kết.
    /// </summary>
    public async Task<DotNhapKhoResponse> UpdateDotNhapKhoAsync(SetDotNhapKhoRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new ArgumentException("Vui lòng cung cấp Id của đợt nhập kho cần cập nhật.");
        }

        var response = await SetDotNhapKhoAsync(request, cancellationToken);

        // Đảm bảo cập nhật chính xác NgayNhapKho cho toàn bộ bản ghi KhoBienSo
        if (request.NgayNhapKho != default)
        {
            var targetUtc = request.NgayNhapKho.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.NgayNhapKho, DateTimeKind.Utc)
                : request.NgayNhapKho.ToUniversalTime();

            var khoList = await _dbContext.KhoBienSos
                .Where(x => x.DotNhapKhoId == request.Id)
                .ToListAsync(cancellationToken);

            bool hasChange = false;
            foreach (var k in khoList)
            {
                if (k.NgayNhapKho != targetUtc)
                {
                    k.NgayNhapKho = targetUtc;
                    k.Modified = DateTime.UtcNow;
                    hasChange = true;
                }
            }

            if (hasChange)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return response;
    }

    /// <summary>
    /// Xác nhận nhập kho theo Id đợt nhập kho.
    /// Khóa phiếu (DaNhapKho = true, TrangThai = 'DaNhapKho') và chuyển toàn bộ biển số trong kho sang trạng thái 'DuocBam' (Biển được bấm / Tồn kho).
    /// </summary>
    public async Task<DotNhapKhoResponse> XacNhanNhapKhoAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Vui lòng cung cấp Id của đợt nhập kho cần xác nhận.");
        }

        var dotNhap = await _dbContext.DotNhapKhos
            .FirstOrDefaultAsync(x => x.Id == id || x.MaDotNhap.ToLower() == id.ToLower(), cancellationToken);

        if (dotNhap == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đợt nhập kho với Id '{id}'.");
        }

        // Lấy danh mục trạng thái biển số mặc định
        var defaultStatus = await _dbContext.TrangThaiBienSos
            .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.ToLower().Contains("được bấm") || x.Ten.ToLower().Contains("bấm") || x.Ten.ToLower().Contains("tồn kho"), cancellationToken);
        var statusDauGia = await _dbContext.TrangThaiBienSos
            .FirstOrDefaultAsync(x => x.Ma == "CHO_DAU_GIA" || x.Ten.ToLower().Contains("đấu giá"), cancellationToken);

        // Cập nhật trạng thái đợt nhập
        dotNhap.DaNhapKho = true;
        dotNhap.TrangThai = "DaNhapKho";
        dotNhap.NgayNhapKhoThucTe ??= DateTime.UtcNow;
        dotNhap.NguoiNhapKho ??= (_currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên");
        dotNhap.Modified = DateTime.UtcNow;

        // Lấy toàn bộ danh sách ChiTietDotNhapBienSo của đợt này
        var chiTiets = await _dbContext.ChiTietDotNhapBienSos
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(cancellationToken);

        // Cập nhật trạng thái các biển số trong ChiTietDotNhapBienSo
        foreach (var ct in chiTiets)
        {
            var isDauGia = ct.LaBienDauGia || (!string.IsNullOrEmpty(ct.TrangThaiBienSoTen) && ct.TrangThaiBienSoTen.Contains("đấu giá", StringComparison.OrdinalIgnoreCase));
            if (isDauGia)
            {
                ct.LaBienDauGia = true;
                ct.TrangThaiBienSoId = statusDauGia?.Id ?? ct.TrangThaiBienSoId;
                ct.TrangThaiBienSoTen = statusDauGia?.Ten ?? "Chờ đấu giá";
                ct.MauSacHienThi = statusDauGia?.MauSacHienThi ?? "warning";
            }
            else
            {
                // Khi xác nhận nhập kho: Trạng thái biển luôn là ĐƯỢC BẤM
                ct.TrangThaiBienSoId = defaultStatus?.Id ?? ct.TrangThaiBienSoId;
                ct.TrangThaiBienSoTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)";
                ct.MauSacHienThi = defaultStatus?.MauSacHienThi ?? "success";
            }
            ct.Modified = DateTime.UtcNow;
        }

        // Lấy toàn bộ KhoBienSo hiện tại của đợt này
        var existingKhoList = await _dbContext.KhoBienSos
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .ToListAsync(cancellationToken);

        var existingKhoMapByChiTietId = existingKhoList
            .Where(x => !string.IsNullOrEmpty(x.ChiTietDotNhapBienSoId))
            .ToDictionary(x => x.ChiTietDotNhapBienSoId!, x => x);

        var existingKhoMapByPlate = existingKhoList
            .GroupBy(x => x.BienKiemSoat.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Đồng bộ vào KhoBienSo đối với các biển không phải đấu giá
        var nonAuctionList = chiTiets.Where(x => !x.LaBienDauGia).ToList();
        var matchedKhoIds = new HashSet<string>();

        // Trích xuất mã đầu biển để map DauBienSo
        var distinctCodes = nonAuctionList
            .Select(x => TrichXuatDauBienSo(x.BienKiemSoat))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dauBienMap = await _dbContext.DauBienSos
            .AsNoTracking()
            .Where(d => distinctCodes.Contains(d.Ma) || distinctCodes.Contains(d.Ten))
            .ToDictionaryAsync(d => d.Ma, d => d.Id!, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var ct in nonAuctionList)
        {
            KhoBienSo? existingKho = null;
            if (ct.Id != null && existingKhoMapByChiTietId.TryGetValue(ct.Id, out var k1)) existingKho = k1;
            else if (existingKhoMapByPlate.TryGetValue(ct.BienKiemSoat.Trim(), out var k2)) existingKho = k2;

            var code = TrichXuatDauBienSo(ct.BienKiemSoat);
            var dauBienId = (code != null && dauBienMap.TryGetValue(code, out var dbId)) ? dbId : ct.DauBienSoId;

            if (existingKho != null)
            {
                matchedKhoIds.Add(existingKho.Id!);
                existingKho.CustomerId = dotNhap.CustomerId;
                existingKho.ChiTietDotNhapBienSoId = ct.Id!;
                existingKho.ThuTu = ct.ThuTu;
                existingKho.BienKiemSoat = ct.BienKiemSoat;
                existingKho.LoaiXeId = ct.LoaiXeId ?? dotNhap.LoaiXeId;
                existingKho.MauBienSoId = ct.MauBienSoId ?? dotNhap.MauBienSoId;
                existingKho.DauBienSoId = dauBienId;
                existingKho.GhiChu = ct.GhiChu ?? dotNhap.GhiChu ?? existingKho.GhiChu;
                existingKho.NgayNhapKho = dotNhap.NgayNhapKho;
                existingKho.Modified = DateTime.UtcNow;

                if (existingKho.DaXuatKho)
                {
                    existingKho.TrangThai = "DaBam";
                    existingKho.TrangThaiTen = "Đã bấm (Đã cấp)";
                }
                else
                {
                    // Biển chưa xuất kho: trạng thái là được bấm
                    existingKho.TrangThai = "DuocBam";
                    existingKho.TrangThaiTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)";
                    existingKho.TrangThaiBienSoId = defaultStatus?.Id ?? existingKho.TrangThaiBienSoId;
                }
            }
            else
            {
                var newKho = new KhoBienSo
                {
                    CustomerId = dotNhap.CustomerId,
                    DotNhapKhoId = dotNhap.Id!,
                    ChiTietDotNhapBienSoId = ct.Id!,
                    ThuTu = ct.ThuTu,
                    BienKiemSoat = ct.BienKiemSoat,
                    TrangThaiBienSoId = defaultStatus?.Id ?? ct.TrangThaiBienSoId,
                    TrangThai = "DuocBam",
                    TrangThaiTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)",
                    LaBienDauGia = false,
                    LoaiXeId = ct.LoaiXeId ?? dotNhap.LoaiXeId,
                    MauBienSoId = ct.MauBienSoId ?? dotNhap.MauBienSoId,
                    DauBienSoId = dauBienId,
                    GhiChu = ct.GhiChu ?? dotNhap.GhiChu ?? "Nhập kho chính thức",
                    NgayNhapKho = dotNhap.NgayNhapKho,
                    DaXuatKho = false
                };
                await _dbContext.KhoBienSos.AddAsync(newKho, cancellationToken);
            }
        }

        // Xóa các kho bản ghi nếu có biển đấu giá đã từng bị lưu vào kho
        var auctionKhoList = existingKhoList.Where(x => !matchedKhoIds.Contains(x.Id!) && nonAuctionList.All(na => na.BienKiemSoat != x.BienKiemSoat)).ToList();
        foreach (var unKho in auctionKhoList)
        {
            if (!unKho.DaXuatKho)
            {
                _dbContext.KhoBienSos.Remove(unKho);
            }
        }

        dotNhap.TongSoBien = chiTiets.Count;
        dotNhap.SoBienDuocBam = chiTiets.Count(x => !x.LaBienDauGia);
        dotNhap.SoBienChoDauGia = chiTiets.Count(x => x.LaBienDauGia);
        dotNhap.SoBienDaBam = existingKhoList.Count(x => x.DaXuatKho);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "NhapKho",
            "XacNhan",
            dotNhap.MaDotNhap,
            $"Xác nhận nhập kho thành công đợt '{dotNhap.MaDotNhap}' ({dotNhap.TongSoBien} biển số). Toàn bộ biển số đã chuyển sang trạng thái Biển được bấm (Tồn kho).",
            dotNhap.Id,
            dotNhap.KyHieuDaiDauBien,
            dotNhap.TrangThai,
            "Đã nhập kho",
            null,
            dotNhap.CustomerId,
            dotNhap.GhiChu,
            cancellationToken);

        _logger.LogInformation("Đã xác nhận nhập kho thành công cho đợt nhập '{MaDotNhap}' (Id: {Id})", dotNhap.MaDotNhap, dotNhap.Id);

        return (await LayChiTietDotNhapAsync(dotNhap.Id!, cancellationToken))
            ?? throw new InvalidOperationException("Không thể tải thông tin đợt nhập sau khi xác nhận.");
    }

    /// <summary>
    /// 3. (GET) Lấy thông tin 1 đợt nhập kho theo Id.
    /// </summary>
    public async Task<DotNhapKhoResponse?> LayChiTietDotNhapAsync(string id, CancellationToken cancellationToken = default)
    {
        var dotNhap = await _dbContext.DotNhapKhos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id || x.MaDotNhap.ToLower() == id.ToLower(), cancellationToken);

        if (dotNhap == null) return null;

        // Tải danh sách biển số chi tiết từ ChiTietDotNhapBienSo (luôn lưu toàn bộ biển số của đợt)
        var itemsChiTiet = await _dbContext.ChiTietDotNhapBienSos
            .AsNoTracking()
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(cancellationToken);

        // Lấy danh sách biển số trong KhoBienSo
        var khoBienSos = dotNhap.DaNhapKho
            ? await _dbContext.KhoBienSos
                .AsNoTracking()
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .ToListAsync(cancellationToken)
            : new List<KhoBienSo>();

        var khoMapByChiTietId = khoBienSos
            .Where(x => !string.IsNullOrEmpty(x.ChiTietDotNhapBienSoId))
            .ToDictionary(x => x.ChiTietDotNhapBienSoId!, x => x);

        var khoMapByPlate = khoBienSos
            .GroupBy(x => x.BienKiemSoat, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var listPlates = itemsChiTiet.Select(b =>
        {
            KhoBienSo? k = null;
            if (b.Id != null && khoMapByChiTietId.TryGetValue(b.Id, out var kb1)) k = kb1;
            else if (khoMapByPlate.TryGetValue(b.BienKiemSoat, out var kb2)) k = kb2;

            bool daLuu = dotNhap.DaNhapKho && !b.LaBienDauGia && k != null;
            bool daXuat = k?.DaXuatKho ?? false;
            bool isDauGia = b.LaBienDauGia || (!string.IsNullOrEmpty(b.TrangThaiBienSoTen) && b.TrangThaiBienSoTen.Contains("đấu giá", StringComparison.OrdinalIgnoreCase));

            string trangThai;
            string trangThaiTen;
            string mauSac;
            string? trangThaiBienSoId;

            if (daXuat)
            {
                trangThai = "DaBam";
                trangThaiTen = "Đã bấm (Đã cấp)";
                mauSac = "secondary";
                trangThaiBienSoId = k?.TrangThaiBienSoId ?? b.TrangThaiBienSoId;
            }
            else if (isDauGia)
            {
                trangThai = "ChoDauGia";
                trangThaiTen = "Chờ đấu giá";
                mauSac = "warning";
                trangThaiBienSoId = b.TrangThaiBienSoId;
            }
            else if (dotNhap.DaNhapKho)
            {
                // Khi đợt nhập đã xác nhận nhập kho: Trạng thái biển là ĐƯỢC BẤM
                trangThai = "DuocBam";
                trangThaiTen = k?.TrangThaiTen ?? b.TrangThaiBienSoTen;
                if (string.IsNullOrWhiteSpace(trangThaiTen) || trangThaiTen == "Lưu tạm")
                {
                    trangThaiTen = "Biển được bấm (Tồn kho)";
                }
                mauSac = "success";
                trangThaiBienSoId = k?.TrangThaiBienSoId ?? b.TrangThaiBienSoId;
            }
            else
            {
                // Bản nháp / Chưa nhập kho
                trangThai = "LuuTam";
                trangThaiTen = !string.IsNullOrWhiteSpace(b.TrangThaiBienSoTen) ? b.TrangThaiBienSoTen : "Lưu tạm";
                mauSac = b.MauSacHienThi ?? "secondary";
                trangThaiBienSoId = b.TrangThaiBienSoId;
            }

            return new KhoBienSoItemResponse
            {
                Id = b.Id!,
                STT = b.ThuTu,
                BienKiemSoat = b.BienKiemSoat,
                TrangThaiBienSoId = trangThaiBienSoId,
                TrangThai = trangThai,
                TrangThaiTen = trangThaiTen,
                MauSacHienThi = mauSac,
                LaBienDauGia = isDauGia,
                GhiChu = k?.GhiChu ?? b.GhiChu,
                NgayNhapKho = k?.NgayNhapKho ?? dotNhap.NgayNhapKho,
                Modified = b.Modified,
                DaLuuKho = daLuu,
                DaXuatKho = daXuat,
                NgayXuatKho = k?.NgayXuatKho,
                XuatKhoId = k?.XuatKhoId
            };
        }).ToList();

        // Tải thông tin Danh mục liên kết
        var loaiXe = !string.IsNullOrEmpty(dotNhap.LoaiXeId)
            ? await _dbContext.LoaiXes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dotNhap.LoaiXeId || x.Ma == dotNhap.LoaiXeId, cancellationToken)
            : null;

        var mauBien = !string.IsNullOrEmpty(dotNhap.MauBienSoId)
            ? await _dbContext.MauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dotNhap.MauBienSoId || x.Ma == dotNhap.MauBienSoId, cancellationToken)
            : null;

        var tinhChat = !string.IsNullOrEmpty(dotNhap.TinhChatDotNhapId)
            ? await _dbContext.TinhChatDotNhaps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dotNhap.TinhChatDotNhapId || x.Ma == dotNhap.TinhChatDotNhapId, cancellationToken)
            : null;

        var kyHieuDaiDauBienList = new List<string>();
        if (!string.IsNullOrEmpty(dotNhap.KyHieuDaiDauBien))
        {
            try
            {
                kyHieuDaiDauBienList = JsonSerializer.Deserialize<List<string>>(dotNhap.KyHieuDaiDauBien) ?? new();
            }
            catch
            {
                kyHieuDaiDauBienList = dotNhap.KyHieuDaiDauBien.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
        }

        return new DotNhapKhoResponse
        {
            Id = dotNhap.Id!,
            CustomerId = dotNhap.CustomerId,
            MaDotNhap = dotNhap.MaDotNhap,
            NgayNhapKho = dotNhap.NgayNhapKho,
            LoaiXeId = dotNhap.LoaiXeId,
            LoaiXeTen = loaiXe?.Ten ?? "Ô tô",
            MauBienSoId = dotNhap.MauBienSoId,
            MauBienSoTen = mauBien?.Ten ?? "Biển Trắng",
            MauBienSoHex = mauBien?.MaMauHex ?? "#FFFFFF",
            KyHieuDaiDauBien = kyHieuDaiDauBienList,
            CanBoTiepNhan = dotNhap.CanBoTiepNhan ?? "Trung Tá - Mai Cao",
            TinhChatDotNhapId = dotNhap.TinhChatDotNhapId,
            TinhChatDotNhapTen = tinhChat?.Ten ?? "Khai báo tồn đầu",
            SoCongVan = dotNhap.SoCongVan ?? "BB-TK-0108/CAP",
            GhiChu = dotNhap.GhiChu,
            TongSoBien = dotNhap.TongSoBien,
            SoBienDuocBam = dotNhap.SoBienDuocBam,
            SoBienDaBam = dotNhap.SoBienDaBam,
            SoBienChoDauGia = dotNhap.SoBienChoDauGia,
            TrangThai = dotNhap.TrangThai,
            TrangThaiPhieuTen = dotNhap.DaNhapKho ? "Đã nhập kho" : "Bản nháp",
            DaNhapKho = dotNhap.DaNhapKho,
            NgayNhapKhoThucTe = dotNhap.NgayNhapKhoThucTe,
            NguoiNhapKho = dotNhap.NguoiNhapKho,
            Created = dotNhap.Created,
            CreatedByName = dotNhap.CreatedByName,
            DanhSachBienSo = listPlates
        };
    }

    /// <summary>
    /// 4. (GET) Xóa đợt nhập kho theo Id.
    /// </summary>
    public async Task<bool> XoaDotNhapKhoAsync(string id, CancellationToken cancellationToken = default)
    {
        var dotNhap = await _dbContext.DotNhapKhos
            .FirstOrDefaultAsync(x => x.Id == id || x.MaDotNhap.ToLower() == id.ToLower(), cancellationToken);

        if (dotNhap == null)
        {
            return false;
        }

        // Xóa các bản ghi liên quan trong KhoBienSo nếu có
        var khoBienSos = await _dbContext.KhoBienSos
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .ToListAsync(cancellationToken);
        if (khoBienSos.Count > 0)
        {
            _dbContext.KhoBienSos.RemoveRange(khoBienSos);
        }

        // Xóa các bản ghi chi tiết biển số trong ChiTietDotNhapBienSo
        var chiTiets = await _dbContext.ChiTietDotNhapBienSos
            .Where(x => x.DotNhapKhoId == dotNhap.Id)
            .ToListAsync(cancellationToken);
        if (chiTiets.Count > 0)
        {
            _dbContext.ChiTietDotNhapBienSos.RemoveRange(chiTiets);
        }

        // Xóa phiếu đợt nhập kho
        _dbContext.DotNhapKhos.Remove(dotNhap);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "NhapKho",
            "Xoa",
            dotNhap.MaDotNhap,
            $"Xóa hoàn toàn đợt nhập kho '{dotNhap.MaDotNhap}' ({dotNhap.TongSoBien} biển số) và các biển số liên quan khỏi hệ thống.",
            dotNhap.Id,
            dotNhap.KyHieuDaiDauBien,
            "DaXoa",
            "Đã xóa",
            null,
            dotNhap.CustomerId,
            dotNhap.GhiChu,
            cancellationToken);

        _logger.LogInformation("Đã xóa hoàn toàn đợt nhập kho '{MaDotNhap}' (Id: {Id}) và các biển số liên quan.", dotNhap.MaDotNhap, dotNhap.Id);

        return true;
    }

    /// <summary>
    /// Trích xuất đầu biển số từ biển kiểm soát theo quy tắc:
    /// Bỏ 2 số cuối đi, phần còn lại lưu vào danh mục DauBienSo.
    /// Ví dụ: "30H-123456" -> "30H-123", "30H-000.01" -> "30H-000", "30H-123.45" -> "30H-123".
    /// </summary>
    public static string? TrichXuatDauBienSo(string? bienKiemSoat)
    {
        if (string.IsNullOrWhiteSpace(bienKiemSoat))
            return null;

        var bks = bienKiemSoat.Trim().ToUpperInvariant();

        // 1. Trường hợp có dấu chấm (30H-000.01, 30H-123.45, 30B-207.66)
        var dotIndex = bks.LastIndexOf('.');
        if (dotIndex > 0)
        {
            return bks.Substring(0, dotIndex);
        }

        // 2. Trường hợp có dấu gạch ngang (30H-123456, 30H-12345)
        var dashIndex = bks.LastIndexOf('-');
        if (dashIndex > 0 && dashIndex < bks.Length - 1)
        {
            var prefix = bks.Substring(0, dashIndex);
            var numberPart = bks.Substring(dashIndex + 1);

            // Với dải số 5 hoặc 6 chữ số: lấy 3 số đầu (bỏ 2 số cuối của biển 5 số)
            if (numberPart.Length >= 5)
            {
                return $"{prefix}-{numberPart.Substring(0, 3)}";
            }

            if (numberPart.Length > 2)
            {
                return $"{prefix}-{numberPart.Substring(0, numberPart.Length - 2)}";
            }

            return prefix;
        }

        // 3. Trường hợp chuỗi liền không có dấu: bỏ 2 số cuối
        if (bks.Length > 2)
        {
            return bks.Substring(0, bks.Length - 2);
        }

        return bks;
    }

    /// <summary>
    /// 5. (GET) Lấy thông tin tệp tin Excel mẫu nhập kho biển số xe (MauBaoCao/NhapKhoBienSo.xlsx).
    /// Output trả về các tham số filename và path (mã hóa Base64) là input cho API tải file /api/FileUpload/download.
    /// </summary>
    public async Task<ExportMauNhapKhoResponse> ExportFileMauNhapKhoAsync(CancellationToken cancellationToken = default)
    {
        // 1. Xác định thư mục StorageFolder từ cấu hình appsettings
        var storageFolder = _configuration["FileStorage:StorageFolder"] ?? "Uploads";
        var storageDir = Path.IsPathRooted(storageFolder)
            ? storageFolder
            : Path.Combine(_environment.ContentRootPath, storageFolder);

        if (!Directory.Exists(storageDir))
        {
            Directory.CreateDirectory(storageDir);
        }

        var templateFileName = "NhapKhoBienSo.xlsx";
        var targetFilePath = Path.Combine(storageDir, templateFileName);

        // 2. Đảm bảo file tồn tại trong StorageFolder (sao chép từ MauBaoCao hoặc tự động sinh chuẩn bằng EPPlus)
        var sourceTemplate = Path.Combine(_environment.ContentRootPath, "MauBaoCao", templateFileName);
        if (File.Exists(sourceTemplate))
        {
            File.Copy(sourceTemplate, targetFilePath, overwrite: true);
        }
        else if (!File.Exists(targetFilePath))
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Sheet1");

            // Tiêu đề
            ws.Cells[1, 1].Value = "DANH SÁCH BIỂN KIỂM SOÁT";
            ws.Cells[1, 1, 1, 2].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 16;
            ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Dòng header
            ws.Cells[3, 1].Value = "STT";
            ws.Cells[3, 2].Value = "Biển kiểm soát";
            for (int c = 1; c <= 2; c++)
            {
                ws.Cells[3, c].Style.Font.Bold = true;
                ws.Cells[3, c].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells[3, c].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 230, 242));
                ws.Cells[3, c].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                ws.Cells[3, c].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            ws.Column(1).Width = 10;
            ws.Column(2).Width = 25;

            await package.SaveAsAsync(new FileInfo(targetFilePath), cancellationToken);
        }

        // 3. Mã hóa Base64 filename và path theo cấu hình StorageFolder
        var encodedFileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(templateFileName));
        var encodedPath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(targetFilePath));
        var downloadUrl = $"/api/FileUpload/download?filename={Uri.EscapeDataString(encodedFileName)}&path={Uri.EscapeDataString(encodedPath)}";

        _logger.LogInformation("Export mẫu nhập kho: Trả về input cho download file: filename='{FileName}', path='{Path}'", encodedFileName, encodedPath);

        return new ExportMauNhapKhoResponse
        {
            FileName = encodedFileName,
            Path = encodedPath,
            DownloadUrl = downloadUrl,
            OriginalFileName = templateFileName
        };
    }

    /// <summary>
    /// 6. (POST) Đọc dữ liệu danh sách biển số xe từ file Excel mẫu theo FileName trong thư mục StorageFolder của appsettings.
    /// Output trả về List&lt;BienSoChiTietItemRequest&gt;.
    /// </summary>
    public async Task<List<BienSoChiTietItemRequest>> ImportExcelBienSoAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Tên file (FileName) không được để trống.", nameof(fileName));
        }

        // 1. Xác định đường dẫn thư mục lưu trữ từ appsettings.json
        var storageFolder = _configuration["FileStorage:StorageFolder"] ?? "Uploads";
        var baseDir = Path.IsPathRooted(storageFolder)
            ? storageFolder
            : Path.Combine(_environment.ContentRootPath, storageFolder);

        // 2. Tìm tệp tin mục tiêu
        var cleanName = fileName.Replace('\\', '/').Trim();
        var folderPrefix = storageFolder.Replace('\\', '/').TrimEnd('/') + "/";
        if (cleanName.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            cleanName = cleanName.Substring(folderPrefix.Length);
        }
        var safeFileName = Path.GetFileName(cleanName);
        var filePath = Path.Combine(baseDir, safeFileName);

        if (!File.Exists(filePath))
        {
            // Kiểm tra các đường dẫn phụ phòng trường hợp client gửi relative path khác
            if (File.Exists(fileName))
            {
                filePath = fileName;
            }
            else
            {
                var altPath = Path.Combine(_environment.ContentRootPath, fileName.TrimStart('/', '\\'));
                if (File.Exists(altPath))
                {
                    filePath = altPath;
                }
                else
                {
                    throw new FileNotFoundException($"Không tìm thấy tệp tin '{fileName}' trong thư mục lưu trữ '{storageFolder}'.", fileName);
                }
            }
        }

        // 3. Đọc dữ liệu bằng EPPlus
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // Tải danh mục trạng thái biển số mặc định nếu có ("Biển được bấm (Tồn kho)")
        var defaultTrangThai = await _dbContext.TrangThaiBienSos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.Contains("được bấm") || x.Ten.Contains("Tồn kho"), cancellationToken);

        var listResult = new List<BienSoChiTietItemRequest>();

        using var package = new ExcelPackage(new FileInfo(filePath));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws == null || ws.Dimension == null)
        {
            return listResult;
        }

        // 4. Nhận diện dòng tiêu đề (Dòng 3 như file mẫu MauBaoCao/NhapKhoBienSo.xlsx hoặc quét linh hoạt)
        int headerRow = 3;
        int bienSoCol = 2;
        int sttCol = 1;
        int ghiChuCol = 3;

        int maxScanRow = Math.Min(10, ws.Dimension.End.Row);
        for (int r = 1; r <= maxScanRow; r++)
        {
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
            {
                var text = ws.Cells[r, c].Text?.Trim().ToLowerInvariant();
                if (text != null && (text.Contains("biển") || text.Contains("bien") || text.Contains("bks")))
                {
                    headerRow = r;
                    bienSoCol = c;
                    break;
                }
            }
            if (headerRow != 3 || bienSoCol != 2)
            {
                for (int c = 1; c <= ws.Dimension.End.Column; c++)
                {
                    var text = ws.Cells[headerRow, c].Text?.Trim().ToLowerInvariant();
                    if (text != null && (text == "stt" || text.Contains("thứ tự") || text.Contains("thu tu")))
                    {
                        sttCol = c;
                        break;
                    }
                }
                break;
            }
        }

        int currentOrder = 1;

        // 5. Duyệt từ dòng kế tiếp của header đến hết bảng
        for (int row = headerRow + 1; row <= ws.Dimension.End.Row; row++)
        {
            var rawBienSo = ws.Cells[row, bienSoCol].Text?.Trim();
            if (string.IsNullOrWhiteSpace(rawBienSo))
            {
                continue;
            }

            // Chuẩn hóa biển số: chữ hoa, xóa khoảng trắng thừa
            var bienKiemSoat = rawBienSo.ToUpperInvariant().Replace(" ", "");

            // STT
            int thuTu = currentOrder++;
            var rawStt = ws.Cells[row, sttCol].Text?.Trim();
            if (int.TryParse(rawStt, out var parsedStt) && parsedStt > 0)
            {
                thuTu = parsedStt;
            }

            // Ghi chú nếu có cột 3
            string? ghiChu = null;
            if (ws.Dimension.End.Column >= ghiChuCol && ghiChuCol != bienSoCol && ghiChuCol != sttCol)
            {
                ghiChu = ws.Cells[row, ghiChuCol].Text?.Trim();
            }

            // Đánh dấu biển đấu giá nếu ghi chú ghi nhận
            bool laBienDauGia = false;
            if (!string.IsNullOrWhiteSpace(ghiChu) && (ghiChu.ToLower().Contains("đấu giá") || ghiChu.ToLower().Contains("dau gia")))
            {
                laBienDauGia = true;
            }

            listResult.Add(new BienSoChiTietItemRequest
            {
                ThuTu = thuTu,
                BienKiemSoat = bienKiemSoat,
                TrangThaiBienSoId = defaultTrangThai?.Id,
                TrangThaiBienSoTen = defaultTrangThai?.Ten ?? "Biển được bấm (Tồn kho)",
                MauSacHienThi = "success",
                LaBienDauGia = laBienDauGia,
                GhiChu = ghiChu
            });
        }

        _logger.LogInformation("Import Excel thành công: Đã trích xuất {Count} biển số từ file '{FileName}'", listResult.Count, fileName);

        return listResult;
    }
}

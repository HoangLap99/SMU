using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Dịch vụ quản lý nghiệp vụ Đợt nhập kho và Kho biển số xe.
/// Cung cấp 4 chức năng cốt lõi:
/// 1. (POST) Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
/// 2. (POST) Thêm mới và update đợt nhập kho. Nếu update và DaNhapKho == true thì sẽ lưu kho (bỏ qua biển số LaBienDauGia == true).
/// 3. (GET) Lấy thông tin 1 đợt nhập kho theo Id.
/// 4. (GET) Xóa đợt nhập kho theo Id.
/// </summary>
public class NhapKhoService : INhapKhoService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NhapKhoService> _logger;

    public NhapKhoService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<NhapKhoService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
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
            query = query.Where(x =>
                x.MaDotNhap.ToLower().Contains(kw) ||
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
            query = query.Where(x => x.NgayNhapKho >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.NgayNhapKho <= request.ToDate.Value);
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
            .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.Contains("bấm"), cancellationToken);
        var statusDauGia = await _dbContext.TrangThaiBienSos
            .FirstOrDefaultAsync(x => x.Ma == "CHO_DAU_GIA" || x.Ten.Contains("đấu giá"), cancellationToken);

        // Kiểm tra xem là Thêm mới hay Update
        DotNhapKho? dotNhap = null;
        bool isUpdate = false;

        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            dotNhap = await _dbContext.DotNhapKhos.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            isUpdate = (dotNhap != null);
        }

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
                var st = isDauGia ? (statusDauGia ?? defaultStatus) : defaultStatus;

                listBienSoItems.Add(new ChiTietDotNhapBienSo
                {
                    CustomerId = customerId,
                    ThuTu = item.ThuTu > 0 ? item.ThuTu : stt++,
                    BienKiemSoat = item.BienKiemSoat.Trim().ToUpperInvariant(),
                    TrangThaiBienSoId = item.TrangThaiBienSoId ?? st?.Id,
                    TrangThaiBienSoTen = item.TrangThaiBienSoTen ?? st?.Ten ?? (isDauGia ? "Chờ đấu giá" : "Biển được bấm (Tồn kho)"),
                    MauSacHienThi = item.MauSacHienThi ?? st?.MauSacHienThi ?? (isDauGia ? "warning" : "success"),
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
                    TrangThaiBienSoTen = defaultStatus?.Ten ?? "Biển được bấm (Tồn kho)",
                    MauSacHienThi = defaultStatus?.MauSacHienThi ?? "success",
                    LaBienDauGia = false,
                    LoaiXeId = request.LoaiXeId,
                    MauBienSoId = request.MauBienSoId,
                    GhiChu = request.GhiChu ?? "Khai báo nhập kho tự động"
                });
            }
        }

        int tongSoBien = listBienSoItems.Count;
        int soBienDuocBam = listBienSoItems.Count(x => !x.LaBienDauGia);
        int soBienChoDauGia = listBienSoItems.Count(x => x.LaBienDauGia);

        // Lưu / Cập nhật bản ghi DotNhapKho
        if (!isUpdate)
        {
            dotNhap = new DotNhapKho
            {
                CustomerId = customerId,
                MaDotNhap = maDotNhapChuanHoa,
                NgayNhapKho = request.NgayNhapKho != default ? request.NgayNhapKho : DateTime.UtcNow,
                LoaiXeId = request.LoaiXeId,
                MauBienSoId = request.MauBienSoId,
                KyHieuDaiDauBien = kyHieuDaiDauBienJson,
                CanBoTiepNhan = request.CanBoTiepNhan?.Trim(),
                TinhChatDotNhapId = request.TinhChatDotNhapId,
                SoCongVan = request.SoCongVan?.Trim(),
                GhiChu = request.GhiChu?.Trim(),
                TrangThai = request.DaNhapKho ? "DaNhapKho" : "LuuTam",
                DaNhapKho = request.DaNhapKho,
                NgayNhapKhoThucTe = request.DaNhapKho ? DateTime.UtcNow : null,
                NguoiNhapKho = request.DaNhapKho ? (_currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên") : null,
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
            dotNhap.NgayNhapKho = request.NgayNhapKho != default ? request.NgayNhapKho : dotNhap.NgayNhapKho;
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
            dotNhap.Modified = DateTime.UtcNow;

            // Xóa các dòng biển số chi tiết cũ của đợt nhập để đồng bộ lại
            var oldChiTiets = await _dbContext.ChiTietDotNhapBienSos
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .ToListAsync(cancellationToken);
            if (oldChiTiets.Count > 0)
            {
                _dbContext.ChiTietDotNhapBienSos.RemoveRange(oldChiTiets);
            }
        }

        // Gán DotNhapKhoId cho các dòng biển số chi tiết và thêm vào ChiTietDotNhapBienSo
        foreach (var item in listBienSoItems)
        {
            item.DotNhapKhoId = dotNhap.Id!;
        }

        if (listBienSoItems.Count > 0)
        {
            await _dbContext.ChiTietDotNhapBienSos.AddRangeAsync(listBienSoItems, cancellationToken);
        }

        // NẾU update (hoặc tạo mới) VÀ DaNhapKho == true: THỰC HIỆN LƯU KHO
        if (request.DaNhapKho)
        {
            dotNhap.DaNhapKho = true;
            dotNhap.TrangThai = "DaNhapKho";
            dotNhap.NgayNhapKhoThucTe ??= DateTime.UtcNow;
            dotNhap.NguoiNhapKho ??= (_currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên");

            // Xóa dữ liệu cũ trong KhoBienSo của đợt nhập này nếu trước đó đã từng lưu kho
            var oldKhoEntities = await _dbContext.KhoBienSos
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .ToListAsync(cancellationToken);
            if (oldKhoEntities.Count > 0)
            {
                _dbContext.KhoBienSos.RemoveRange(oldKhoEntities);
            }

            // ĐIỀU KIỆN QUAN TRỌNG: Biển số đem đấu giá (LaBienDauGia == true) thì SẼ KHÔNG LƯU vào bảng KhoBienSo
            var bienSoLuuKho = listBienSoItems.Where(x => !x.LaBienDauGia).ToList();
            var listKhoEntities = bienSoLuuKho.Select(item => new KhoBienSo
            {
                CustomerId = dotNhap.CustomerId,
                DotNhapKhoId = dotNhap.Id!,
                ChiTietDotNhapBienSoId = item.Id!,
                ThuTu = item.ThuTu,
                BienKiemSoat = item.BienKiemSoat,
                TrangThaiBienSoId = item.TrangThaiBienSoId,
                TrangThai = "DuocBam",
                TrangThaiTen = item.TrangThaiBienSoTen,
                LaBienDauGia = false,
                LoaiXeId = item.LoaiXeId ?? dotNhap.LoaiXeId,
                MauBienSoId = item.MauBienSoId ?? dotNhap.MauBienSoId,
                DauBienSoId = item.DauBienSoId,
                GhiChu = item.GhiChu ?? "Nhập kho chính thức",
                NgayNhapKho = dotNhap.NgayNhapKhoThucTe ?? DateTime.UtcNow
            }).ToList();

            if (listKhoEntities.Count > 0)
            {
                await _dbContext.KhoBienSos.AddRangeAsync(listKhoEntities, cancellationToken);
            }

            _logger.LogInformation("Đợt nhập kho '{MaDotNhap}' đã lưu vào bảng KhoBienSo ({Count} biển số bấm ngẫu nhiên; {DauGiaCount} biển đấu giá không lưu vào kho).",
                dotNhap.MaDotNhap, listKhoEntities.Count, listBienSoItems.Count(x => x.LaBienDauGia));
        }
        else
        {
            // DaNhapKho == false: Đưa về bản nháp
            dotNhap.DaNhapKho = false;
            dotNhap.TrangThai = "LuuTam";

            // Nếu trước đó đã từng có trong KhoBienSo thì xóa đi vì đã chuyển về nháp
            var oldKhoEntities = await _dbContext.KhoBienSos
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .ToListAsync(cancellationToken);
            if (oldKhoEntities.Count > 0)
            {
                _dbContext.KhoBienSos.RemoveRange(oldKhoEntities);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã lưu đợt nhập kho '{MaDotNhap}' thành công (DaNhapKho = {DaNhapKho})",
            dotNhap.MaDotNhap, dotNhap.DaNhapKho);

        return (await LayChiTietDotNhapAsync(dotNhap.Id!, cancellationToken))
            ?? throw new InvalidOperationException("Không thể tải thông tin đợt nhập sau khi lưu.");
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

        // Lấy danh sách ID các biển số đã thực sự được lưu vào KhoBienSo
        var daLuuKhoChiTietIds = dotNhap.DaNhapKho
            ? await _dbContext.KhoBienSos
                .AsNoTracking()
                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                .Select(x => x.ChiTietDotNhapBienSoId)
                .ToListAsync(cancellationToken)
            : new List<string?>();

        var listPlates = itemsChiTiet.Select(b =>
        {
            // Biển số chỉ coi là đã lưu kho khi đợt nhập đã nhập kho, biển KHÔNG phải đấu giá, và có trong bảng KhoBienSo
            bool daLuu = dotNhap.DaNhapKho && !b.LaBienDauGia && daLuuKhoChiTietIds.Contains(b.Id);
            return new KhoBienSoItemResponse
            {
                Id = b.Id!,
                STT = b.ThuTu,
                BienKiemSoat = b.BienKiemSoat,
                TrangThaiBienSoId = b.TrangThaiBienSoId,
                TrangThai = (b.LaBienDauGia || b.TrangThaiBienSoTen.Contains("Đấu giá")) ? "ChoDauGia" : "DuocBam",
                TrangThaiTen = b.TrangThaiBienSoTen,
                MauSacHienThi = (b.LaBienDauGia || b.MauSacHienThi == "warning" || b.TrangThaiBienSoTen.Contains("Đấu giá")) ? "warning" : (b.MauSacHienThi ?? "success"),
                LaBienDauGia = b.LaBienDauGia || b.TrangThaiBienSoTen.Contains("Đấu giá"),
                GhiChu = b.GhiChu,
                NgayNhapKho = dotNhap.DaNhapKho ? (dotNhap.NgayNhapKhoThucTe ?? dotNhap.NgayNhapKho) : dotNhap.NgayNhapKho,
                Modified = b.Modified,
                DaLuuKho = daLuu
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

        _logger.LogInformation("Đã xóa hoàn toàn đợt nhập kho '{MaDotNhap}' (Id: {Id}) và các biển số liên quan.", dotNhap.MaDotNhap, dotNhap.Id);

        return true;
    }
}

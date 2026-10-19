using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;
using SMU.Infrastructure.Storage.Configuration;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai các nghiệp vụ Xuất kho biển số xe:
/// 1. Lấy danh sách biển số chưa xuất kho khả dụng trong bảng KhoBienSo
/// 2. Lấy thông tin mức thu lệ phí theo nội dung và khu vực (TT 155/2025/TT-BTC)
/// 3. Lấy thông tin số biên lai tiếp theo theo ký hiệu quyển
/// 4. Lập thủ tục &amp; biên lai thu tiền (Xuất kho)
/// 5. Quản lý danh sách, chi tiết và xóa phiếu xuất kho
/// </summary>
public class XuatKhoService : IXuatKhoService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<XuatKhoService> _logger;
    private readonly ILogHoatDongService _logHoatDongService;
    private readonly IWebHostEnvironment _environment;
    private readonly FileStorageSettings _fileStorageSettings;

    public XuatKhoService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<XuatKhoService> logger,
        ILogHoatDongService logHoatDongService,
        IWebHostEnvironment environment,
        IOptions<FileStorageSettings> fileStorageOptions)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _logHoatDongService = logHoatDongService;
        _environment = environment;
        _fileStorageSettings = fileStorageOptions.Value;
    }

    /// <summary>
    /// API 1: Lấy danh sách biển số chưa xuất kho khả dụng trong bảng KhoBienSo.
    /// Điều kiện: !DaXuatKho &amp;&amp; TrangThai == "DuocBam" &amp;&amp; !LaBienDauGia.
    /// Kèm tổng số biển khả dụng để hiển thị "(Còn 591 biển khả dụng trong kho)".
    /// </summary>
    public async Task<DanhSachBienSoKhaDungResponse> LayDanhSachBienSoKhaDungAsync(
        FilterBienSoKhaDungRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterBienSoKhaDungRequest();
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // Base query: Biển số chưa xuất kho, được bấm và không đem đấu giá
        var baseQuery = _dbContext.KhoBienSos
            .AsNoTracking()
            .Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            baseQuery = baseQuery.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        // Đếm tổng số biển khả dụng trong kho của đơn vị
        var tongSoKhaDung = await baseQuery.CountAsync(cancellationToken);

        // Áp dụng các bộ lọc bổ sung
        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(x => x.BienKiemSoat.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        if (!string.IsNullOrWhiteSpace(request.DauBienSoId))
        {
            query = query.Where(x => x.DauBienSoId == request.DauBienSoId);
        }

        var pageSize = request.PageSize > 0 ? request.PageSize : 100;
        var plates = await query
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.BienKiemSoat)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (plates.Count == 0)
        {
            return new DanhSachBienSoKhaDungResponse
            {
                TongSoBienKhaDung = tongSoKhaDung,
                Items = new List<BienSoKhaDungItemResponse>()
            };
        }

        // Tải thông tin danh mục liên quan để tạo chuỗi hiển thị đẹp
        var loaiXeIds = plates.Select(p => p.LoaiXeId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        var mauBienIds = plates.Select(p => p.MauBienSoId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        var dauBienIds = plates.Select(p => p.DauBienSoId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();

        var loaiXes = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null && loaiXeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var mauBiens = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null && mauBienIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Biển Trắng", cancellationToken);

        var dauBiens = await _dbContext.DauBienSos.AsNoTracking()
            .Where(x => x.Id != null && dauBienIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => x.KyHieuSeries ?? x.Ten ?? "Đầu biển", cancellationToken);

        var items = plates.Select(p =>
        {
            var lxTen = (p.LoaiXeId != null && loaiXes.TryGetValue(p.LoaiXeId, out var lx)) ? lx : "Ô tô";
            var mbTen = (p.MauBienSoId != null && mauBiens.TryGetValue(p.MauBienSoId, out var mb)) ? mb : "Biển Trắng";
            var dbTen = (p.DauBienSoId != null && dauBiens.TryGetValue(p.DauBienSoId, out var db)) 
                ? db 
                : (p.BienKiemSoat.Contains('-') ? p.BienKiemSoat.Substring(0, p.BienKiemSoat.IndexOf('.')) : "Đầu biển");

            // Format hiển thị chuẩn như giao diện modal:
            // "30B-207.66 • Ô tô (Biển Trắng - Đầu 30B-207)"
            var thongTinHienThi = $"{p.BienKiemSoat} • {lxTen} ({mbTen} - Đầu {dbTen})";

            return new BienSoKhaDungItemResponse
            {
                Id = p.Id,
                BienKiemSoat = p.BienKiemSoat,
                LoaiXeId = p.LoaiXeId,
                LoaiXeTen = lxTen,
                MauBienSoId = p.MauBienSoId,
                MauBienSoTen = mbTen,
                DauBienSoId = p.DauBienSoId,
                DauBienSoTen = dbTen,
                ThongTinHienThi = thongTinHienThi,
                TrangThai = p.TrangThai,
                TrangThaiTen = p.TrangThaiTen,
                NgayNhapKho = p.NgayNhapKho
            };
        }).ToList();

        return new DanhSachBienSoKhaDungResponse
        {
            TongSoBienKhaDung = tongSoKhaDung,
            Items = items
        };
    }

    /// <summary>
    /// API 2: Lấy thông tin mức thu và tự động điền số tiền dựa vào nội dung lệ phí (MucThuLePhiId) và khu vực (KhuVucApDungId) từ bảng MucThuLePhiKhuVuc.
    /// </summary>
    public async Task<ThongTinMucThuTheoKhuVucResponse?> LayMucThuTheoKhuVucAsync(
        string mucThuLePhiId,
        string khuVucApDungId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mucThuLePhiId) || string.IsNullOrWhiteSpace(khuVucApDungId))
        {
            return null;
        }

        var mucThuLePhi = await _dbContext.MucThuLePhis
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>x.IsActive && x.Id == mucThuLePhiId, cancellationToken);

        if (mucThuLePhi == null)
        {
            return null;
        }

        var khuVuc = await _dbContext.KhuVucApDungs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == khuVucApDungId, cancellationToken);

        if (khuVuc == null)
        {
            return null;
        }

        // Lấy chi tiết mức thu từ bảng MucThuLePhiKhuVuc
        var chiTiet = await _dbContext.MucThuLePhiKhuVucs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MucThuLePhiId == mucThuLePhiId && x.KhuVucApDungId == khuVucApDungId, cancellationToken);

        decimal mucThu = chiTiet?.MucThu ?? 0;

        // Tìm các tùy chọn khoảng trị giá xe liên quan trong cùng nhóm xe / cùng loại thủ tục để hiển thị các nút điều kiện
        var relatedMucThuOptions = new List<DieuKienTriGiaXeOption>();

        var relatedItems = await _dbContext.MucThuLePhis
            .AsNoTracking()
            .Where(x => x.Id == mucThuLePhiId || (mucThuLePhi.LoaiXeId != null && x.LoaiXeId == mucThuLePhi.LoaiXeId && x.LoaiThuTucId == mucThuLePhi.LoaiThuTucId))
            .OrderBy(x => x.ThuTu)
            .ToListAsync(cancellationToken);

        if (relatedItems.Count > 0)
        {
            var relatedIds = relatedItems.Select(r => r.Id).ToList();
            var relatedKhuVucs = await _dbContext.MucThuLePhiKhuVucs
                .AsNoTracking()
                .Where(x => relatedIds.Contains(x.MucThuLePhiId) && x.KhuVucApDungId == khuVucApDungId)
                .ToDictionaryAsync(x => x.MucThuLePhiId, x => x, cancellationToken);

            foreach (var item in relatedItems)
            {
                if (relatedKhuVucs.TryGetValue(item.Id, out var kv))
                {
                    var label = !string.IsNullOrWhiteSpace(item.KhoangTriGiaXe) 
                        ? item.KhoangTriGiaXe 
                        : (item.Ten ?? "Trị giá xe");
                    
                    relatedMucThuOptions.Add(new DieuKienTriGiaXeOption
                    {
                        MucThuLePhiId = item.Id,
                        MucThuLePhiKhuVucId = kv.Id,
                        KhoangTriGiaXe = label,
                        MucThu = kv.MucThu,
                        TieuDeHienThi = $"{label} (Mức thu: {kv.MucThu:N0}đ)",
                        GhiChu = kv.GhiChu
                    });
                }
            }
        }

        // Căn cứ pháp lý và ghi chú
        var canCuPhapLy = mucThuLePhi.CanCuPhapLy ?? "Thông tư 155/2025/TT-BTC";
        var ghiChuHuongDan = mucThuLePhi.GhiChuHuongDan ?? $"{khuVuc.Ten} ({khuVuc.PhamVi})";

        return new ThongTinMucThuTheoKhuVucResponse
        {
            MucThuLePhiId = mucThuLePhi.Id,
            MucThuLePhiTen = mucThuLePhi.Ten ?? string.Empty,
            KhuVucApDungId = khuVuc.Id,
            KhuVucApDungTen = khuVuc.Ten ?? string.Empty,
            MucThuLePhiKhuVucId = chiTiet?.Id,
            MucThu = mucThu,
            CanCuPhapLy = canCuPhapLy,
            GhiChuHuongDan = ghiChuHuongDan,
            BatBuocChonTriGiaXe = mucThuLePhi.BatBuocChonTriGiaXe,
            KhoangTriGiaXe = mucThuLePhi.KhoangTriGiaXe,
            SoTienTuDongDien = mucThu,
            DanhSachDieuKienTriGia = relatedMucThuOptions
        };
    }

    /// <summary>
    /// API 3: Lấy thông tin quyển biên lai và gợi ý số biên lai tiếp theo theo KyHieuQuyenBienLaiId.
    /// </summary>
    public async Task<SoBienLaiTiepTheoResponse?> LaySoBienLaiTiepTheoAsync(
        string kyHieuQuyenBienLaiId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(kyHieuQuyenBienLaiId))
        {
            return null;
        }

        var quyen = await _dbContext.KyHieuQuyenBienLais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == kyHieuQuyenBienLaiId, cancellationToken);

        if (quyen == null)
        {
            return null;
        }

        // Tính số biên lai tiếp theo:
        // Nếu đã có SoHienTai thì + 1; nếu chưa có thì lấy TuSo hoặc 1
        long soTiepTheo = (quyen.SoHienTai ?? ((quyen.TuSo ?? 1) - 1)) + 1;
        var soBienLaiGoiY = soTiepTheo.ToString("D7");

        long conLai = 0;
        if (quyen.DenSo.HasValue)
        {
            conLai = quyen.DenSo.Value - soTiepTheo + 1;
            if (conLai < 0) conLai = 0;
        }

        return new SoBienLaiTiepTheoResponse
        {
            KyHieuQuyenBienLaiId = quyen.Id,
            KyHieuQuyen = quyen.Ma ?? string.Empty,
            TuSo = quyen.TuSo,
            DenSo = quyen.DenSo,
            SoHienTai = quyen.SoHienTai,
            SoBienLaiGoiY = soBienLaiGoiY,
            ConLai = conLai
        };
    }

    /// <summary>
    /// API 4: Thêm mới hoặc Cập nhật phiếu Lập thủ tục &amp; Biên lai thu tiền (Xuất kho).
    /// Tự động cập nhật bản ghi trong KhoBienSo sang DaXuatKho = true, TrangThai = "DaBam" và cập nhật SoHienTai của quyển biên lai.
    /// </summary>
    public async Task<XuatKhoResponse> SetXuatKhoAsync(
        SetXuatKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Kiểm tra Biển số trong kho (KhoBienSo)
        var bienSo = await _dbContext.KhoBienSos
            .FirstOrDefaultAsync(x => x.Id == request.KhoBienSoId, cancellationToken);

        if (bienSo == null)
        {
            throw new ArgumentException($"Không tìm thấy biển số với Id '{request.KhoBienSoId}' trong kho.");
        }

        bool isAdd = string.IsNullOrWhiteSpace(request.Id);

        // Nếu thực hiện xuất kho (DaXuatKho == true): kiểm tra biển số phải khả dụng trong kho
        if (request.DaXuatKho)
        {
            if (isAdd)
            {
                if (bienSo.DaXuatKho)
                {
                    throw new InvalidOperationException($"Biển số '{bienSo.BienKiemSoat}' đã được xuất kho trước đó và không còn khả dụng.");
                }

                if (bienSo.TrangThai != "DuocBam")
                {
                    throw new InvalidOperationException($"Biển số '{bienSo.BienKiemSoat}' hiện ở trạng thái '{bienSo.TrangThaiTen}', không thuộc diện Biển được bấm để xuất kho.");
                }
            }
        }
        else
        {
            // Trường hợp lưu tạm (DaXuatKho == false): nếu biển số đã bị xuất kho chính thức bởi phiếu khác
            if (isAdd && bienSo.DaXuatKho)
            {
                throw new InvalidOperationException($"Biển số '{bienSo.BienKiemSoat}' đã được xuất kho trước đó và không còn khả dụng.");
            }
        }

        //// 2. Kiểm tra Quyển biên lai (KyHieuQuyenBienLai)
        var quyen = await _dbContext.KyHieuQuyenBienLais
            .FirstOrDefaultAsync(x => x.Id == request.KyHieuQuyenBienLaiId, cancellationToken);

        if (quyen == null)
            quyen = new KyHieuQuyenBienLai();

        // 4. Lấy thông tin các danh mục liên quan để snapshot
        var loaiThuTuc = await _dbContext.LoaiThuTucs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LoaiThuTucId, cancellationToken);

        var mucThuLePhi = await _dbContext.MucThuLePhis.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.MucThuLePhiId, cancellationToken);

        var khuVuc = await _dbContext.KhuVucApDungs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.KhuVucApDungId, cancellationToken);

        var loaiXe = bienSo.LoaiXeId != null 
            ? await _dbContext.LoaiXes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bienSo.LoaiXeId, cancellationToken)
            : null;

        var mauBien = bienSo.MauBienSoId != null
            ? await _dbContext.MauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bienSo.MauBienSoId, cancellationToken)
            : null;

        var dauBien = bienSo.DauBienSoId != null
            ? await _dbContext.DauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bienSo.DauBienSoId, cancellationToken)
            : null;

        XuatKhoBienSo xuatKho;

        if (isAdd)
        {
            xuatKho = new XuatKhoBienSo
            {
                CustomerId = customerId,
                LoaiThuTucId = request.LoaiThuTucId,
                LoaiThuTucTen = loaiThuTuc?.Ten ?? "Cấp biển (Có xuất kho số)",
                NgayThucHien = request.NgayThucHien,
                TinhChatXuatKho = !string.IsNullOrWhiteSpace(request.TinhChatXuatKho) 
                    ? request.TinhChatXuatKho 
                    : "Xuất 01 biển từ kho 'Biển được bấm'",
                KhoBienSoId = bienSo.Id,
                BienKiemSoat = bienSo.BienKiemSoat,
                LoaiXeId = bienSo.LoaiXeId,
                LoaiXeTen = loaiXe?.Ten ?? "Ô tô",
                MauBienSoId = bienSo.MauBienSoId,
                MauBienSoTen = mauBien?.Ten ?? "Biển Trắng",
                DauBienSoId = bienSo.DauBienSoId,
                DauBienSoTen = dauBien?.KyHieuSeries ?? dauBien?.Ten,

                MucThuLePhiId = request.MucThuLePhiId,
                MucThuLePhiTen = mucThuLePhi?.Ten,
                KhuVucApDungId = request.KhuVucApDungId,
                KhuVucApDungTen = khuVuc?.Ten,
                MucThuLePhiKhuVucId = request.MucThuLePhiKhuVucId,
                DieuKienTriGiaXe = request.DieuKienTriGiaXe,
                CanCuPhapLy = !string.IsNullOrWhiteSpace(request.CanCuPhapLy) 
                    ? request.CanCuPhapLy 
                    : (mucThuLePhi?.CanCuPhapLy ?? "Thông tư 155/2025/TT-BTC"),
                SoTienTuDongDien = request.SoTienTuDongDien ?? request.SoTienThu,

                KyHieuQuyenBienLaiId = request.KyHieuQuyenBienLaiId,
                KyHieuQuyen = quyen.Ma ?? string.Empty,
                SoBienLai = request.SoBienLai.Trim(),
                NgayBienLai = request.NgayBienLai,
                SoTienThu = request.SoTienThu,
                HoTenNguoiNop = request.HoTenNguoiNop.Trim(),
                SoCccd = request.SoCccd?.Trim(),
                MaHoSoDvc = request.MaHoSoDvc?.Trim(),
                GhiChu = request.GhiChu,
                DaXuatKho = request.DaXuatKho,
                TrangThai = request.DaXuatKho ? "HoanTat" : "LuuTam"
            };

            await _dbContext.XuatKhoBienSos.AddAsync(xuatKho, cancellationToken);
        }
        else
        {
            var existing = await _dbContext.XuatKhoBienSos
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (existing == null)
            {
                throw new ArgumentException($"Không tìm thấy phiếu xuất kho với Id '{request.Id}'.");
            }

            // Xử lý chuyển đổi biển số cũ nếu trước đó phiếu đã xuất kho và nay đổi sang biển số khác hoặc chuyển sang chưa xuất kho
            if (existing.DaXuatKho && (existing.KhoBienSoId != request.KhoBienSoId || !request.DaXuatKho))
            {
                var oldBienSo = await _dbContext.KhoBienSos
                    .FirstOrDefaultAsync(x => x.Id == existing.KhoBienSoId, cancellationToken);
                if (oldBienSo != null)
                {
                    var statusDuocBam = await _dbContext.TrangThaiBienSos
                        .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.ToLower().Contains("được bấm") || x.Ten.ToLower().Contains("bấm") || x.Ten.ToLower().Contains("tồn kho"), cancellationToken);

                    oldBienSo.DaXuatKho = false;
                    oldBienSo.NgayXuatKho = null;
                    oldBienSo.XuatKhoId = null;
                    oldBienSo.TrangThai = "DuocBam";
                    oldBienSo.TrangThaiTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
                    oldBienSo.TrangThaiBienSoId = statusDuocBam?.Id ?? oldBienSo.TrangThaiBienSoId;
                    oldBienSo.Modified = DateTime.UtcNow;

                    var oldChiTiet = !string.IsNullOrWhiteSpace(oldBienSo.ChiTietDotNhapBienSoId)
                        ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.Id == oldBienSo.ChiTietDotNhapBienSoId, cancellationToken)
                        : (!string.IsNullOrWhiteSpace(oldBienSo.DotNhapKhoId)
                            ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.DotNhapKhoId == oldBienSo.DotNhapKhoId && x.BienKiemSoat.ToLower() == oldBienSo.BienKiemSoat.ToLower(), cancellationToken)
                            : null);

                    if (oldChiTiet != null)
                    {
                        oldChiTiet.TrangThaiBienSoId = statusDuocBam?.Id ?? oldChiTiet.TrangThaiBienSoId;
                        oldChiTiet.TrangThaiBienSoTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
                        oldChiTiet.MauSacHienThi = statusDuocBam?.MauSacHienThi ?? "success";
                        oldChiTiet.Modified = DateTime.UtcNow;
                    }

                    if (!string.IsNullOrWhiteSpace(oldBienSo.DotNhapKhoId))
                    {
                        var dotNhap = await _dbContext.DotNhapKhos
                            .FirstOrDefaultAsync(x => x.Id == oldBienSo.DotNhapKhoId, cancellationToken);
                        if (dotNhap != null)
                        {
                            var dotNhapKhoList = await _dbContext.KhoBienSos
                                .Where(x => x.DotNhapKhoId == dotNhap.Id)
                                .ToListAsync(cancellationToken);
                            dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho && x.Id != oldBienSo.Id);
                            dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => (!x.DaXuatKho || x.Id == oldBienSo.Id) && !x.LaBienDauGia);
                            dotNhap.Modified = DateTime.UtcNow;
                        }
                    }
                }
            }

            // Nếu cập nhật thành xuất kho với biển số mới hoặc phiếu trước đó chưa xuất kho:
            if (request.DaXuatKho && (!existing.DaXuatKho || existing.KhoBienSoId != request.KhoBienSoId))
            {
                if (bienSo.DaXuatKho)
                {
                    throw new InvalidOperationException($"Biển số '{bienSo.BienKiemSoat}' đã được xuất kho trước đó.");
                }

                if (bienSo.TrangThai != "DuocBam")
                {
                    throw new InvalidOperationException($"Biển số '{bienSo.BienKiemSoat}' hiện ở trạng thái '{bienSo.TrangThaiTen}', không thuộc diện Biển được bấm để xuất kho.");
                }
            }

            xuatKho = existing;
            xuatKho.LoaiThuTucId = request.LoaiThuTucId;
            xuatKho.LoaiThuTucTen = loaiThuTuc?.Ten ?? xuatKho.LoaiThuTucTen;
            xuatKho.NgayThucHien = request.NgayThucHien;
            xuatKho.TinhChatXuatKho = request.TinhChatXuatKho ?? xuatKho.TinhChatXuatKho;

            // Cập nhật thông tin phương tiện / biển số nếu có truyền từ API mới
            if (!string.IsNullOrWhiteSpace(request.BienKiemSoat))
            {
                bienSo.BienKiemSoat = request.BienKiemSoat.Trim().ToUpperInvariant();
            }
            if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
            {
                bienSo.LoaiXeId = request.LoaiXeId;
            }
            if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
            {
                bienSo.MauBienSoId = request.MauBienSoId;
            }
            if (!string.IsNullOrWhiteSpace(request.DauBienSoId))
            {
                bienSo.DauBienSoId = request.DauBienSoId;
            }

            loaiXe = bienSo.LoaiXeId != null 
                ? await _dbContext.LoaiXes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bienSo.LoaiXeId || x.Ma == bienSo.LoaiXeId, cancellationToken)
                : loaiXe;
            mauBien = bienSo.MauBienSoId != null
                ? await _dbContext.MauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bienSo.MauBienSoId || x.Ma == bienSo.MauBienSoId, cancellationToken)
                : mauBien;
            dauBien = bienSo.DauBienSoId != null
                ? await _dbContext.DauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bienSo.DauBienSoId || x.Ma == bienSo.DauBienSoId, cancellationToken)
                : dauBien;

            xuatKho.KhoBienSoId = bienSo.Id;
            xuatKho.BienKiemSoat = bienSo.BienKiemSoat;
            xuatKho.LoaiXeId = bienSo.LoaiXeId;
            xuatKho.LoaiXeTen = loaiXe?.Ten ?? xuatKho.LoaiXeTen;
            xuatKho.MauBienSoId = bienSo.MauBienSoId;
            xuatKho.MauBienSoTen = mauBien?.Ten ?? xuatKho.MauBienSoTen;
            xuatKho.DauBienSoId = bienSo.DauBienSoId;
            xuatKho.DauBienSoTen = dauBien?.KyHieuSeries ?? dauBien?.Ten ?? xuatKho.DauBienSoTen;

            xuatKho.MucThuLePhiId = request.MucThuLePhiId;
            xuatKho.MucThuLePhiTen = mucThuLePhi?.Ten ?? xuatKho.MucThuLePhiTen;
            xuatKho.KhuVucApDungId = request.KhuVucApDungId;
            xuatKho.KhuVucApDungTen = khuVuc?.Ten ?? xuatKho.KhuVucApDungTen;
            xuatKho.MucThuLePhiKhuVucId = request.MucThuLePhiKhuVucId;
            xuatKho.DieuKienTriGiaXe = request.DieuKienTriGiaXe;
            xuatKho.CanCuPhapLy = request.CanCuPhapLy ?? xuatKho.CanCuPhapLy;
            xuatKho.SoTienTuDongDien = request.SoTienTuDongDien ?? request.SoTienThu;

            xuatKho.KyHieuQuyenBienLaiId = request.KyHieuQuyenBienLaiId;
            xuatKho.KyHieuQuyen = quyen.Ma ?? string.Empty;
            xuatKho.SoBienLai = request.SoBienLai.Trim();
            xuatKho.NgayBienLai = request.NgayBienLai;
            xuatKho.SoTienThu = request.SoTienThu;
            xuatKho.HoTenNguoiNop = request.HoTenNguoiNop.Trim();
            xuatKho.SoCccd = request.SoCccd?.Trim();
            xuatKho.MaHoSoDvc = request.MaHoSoDvc?.Trim();
            xuatKho.GhiChu = request.GhiChu;
            xuatKho.DaXuatKho = request.DaXuatKho;
            xuatKho.TrangThai = request.DaXuatKho ? "HoanTat" : "LuuTam";
            xuatKho.Modified = DateTime.UtcNow;
        }

        // 5. CẬP NHẬT DỮ LIỆU CỦA KhoBienSo
        if (request.DaXuatKho)
        {
            var statusDaBam = await _dbContext.TrangThaiBienSos
                .FirstOrDefaultAsync(x => x.Ma == "DA_BAM" || x.Ten.ToLower().Contains("đã bấm") || x.Ten.ToLower().Contains("đã cấp"), cancellationToken);

            bienSo.DaXuatKho = true;
            bienSo.NgayXuatKho = request.NgayThucHien;
            bienSo.XuatKhoId = xuatKho.Id;
            bienSo.TrangThai = "DaBam";
            bienSo.TrangThaiTen = statusDaBam?.Ten ?? "Đã bấm (Đã cấp)";
            bienSo.TrangThaiBienSoId = statusDaBam?.Id ?? bienSo.TrangThaiBienSoId;
            bienSo.Modified = DateTime.UtcNow;

            // 6. Cập nhật dòng chi tiết đợt nhập (nếu có liên kết)
            var chiTiet = !string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId)
                ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken)
                : (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId)
                    ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.DotNhapKhoId == bienSo.DotNhapKhoId && x.BienKiemSoat.ToLower() == bienSo.BienKiemSoat.ToLower(), cancellationToken)
                    : null);

            if (chiTiet != null)
            {
                chiTiet.BienKiemSoat = bienSo.BienKiemSoat;
                chiTiet.LoaiXeId = bienSo.LoaiXeId;
                chiTiet.MauBienSoId = bienSo.MauBienSoId;
                chiTiet.DauBienSoId = bienSo.DauBienSoId;
                chiTiet.TrangThaiBienSoId = statusDaBam?.Id ?? chiTiet.TrangThaiBienSoId;
                chiTiet.TrangThaiBienSoTen = statusDaBam?.Ten ?? "Đã bấm (Đã cấp)";
                chiTiet.MauSacHienThi = statusDaBam?.MauSacHienThi ?? "secondary";
                chiTiet.Modified = DateTime.UtcNow;
            }

            // 7. Cập nhật Số hiện tại của Quyển biên lai
            if (long.TryParse(request.SoBienLai.Trim(), out var soMoi))
            {
                if (!quyen.SoHienTai.HasValue || soMoi > quyen.SoHienTai.Value)
                {
                    quyen.SoHienTai = soMoi;
                }
            }

            // 8. Cập nhật số lượng đợt nhập kho liên kết (nếu có)
            if (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId))
            {
                var dotNhap = await _dbContext.DotNhapKhos
                    .FirstOrDefaultAsync(x => x.Id == bienSo.DotNhapKhoId, cancellationToken);
                if (dotNhap != null)
                {
                    var dotNhapKhoList = await _dbContext.KhoBienSos
                        .Where(x => x.DotNhapKhoId == dotNhap.Id)
                        .ToListAsync(cancellationToken);
                    dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho || x.Id == bienSo.Id);
                    dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => !x.DaXuatKho && x.Id != bienSo.Id && !x.LaBienDauGia);
                    dotNhap.Modified = DateTime.UtcNow;
                }
            }
        }
        else
        {
            var statusDuocBam = await _dbContext.TrangThaiBienSos
                .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.ToLower().Contains("được bấm") || x.Ten.ToLower().Contains("bấm") || x.Ten.ToLower().Contains("tồn kho"), cancellationToken);

            // DaXuatKho == false: Hoàn tác trạng thái xuất kho của biển số này
            bienSo.DaXuatKho = false;
            bienSo.NgayXuatKho = null;
            bienSo.XuatKhoId = null;
            bienSo.TrangThai = "DuocBam";
            bienSo.TrangThaiTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
            bienSo.TrangThaiBienSoId = statusDuocBam?.Id ?? bienSo.TrangThaiBienSoId;
            bienSo.Modified = DateTime.UtcNow;

            var chiTiet = !string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId)
                ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken)
                : (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId)
                    ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.DotNhapKhoId == bienSo.DotNhapKhoId && x.BienKiemSoat.ToLower() == bienSo.BienKiemSoat.ToLower(), cancellationToken)
                    : null);

            if (chiTiet != null)
            {
                chiTiet.TrangThaiBienSoId = statusDuocBam?.Id ?? chiTiet.TrangThaiBienSoId;
                chiTiet.TrangThaiBienSoTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
                chiTiet.MauSacHienThi = statusDuocBam?.MauSacHienThi ?? "success";
                chiTiet.Modified = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId))
            {
                var dotNhap = await _dbContext.DotNhapKhos
                    .FirstOrDefaultAsync(x => x.Id == bienSo.DotNhapKhoId, cancellationToken);
                if (dotNhap != null)
                {
                    var dotNhapKhoList = await _dbContext.KhoBienSos
                        .Where(x => x.DotNhapKhoId == dotNhap.Id)
                        .ToListAsync(cancellationToken);
                    dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho && x.Id != bienSo.Id);
                    dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => (!x.DaXuatKho || x.Id == bienSo.Id) && !x.LaBienDauGia);
                    dotNhap.Modified = DateTime.UtcNow;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "XuatKho",
            isAdd ? (xuatKho.DaXuatKho ? "XacNhan" : "ThemMoi") : "CapNhat",
            xuatKho.SoBienLai,
            isAdd 
                ? $"Lập thủ tục & biên lai xuất kho '{xuatKho.KyHieuQuyen}-{xuatKho.SoBienLai}' cho biển số {xuatKho.BienKiemSoat} ({xuatKho.HoTenNguoiNop}), thu {xuatKho.SoTienThu:N0}đ."
                : $"Cập nhật phiếu xuất kho '{xuatKho.KyHieuQuyen}-{xuatKho.SoBienLai}' cho biển số {xuatKho.BienKiemSoat}.",
            xuatKho.Id,
            xuatKho.BienKiemSoat,
            xuatKho.TrangThai,
            xuatKho.TrangThai == "HoanTat" ? "Hoàn tất" : (xuatKho.TrangThai == "DaHuy" ? "Đã hủy" : "Lưu tạm"),
            null,
            xuatKho.CustomerId,
            xuatKho.GhiChu,
            cancellationToken);

        _logger.LogInformation("Đã lưu thành công phiếu xuất kho '{Id}' cho biển số '{BienKiemSoat}', số biên lai '{SoBienLai}', DaXuatKho = {DaXuatKho}",
            xuatKho.Id, xuatKho.BienKiemSoat, xuatKho.SoBienLai, xuatKho.DaXuatKho);

        return MapToResponse(xuatKho);
    }

    /// <summary>
    /// Cập nhật phiếu Lập thủ tục &amp; Biên lai xuất kho và đồng bộ toàn bộ bảng KhoBienSo theo dữ liệu mới.
    /// Đối với trường hợp đã xuất kho, cập nhật thông tin biển số trong KhoBienSo và xử lý hoàn tác/chuyển đổi biển số nếu thay đổi.
    /// </summary>
    public async Task<XuatKhoResponse> UpdateXuatKhoAsync(
        SetXuatKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new ArgumentException("Vui lòng cung cấp Id của phiếu xuất kho cần cập nhật.");
        }
        return await SetXuatKhoAsync(request, cancellationToken);
    }

    /// <summary>
    /// Xác nhận xuất kho theo Id phiếu xuất kho.
    /// Khóa phiếu (DaXuatKho = true, TrangThai = 'HoanTat') và chuyển trạng thái biển số trong KhoBienSo sang 'DaBam' (Đã bấm / Đã cấp).
    /// </summary>
    public async Task<XuatKhoResponse> XacNhanXuatKhoAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Mã phiếu xuất kho không được để trống.", nameof(id));
        }

        var xuatKho = await _dbContext.XuatKhoBienSos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (xuatKho == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiếu xuất kho với Id '{id}'.");
        }

        if (xuatKho.TrangThai == "DaHuy")
        {
            throw new InvalidOperationException($"Phiếu xuất kho cho biển số '{xuatKho.BienKiemSoat}' đã bị hủy, không thể xác nhận xuất kho.");
        }

        var bienSo = await _dbContext.KhoBienSos
            .FirstOrDefaultAsync(x => x.Id == xuatKho.KhoBienSoId, cancellationToken);

        if (bienSo == null)
        {
            throw new InvalidOperationException($"Không tìm thấy biển số liên kết với phiếu xuất kho.");
        }

        if (bienSo.DaXuatKho && !string.IsNullOrEmpty(bienSo.XuatKhoId) && bienSo.XuatKhoId != xuatKho.Id)
        {
            throw new InvalidOperationException($"Biển số '{bienSo.BienKiemSoat}' đã được xuất kho bởi một phiếu khác.");
        }

        // Lấy danh mục trạng thái biển số Đã bấm
        var statusDaBam = await _dbContext.TrangThaiBienSos
            .FirstOrDefaultAsync(x => x.Ma == "DA_BAM" || x.Ten.ToLower().Contains("đã bấm") || x.Ten.ToLower().Contains("đã cấp"), cancellationToken);

        // 1. Cập nhật phiếu xuất kho
        xuatKho.DaXuatKho = true;
        xuatKho.TrangThai = "HoanTat";
        xuatKho.Modified = DateTime.UtcNow;

        // 2. Cập nhật trạng thái biển số trong KhoBienSo thành ĐÃ BẤM
        bienSo.DaXuatKho = true;
        bienSo.NgayXuatKho = xuatKho.NgayThucHien;
        bienSo.XuatKhoId = xuatKho.Id;
        bienSo.TrangThai = "DaBam";
        bienSo.TrangThaiTen = statusDaBam?.Ten ?? "Đã bấm (Đã cấp)";
        bienSo.TrangThaiBienSoId = statusDaBam?.Id ?? bienSo.TrangThaiBienSoId;
        bienSo.Modified = DateTime.UtcNow;

        // 3. Cập nhật chi tiết đợt nhập (nếu có)
        var chiTiet = !string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId)
            ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken)
            : (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId)
                ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.DotNhapKhoId == bienSo.DotNhapKhoId && x.BienKiemSoat.ToLower() == bienSo.BienKiemSoat.ToLower(), cancellationToken)
                : null);

        if (chiTiet != null)
        {
            chiTiet.TrangThaiBienSoId = statusDaBam?.Id ?? chiTiet.TrangThaiBienSoId;
            chiTiet.TrangThaiBienSoTen = statusDaBam?.Ten ?? "Đã bấm (Đã cấp)";
            chiTiet.MauSacHienThi = statusDaBam?.MauSacHienThi ?? "secondary";
            chiTiet.Modified = DateTime.UtcNow;
        }

        // 4. Cập nhật số hiện tại của quyển biên lai
        if (!string.IsNullOrWhiteSpace(xuatKho.KyHieuQuyenBienLaiId))
        {
            var quyen = await _dbContext.KyHieuQuyenBienLais
                .FirstOrDefaultAsync(x => x.Id == xuatKho.KyHieuQuyenBienLaiId, cancellationToken);
            if (quyen != null && long.TryParse(xuatKho.SoBienLai.Trim(), out var soMoi))
            {
                if (!quyen.SoHienTai.HasValue || soMoi > quyen.SoHienTai.Value)
                {
                    quyen.SoHienTai = soMoi;
                }
            }
        }

        // 5. Cập nhật số liệu đợt nhập kho liên kết (nếu có)
        if (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId))
        {
            var dotNhap = await _dbContext.DotNhapKhos
                .FirstOrDefaultAsync(x => x.Id == bienSo.DotNhapKhoId, cancellationToken);
            if (dotNhap != null)
            {
                var dotNhapKhoList = await _dbContext.KhoBienSos
                    .Where(x => x.DotNhapKhoId == dotNhap.Id)
                    .ToListAsync(cancellationToken);
                dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho || x.Id == bienSo.Id);
                dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => !x.DaXuatKho && x.Id != bienSo.Id && !x.LaBienDauGia);
                dotNhap.Modified = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "XuatKho",
            "XacNhan",
            xuatKho.SoBienLai,
            $"Xác nhận xuất kho phiếu '{xuatKho.KyHieuQuyen}-{xuatKho.SoBienLai}' cho biển số {xuatKho.BienKiemSoat}. Cấp biển thành công cho {xuatKho.HoTenNguoiNop}.",
            xuatKho.Id,
            xuatKho.BienKiemSoat,
            xuatKho.TrangThai,
            "Hoàn tất",
            null,
            xuatKho.CustomerId,
            xuatKho.GhiChu,
            cancellationToken);

        _logger.LogInformation("Đã xác nhận xuất kho thành công cho phiếu '{Id}' (Biển số: {BienKiemSoat}, Số biên lai: {SoBienLai}), biển số chuyển sang 'DaBam'",
            xuatKho.Id, xuatKho.BienKiemSoat, xuatKho.SoBienLai);

        return MapToResponse(xuatKho);
    }

    /// <summary>
    /// API 5: Lấy danh sách phiếu xuất kho có phân trang và bộ lọc.
    /// </summary>
    public async Task<PagedResult<XuatKhoResponse>> LayDanhSachXuatKhoAsync(
        PagedXuatKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        request ??= new PagedXuatKhoRequest();
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.XuatKhoBienSos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiThuTucId))
        {
            query = query.Where(x => x.LoaiThuTucId == request.LoaiThuTucId);
        }

        if (!string.IsNullOrWhiteSpace(request.KyHieuQuyenBienLaiId))
        {
            query = query.Where(x => x.KyHieuQuyenBienLaiId == request.KyHieuQuyenBienLaiId);
        }

        if (request.DaXuatKho.HasValue)
        {
            query = query.Where(x => x.DaXuatKho == request.DaXuatKho.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            query = query.Where(x => x.TrangThai == request.TrangThai.Trim());
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.NgayThucHien >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.NgayThucHien <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.KeyWord))
        {
            var kw = request.KeyWord.Trim().ToLower();
            query = query.Where(x =>
                x.BienKiemSoat.ToLower().Contains(kw) ||
                x.HoTenNguoiNop.ToLower().Contains(kw) ||
                (x.SoCccd != null && x.SoCccd.ToLower().Contains(kw)) ||
                (x.MaHoSoDvc != null && x.MaHoSoDvc.ToLower().Contains(kw)) ||
                x.SoBienLai.ToLower().Contains(kw));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var items = await query
            .OrderByDescending(x => x.Created)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(MapToResponse).ToList();

        return new PagedResult<XuatKhoResponse>
        {
            Items = mapped,
            TotalCount = totalItems,
            CurrentPage = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// API 6: Lấy chi tiết một phiếu xuất kho theo Id.
    /// </summary>
    public async Task<XuatKhoResponse?> LayChiTietXuatKhoAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var entity = await _dbContext.XuatKhoBienSos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : MapToResponse(entity);
    }

    /// <summary>
    /// API 7: Xóa phiếu xuất kho theo Id. Hoàn tác trạng thái biển số trong KhoBienSo về DaXuatKho = false, TrangThai = "DuocBam" nếu phiếu đã xuất kho.
    /// </summary>
    public async Task<bool> XoaXuatKhoAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var entity = await _dbContext.XuatKhoBienSos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null) return false;

        // Hoàn tác trạng thái biển số trong KhoBienSo nếu phiếu đã thực hiện xuất kho (DaXuatKho == true)
        if (entity.DaXuatKho)
        {
            var bienSo = await _dbContext.KhoBienSos
                .FirstOrDefaultAsync(x => x.Id == entity.KhoBienSoId || x.XuatKhoId == entity.Id, cancellationToken);

            if (bienSo != null)
            {
                var statusDuocBam = await _dbContext.TrangThaiBienSos
                    .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.ToLower().Contains("được bấm") || x.Ten.ToLower().Contains("bấm") || x.Ten.ToLower().Contains("tồn kho"), cancellationToken);

                bienSo.DaXuatKho = false;
                bienSo.NgayXuatKho = null;
                bienSo.XuatKhoId = null;
                bienSo.TrangThai = "DuocBam";
                bienSo.TrangThaiTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
                bienSo.TrangThaiBienSoId = statusDuocBam?.Id ?? bienSo.TrangThaiBienSoId;
                bienSo.Modified = DateTime.UtcNow;

                var chiTiet = !string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId)
                    ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken)
                    : (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId)
                        ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.DotNhapKhoId == bienSo.DotNhapKhoId && x.BienKiemSoat.ToLower() == bienSo.BienKiemSoat.ToLower(), cancellationToken)
                        : null);

                if (chiTiet != null)
                {
                    chiTiet.TrangThaiBienSoId = statusDuocBam?.Id ?? chiTiet.TrangThaiBienSoId;
                    chiTiet.TrangThaiBienSoTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
                    chiTiet.MauSacHienThi = statusDuocBam?.MauSacHienThi ?? "success";
                    chiTiet.Modified = DateTime.UtcNow;
                }

                if (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId))
                {
                    var dotNhap = await _dbContext.DotNhapKhos
                        .FirstOrDefaultAsync(x => x.Id == bienSo.DotNhapKhoId, cancellationToken);
                    if (dotNhap != null)
                    {
                        var dotNhapKhoList = await _dbContext.KhoBienSos
                            .Where(x => x.DotNhapKhoId == dotNhap.Id)
                            .ToListAsync(cancellationToken);
                        dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho && x.Id != bienSo.Id);
                        dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => (!x.DaXuatKho || x.Id == bienSo.Id) && !x.LaBienDauGia);
                        dotNhap.Modified = DateTime.UtcNow;
                    }
                }
            }
        }

        _dbContext.XuatKhoBienSos.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "XuatKho",
            "Xoa",
            entity.SoBienLai,
            $"Xóa phiếu xuất kho '{entity.KyHieuQuyen}-{entity.SoBienLai}' cho biển số {entity.BienKiemSoat} và hoàn tác trạng thái biển số về kho.",
            entity.Id,
            entity.BienKiemSoat,
            "DaXoa",
            "Đã xóa",
            null,
            entity.CustomerId,
            entity.GhiChu,
            cancellationToken);

        _logger.LogInformation("Đã xóa phiếu xuất kho '{Id}' (DaXuatKho = {DaXuatKho}) và hoàn tác biển số '{BienKiemSoat}'",
            entity.Id, entity.DaXuatKho, entity.BienKiemSoat);

        return true;
    }

    /// <summary>
    /// API 8: Hủy phiếu xuất kho theo Id.
    /// Cập nhật trạng thái phiếu xuất kho sang 'DaHuy', DaXuatKho = false và khôi phục trạng thái biển số trong KhoBienSo về DaXuatKho = false, TrangThai = 'DuocBam'.
    /// </summary>
    public async Task<XuatKhoResponse> HuyPhieuXuatKhoAsync(
        string id, 
        HuyPhieuXuatKhoRequest? request = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Mã phiếu xuất kho không được để trống.", nameof(id));
        }

        var entity = await _dbContext.XuatKhoBienSos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiếu xuất kho có Id: '{id}'.");
        }

        if (entity.TrangThai == "DaHuy")
        {
            throw new InvalidOperationException($"Phiếu xuất kho cho biển số '{entity.BienKiemSoat}' (Số biên lai: {entity.SoBienLai}) đã ở trạng thái Đã hủy trước đó.");
        }

        // 1. Cập nhật lại trạng thái của biển số trong KhoBienSo về khả dụng (chưa xuất kho)
        var bienSo = await _dbContext.KhoBienSos
            .FirstOrDefaultAsync(x => x.Id == entity.KhoBienSoId || x.XuatKhoId == entity.Id, cancellationToken);

        if (bienSo != null)
        {
            var statusDuocBam = await _dbContext.TrangThaiBienSos
                .FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM" || x.Ten.ToLower().Contains("được bấm") || x.Ten.ToLower().Contains("bấm") || x.Ten.ToLower().Contains("tồn kho"), cancellationToken);

            bienSo.DaXuatKho = false;
            bienSo.NgayXuatKho = null;
            bienSo.XuatKhoId = null;
            bienSo.TrangThai = "DuocBam";
            bienSo.TrangThaiTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
            bienSo.TrangThaiBienSoId = statusDuocBam?.Id ?? bienSo.TrangThaiBienSoId;
            bienSo.Modified = DateTime.UtcNow;

            // Cập nhật lại dòng chi tiết đợt nhập (nếu có liên kết)
            var chiTiet = !string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId)
                ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken)
                : (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId)
                    ? await _dbContext.ChiTietDotNhapBienSos.FirstOrDefaultAsync(x => x.DotNhapKhoId == bienSo.DotNhapKhoId && x.BienKiemSoat.ToLower() == bienSo.BienKiemSoat.ToLower(), cancellationToken)
                    : null);

            if (chiTiet != null)
            {
                chiTiet.TrangThaiBienSoId = statusDuocBam?.Id ?? chiTiet.TrangThaiBienSoId;
                chiTiet.TrangThaiBienSoTen = statusDuocBam?.Ten ?? "Biển được bấm (Tồn kho)";
                chiTiet.MauSacHienThi = statusDuocBam?.MauSacHienThi ?? "success";
                chiTiet.Modified = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId))
            {
                var dotNhap = await _dbContext.DotNhapKhos
                    .FirstOrDefaultAsync(x => x.Id == bienSo.DotNhapKhoId, cancellationToken);
                if (dotNhap != null)
                {
                    var dotNhapKhoList = await _dbContext.KhoBienSos
                        .Where(x => x.DotNhapKhoId == dotNhap.Id)
                        .ToListAsync(cancellationToken);
                    dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho && x.Id != bienSo.Id);
                    dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => (!x.DaXuatKho || x.Id == bienSo.Id) && !x.LaBienDauGia);
                    dotNhap.Modified = DateTime.UtcNow;
                }
            }
        }

        // 2. Cập nhật trạng thái phiếu xuất kho sang 'DaHuy'
        entity.TrangThai = "DaHuy";
        entity.DaXuatKho = false;

        var lyDo = request?.LyDoHuy?.Trim();
        var noiDungHuy = !string.IsNullOrWhiteSpace(lyDo)
            ? $"[ĐÃ HỦY] Lý do: {lyDo} (Thời điểm: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss})"
            : $"[ĐÃ HỦY] (Thời điểm: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss})";

        entity.GhiChu = string.IsNullOrWhiteSpace(entity.GhiChu)
            ? noiDungHuy
            : $"{entity.GhiChu}\n{noiDungHuy}";

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "XuatKho",
            "Huy",
            entity.SoBienLai,
            $"Hủy phiếu xuất kho '{entity.KyHieuQuyen}-{entity.SoBienLai}' cho biển số {entity.BienKiemSoat}. Khôi phục biển số về kho khả dụng. Lý do: {lyDo ?? "N/A"}",
            entity.Id,
            entity.BienKiemSoat,
            entity.TrangThai,
            "Đã hủy",
            null,
            entity.CustomerId,
            entity.GhiChu,
            cancellationToken);

        _logger.LogInformation("Người dùng '{UserName}' đã hủy thành công phiếu xuất kho '{Id}' (Biển số: {BienKiemSoat}, Số biên lai: {SoBienLai}) và cập nhật lại trạng thái KhoBienSo về 'DuocBam'",
            _currentUserService.Username ?? "N/A", entity.Id, entity.BienKiemSoat, entity.SoBienLai);

        return MapToResponse(entity);
    }

    private static XuatKhoResponse MapToResponse(XuatKhoBienSo x)
    {
        var db = !string.IsNullOrWhiteSpace(x.DauBienSoTen) ? x.DauBienSoTen : (x.BienKiemSoat.Contains('-') ? x.BienKiemSoat.Split('-')[0] : "");
        var lx = x.LoaiXeTen ?? "Ô tô";
        var mb = x.MauBienSoTen ?? "Biển Trắng";
        var displayPlate = $"{x.BienKiemSoat} • {lx} ({mb} - Đầu {db})";

        return new XuatKhoResponse
        {
            Id = x.Id ?? string.Empty,
            CustomerId = x.CustomerId,
            LoaiThuTucId = x.LoaiThuTucId,
            LoaiThuTucTen = x.LoaiThuTucTen,
            NgayThucHien = x.NgayThucHien,
            TinhChatXuatKho = x.TinhChatXuatKho,
            KhoBienSoId = x.KhoBienSoId,
            BienKiemSoat = x.BienKiemSoat,
            LoaiXeId = x.LoaiXeId,
            LoaiXeTen = x.LoaiXeTen,
            MauBienSoId = x.MauBienSoId,
            MauBienSoTen = x.MauBienSoTen,
            DauBienSoId = x.DauBienSoId,
            DauBienSoTen = x.DauBienSoTen,
            ThongTinBienSoHienThi = displayPlate,

            MucThuLePhiId = x.MucThuLePhiId,
            MucThuLePhiTen = x.MucThuLePhiTen,
            KhuVucApDungId = x.KhuVucApDungId,
            KhuVucApDungTen = x.KhuVucApDungTen,
            MucThuLePhiKhuVucId = x.MucThuLePhiKhuVucId,
            DieuKienTriGiaXe = x.DieuKienTriGiaXe,
            CanCuPhapLy = x.CanCuPhapLy,
            SoTienTuDongDien = x.SoTienTuDongDien,

            KyHieuQuyenBienLaiId = x.KyHieuQuyenBienLaiId,
            KyHieuQuyen = x.KyHieuQuyen,
            SoBienLai = x.SoBienLai,
            NgayBienLai = x.NgayBienLai,
            SoTienThu = x.SoTienThu,
            HoTenNguoiNop = x.HoTenNguoiNop,
            SoCccd = x.SoCccd,
            MaHoSoDvc = x.MaHoSoDvc,
            GhiChu = x.GhiChu,
            TrangThai = x.TrangThai,
            DaXuatKho = x.DaXuatKho,
            Created = x.Created,
            CreatedByName = x.CreatedByName,
            Modified = x.Modified,
            ModifileByName = x.ModifileByName
        };
    }

    /// <summary>
    /// API 9: Nhập xuất kho biển số xe tự động từ tệp tin Excel lưu trong thư mục StorageFolder (appsettings.json).
    /// Tự động tạo các phiếu xuất kho với DaXuatKho = true và cập nhật bảng KhoBienSo.
    /// </summary>
    public async Task<ImportXuatKhoExcelResponse> ImportXuatKhoTuExcelAsync(
        ImportXuatKhoExcelRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Dữ liệu yêu cầu không được rỗng.");
        }

        if (string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            throw new ArgumentException("Vui lòng chọn Loại xe.", nameof(request.LoaiXeId));
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("Vui lòng chỉ định tên tệp tin Excel trong thư mục StorageFolder.", nameof(request.FileName));
        }

        // 1. Kiểm tra tồn tại của Loại xe
        var loaiXe = await _dbContext.LoaiXes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LoaiXeId || x.Ma == request.LoaiXeId, cancellationToken);

        if (loaiXe == null)
        {
            throw new ArgumentException($"Không tìm thấy Loại xe với Id: '{request.LoaiXeId}'.", nameof(request.LoaiXeId));
        }

        // 2. Xác định đường dẫn tệp tin trong thư mục StorageFolder
        var configuredFolder = string.IsNullOrWhiteSpace(_fileStorageSettings.StorageFolder) ? "Uploads" : _fileStorageSettings.StorageFolder;
        var destinationFolder = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        var safeFileName = Path.GetFileName(request.FileName);
        var filePath = Path.Combine(destinationFolder, safeFileName);

        if (!File.Exists(filePath))
        {
            if (File.Exists(request.FileName))
            {
                filePath = request.FileName;
            }
            else
            {
                var mauBaoCaoPath = Path.Combine(_environment.ContentRootPath, "MauBaoCao", safeFileName);
                if (File.Exists(mauBaoCaoPath))
                {
                    filePath = mauBaoCaoPath;
                }
                else
                {
                    var altPath = Path.Combine(_environment.ContentRootPath, request.FileName.TrimStart('/', '\\'));
                    if (File.Exists(altPath))
                    {
                        filePath = altPath;
                    }
                    else
                    {
                        throw new FileNotFoundException($"Không tìm thấy tệp tin '{safeFileName}' trong thư mục lưu trữ '{configuredFolder}'.", safeFileName);
                    }
                }
            }
        }

        // 3. Đọc dữ liệu bằng EPPlus
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(filePath));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws == null || ws.Dimension == null)
        {
            return new ImportXuatKhoExcelResponse { TongSoDong = 0, ThongBao = "Tệp Excel không chứa dữ liệu hoặc không có trang tính hợp lệ." };
        }

        // 4. Nhận diện dòng tiêu đề và vị trí các cột
        int headerRow = 1;
        int colStt = 1;
        int colMaHoSoDvc = 2;
        int colLoaiThuTuc = 3;
        int colTinhChat = 4;
        int colSoTienThu = 5;
        int colBienSo = 6;
        int colKhuVuc = 7;
        int colNhanHieu = 8;
        int colSoKhung = 9;
        int colSoMay = 10;
        int colGhiChu = 11;
        int colNgay = 12;
        int colKyHieuQuyen = 13;
        int colSoBienLai = 14;
        int colSoTienThu2 = 15;
        int colCanCuPhapLy = 16;
        int colHoTenNguoiNop = 17;
        int colSoCccd = 18;
        int colNgayThucHien = 19;
        int colTrangThai = 20;

        int maxScanRow = Math.Min(10, ws.Dimension.End.Row);
        bool foundDynamicHeader = false;

        for (int r = 1; r <= maxScanRow; r++)
        {
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
            {
                var text = ws.Cells[r, c].Text?.Trim().ToLowerInvariant();
                if (text != null && (text.Contains("biển") || text.Contains("bien") || text.Contains("bks") || text.Contains("biên lai") || text.Contains("thủ tục"))
                    && !text.Contains("danh sách") && !text.Contains("báo cáo") && !text.Contains("tiêu đề"))
                {
                    headerRow = r;
                    foundDynamicHeader = true;
                    break;
                }
            }
            if (foundDynamicHeader) break;
        }

        if (foundDynamicHeader)
        {
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
            {
                var text = ws.Cells[headerRow, c].Text?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (text == "stt" || text.Contains("thứ tự")) colStt = c;
                else if (text.Contains("hồ sơ") || text.Contains("dvc") || text.Contains("mã hs")) colMaHoSoDvc = c;
                else if (text.Contains("thủ tục") || text.Contains("thu tuc")) colLoaiThuTuc = c;
                else if (text.Contains("hình thức") || text.Contains("tính chất") || text.Contains("tinh chat")) colTinhChat = c;
                else if (text.Contains("khu vực") || text.Contains("khu vuc")) colKhuVuc = c;
                else if (text.Contains("nhãn hiệu") || text.Contains("hiệu xe")) colNhanHieu = c;
                else if (text.Contains("khung")) colSoKhung = c;
                else if (text.Contains("máy") || text.Contains("so may")) colSoMay = c;
                else if (text.Contains("căn cứ") || text.Contains("pháp lý")) colCanCuPhapLy = c;
                else if (text.Contains("người nộp") || text.Contains("họ tên") || text.Contains("chủ phương tiện")) colHoTenNguoiNop = c;
                else if (text.Contains("cccd") || text.Contains("cmnd") || text.Contains("định danh")) colSoCccd = c;
                else if (text.Contains("trạng thái") || text.Contains("trang thai")) colTrangThai = c;
                else if (text.Contains("quyển") || text.Contains("quyen")) colKyHieuQuyen = c;
                else if (text.Contains("số bl") || text.Contains("số biên lai") || text.Contains("so bl")) colSoBienLai = c;
                else if (text.Contains("thời gian") || text.Contains("ngày thực hiện") || text.Contains("ngày nộp") || text.Contains("ngày biên lai")) colNgay = c;
                else if (text.Contains("biển") || text.Contains("bien") || text.Contains("bks")) colBienSo = c;
                else if (text.Contains("tiền") || text.Contains("lệ phí") || text.Contains("mức thu"))
                {
                    if (colSoTienThu == 5) colSoTienThu = c;
                    else colSoTienThu2 = c;
                }
                else if (text.Contains("ghi chú")) colGhiChu = c;
            }
        }

        // 5. Nạp danh mục cần thiết để liên kết
        var customerId = _currentUserService.CustomerId;

        var statusDaBam = await _dbContext.TrangThaiBienSos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ma == "DA_BAM" || x.Ten.ToLower().Contains("đã bấm") || x.Ten.ToLower().Contains("đã cấp"), cancellationToken);

        var listLoaiThuTuc = await _dbContext.LoaiThuTucs.AsNoTracking().ToListAsync(cancellationToken);
        var listKhuVuc = await _dbContext.KhuVucApDungs.AsNoTracking().ToListAsync(cancellationToken);
        var listQuyen = await _dbContext.KyHieuQuyenBienLais.ToListAsync(cancellationToken);
        var listMucThu = await _dbContext.MucThuLePhis.AsNoTracking()
            .Where(x => x.LoaiXeId == null || x.LoaiXeId == loaiXe.Id)
            .ToListAsync(cancellationToken);
        var listDauBien = await _dbContext.DauBienSos.AsNoTracking().ToListAsync(cancellationToken);
        var defaultMauBien = await _dbContext.MauBienSos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ten.ToLower().Contains("trắng") || x.Ma == "TRANG", cancellationToken);

        var response = new ImportXuatKhoExcelResponse();
        var processedPlatesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectedDotNhapIds = new HashSet<string>();

        // 6. Quét từng dòng dữ liệu từ Excel
        for (int row = headerRow + 1; row <= ws.Dimension.End.Row; row++)
        {
            var rawBienSo = ws.Cells[row, colBienSo].Text?.Trim();
            if (string.IsNullOrWhiteSpace(rawBienSo))
            {
                continue;
            }

            response.TongSoDong++;

            // Chuẩn hóa biển kiểm soát
            var bienKiemSoat = rawBienSo.ToUpperInvariant().Trim();
            var normalizedPlate = bienKiemSoat.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpperInvariant();

            // Kiểm tra trùng lặp trong cùng tệp Excel
            if (!processedPlatesInFile.Add(normalizedPlate))
            {
                response.SoLuongBoQua++;
                response.DanhSachLoi.Add(new ImportXuatKhoLoiItem
                {
                    Dong = row,
                    BienKiemSoat = bienKiemSoat,
                    LyDoLoi = $"Biển số '{bienKiemSoat}' bị trùng lặp nhiều lần trong tệp Excel."
                });
                continue;
            }

            try
            {
                // Đọc các trường thông tin từ dòng
                var maHoSoDvc = colMaHoSoDvc > 0 ? ws.Cells[row, colMaHoSoDvc].Text?.Trim() : null;
                var loaiThuTucRaw = colLoaiThuTuc > 0 ? ws.Cells[row, colLoaiThuTuc].Text?.Trim() : null;
                var tinhChatRaw = colTinhChat > 0 ? ws.Cells[row, colTinhChat].Text?.Trim() : null;
                var khuVucRaw = colKhuVuc > 0 ? ws.Cells[row, colKhuVuc].Text?.Trim() : null;
                var nhanHieu = colNhanHieu > 0 ? ws.Cells[row, colNhanHieu].Text?.Trim() : null;
                var soKhung = colSoKhung > 0 ? ws.Cells[row, colSoKhung].Text?.Trim() : null;
                var soMay = colSoMay > 0 ? ws.Cells[row, colSoMay].Text?.Trim() : null;
                var rawGhiChu = colGhiChu > 0 ? ws.Cells[row, colGhiChu].Text?.Trim() : null;
                var kyHieuQuyen = colKyHieuQuyen > 0 ? ws.Cells[row, colKyHieuQuyen].Text?.Trim() : string.Empty;
                var soBienLai = colSoBienLai > 0 ? ws.Cells[row, colSoBienLai].Text?.Trim() : string.Empty;
                var canCuPhapLy = colCanCuPhapLy > 0 ? ws.Cells[row, colCanCuPhapLy].Text?.Trim() : null;
                var hoTenNguoiNop = colHoTenNguoiNop > 0 ? ws.Cells[row, colHoTenNguoiNop].Text?.Trim() : "Người nộp tiền";
                var soCccd = colSoCccd > 0 ? ws.Cells[row, colSoCccd].Text?.Trim() : null;

                // Đọc số tiền lệ phí
                decimal soTienThu = 0;
                var valMoney = colSoTienThu > 0 ? ws.Cells[row, colSoTienThu].Value : null;
                if (valMoney == null && colSoTienThu2 > 0) valMoney = ws.Cells[row, colSoTienThu2].Value;

                if (valMoney is double dVal) soTienThu = Convert.ToDecimal(dVal);
                else if (valMoney is decimal decVal) soTienThu = decVal;
                else if (valMoney is int iVal) soTienThu = iVal;
                else if (valMoney is long lVal) soTienThu = lVal;
                else
                {
                    var textMoney = (colSoTienThu > 0 ? ws.Cells[row, colSoTienThu].Text?.Trim() : null);
                    if (string.IsNullOrWhiteSpace(textMoney) && colSoTienThu2 > 0)
                        textMoney = ws.Cells[row, colSoTienThu2].Text?.Trim();

                    if (!string.IsNullOrWhiteSpace(textMoney))
                    {
                        var cleanMoney = textMoney.Replace(",", "").Replace(".", "").Replace("đ", "").Replace("VND", "").Trim();
                        decimal.TryParse(cleanMoney, out soTienThu);
                    }
                }

                // Đọc ngày thực hiện / ngày biên lai
                DateTime ngayThucHien = DateTime.UtcNow;
                var dateCellVal = colNgay > 0 ? ws.Cells[row, colNgay].Value : null;
                if (dateCellVal == null && colNgayThucHien > 0) dateCellVal = ws.Cells[row, colNgayThucHien].Value;

                if (dateCellVal is DateTime dt)
                {
                    ngayThucHien = dt;
                }
                else
                {
                    var dateText = colNgay > 0 ? ws.Cells[row, colNgay].Text?.Trim() : null;
                    if (string.IsNullOrWhiteSpace(dateText) && colNgayThucHien > 0) dateText = ws.Cells[row, colNgayThucHien].Text?.Trim();

                    if (!string.IsNullOrWhiteSpace(dateText) && DateTime.TryParse(dateText, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var parsedDt))
                    {
                        ngayThucHien = parsedDt;
                    }
                    else if (!string.IsNullOrWhiteSpace(dateText) && DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDtInv))
                    {
                        ngayThucHien = parsedDtInv;
                    }
                }

                // Ghép thông tin xe vào GhiChu
                var ghiChuParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(rawGhiChu)) ghiChuParts.Add(rawGhiChu);
                if (!string.IsNullOrWhiteSpace(nhanHieu)) ghiChuParts.Add($"Nhãn hiệu: {nhanHieu}");
                if (!string.IsNullOrWhiteSpace(soKhung)) ghiChuParts.Add($"Số khung: {soKhung}");
                if (!string.IsNullOrWhiteSpace(soMay)) ghiChuParts.Add($"Số máy: {soMay}");
                var ghiChu = ghiChuParts.Count > 0 ? string.Join("; ", ghiChuParts) : null;

                // 7. Kiểm tra biển số trong bảng KhoBienSo
                var bienSo = await _dbContext.KhoBienSos
                    .FirstOrDefaultAsync(x =>
                        (string.IsNullOrEmpty(customerId) || x.CustomerId == customerId || x.CustomerId == null) &&
                        (x.BienKiemSoat.ToUpper() == bienKiemSoat ||
                         x.BienKiemSoat.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpper() == normalizedPlate),
                        cancellationToken);

                // Kiểm tra xem biển số đã có trong kho hay chưa
                if (bienSo == null)
                {
                    // Biển số CHƯA CÓ TRONG KHO: Báo lỗi và ghi nhận vào danh sách biển số chưa có trong kho
                    response.SoLuongLoi++;
                    response.DanhSachBienSoChuaCoTrongKho.Add(bienKiemSoat);
                    response.DanhSachLoi.Add(new ImportXuatKhoLoiItem
                    {
                        Dong = row,
                        BienKiemSoat = bienKiemSoat,
                        LyDoLoi = $"Biển số '{bienKiemSoat}' chưa có trong kho (chưa được nhập kho)."
                    });
                    continue;
                }

                // Kiểm tra nếu biển số đã xuất kho trước đó
                if (bienSo.DaXuatKho)
                {
                    response.SoLuongBoQua++;
                    response.DanhSachLoi.Add(new ImportXuatKhoLoiItem
                    {
                        Dong = row,
                        BienKiemSoat = bienKiemSoat,
                        LyDoLoi = $"Biển số '{bienKiemSoat}' đã được xuất kho trước đó."
                    });
                    continue;
                }

                // Nếu là chế độ chỉ kiểm tra (dry-run): Ghi nhận thành công mà không cập nhật cơ sở dữ liệu
                if (request.ChiKiemTra == true)
                {
                    response.SoLuongThanhCong++;
                    response.TongTienThu += soTienThu;
                    response.DanhSachBienSoXuatThanhCong.Add(bienKiemSoat);
                    continue;
                }

                // Cập nhật biển số trong KhoBienSo sang đã xuất kho
                bienSo.DaXuatKho = true;
                bienSo.NgayXuatKho = ngayThucHien;
                bienSo.TrangThai = "DaBam";
                bienSo.TrangThaiTen = statusDaBam?.Ten ?? "Đã bấm (Đã cấp)";
                bienSo.TrangThaiBienSoId = statusDaBam?.Id ?? bienSo.TrangThaiBienSoId;
                bienSo.Modified = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(bienSo.DotNhapKhoId))
                {
                    affectedDotNhapIds.Add(bienSo.DotNhapKhoId);
                }

                // 8. Khớp nối các danh mục liên quan
                var loaiThuTuc = listLoaiThuTuc.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(loaiThuTucRaw) &&
                    (x.Ten.ToLower().Contains(loaiThuTucRaw.ToLower()) || loaiThuTucRaw.ToLower().Contains(x.Ten.ToLower())))
                    ?? listLoaiThuTuc.FirstOrDefault();

                var khuVuc = listKhuVuc.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(khuVucRaw) &&
                    (x.Ten.ToLower().Contains(khuVucRaw.ToLower()) || khuVucRaw.ToLower().Contains(x.Ten.ToLower())))
                    ?? listKhuVuc.FirstOrDefault();

                var quyen = listQuyen.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(kyHieuQuyen) &&
                    (x.Ma.Equals(kyHieuQuyen, StringComparison.OrdinalIgnoreCase) ||
                     x.Ten.Contains(kyHieuQuyen, StringComparison.OrdinalIgnoreCase)))
                    ?? listQuyen.FirstOrDefault();

                var mucThu = listMucThu.FirstOrDefault(x => loaiThuTuc != null && x.LoaiThuTucId == loaiThuTuc.Id)
                    ?? listMucThu.FirstOrDefault();

                var dauBien = listDauBien.FirstOrDefault(d => d.Id == bienSo.DauBienSoId);
                var mauBien = defaultMauBien;

                // 9. Tạo thực thể XuatKhoBienSo với DaXuatKho = true
                var xuatKho = new XuatKhoBienSo
                {
                    Id = Guid.NewGuid().ToString("D"),
                    CustomerId = customerId,
                    LoaiThuTucId = loaiThuTuc?.Id ?? string.Empty,
                    LoaiThuTucTen = loaiThuTuc?.Ten ?? loaiThuTucRaw ?? "Cấp biển (Có xuất kho số)",
                    NgayThucHien = ngayThucHien,
                    TinhChatXuatKho = !string.IsNullOrWhiteSpace(tinhChatRaw) ? tinhChatRaw : "Xuất 01 biển từ kho 'Biển được bấm'",
                    KhoBienSoId = bienSo.Id,
                    BienKiemSoat = bienKiemSoat,
                    LoaiXeId = loaiXe.Id,
                    LoaiXeTen = loaiXe.Ten ?? "Ô tô",
                    MauBienSoId = bienSo.MauBienSoId ?? defaultMauBien?.Id,
                    MauBienSoTen = mauBien?.Ten ?? "Biển Trắng",
                    DauBienSoId = bienSo.DauBienSoId,
                    DauBienSoTen = dauBien?.KyHieuSeries ?? dauBien?.Ten,

                    MucThuLePhiId = mucThu?.Id ?? string.Empty,
                    MucThuLePhiTen = mucThu?.Ten,
                    KhuVucApDungId = khuVuc?.Id ?? string.Empty,
                    KhuVucApDungTen = khuVuc?.Ten,
                    DieuKienTriGiaXe = null,
                    CanCuPhapLy = !string.IsNullOrWhiteSpace(canCuPhapLy) ? canCuPhapLy : (mucThu?.CanCuPhapLy ?? "Thông tư 155/2025/TT-BTC"),
                    SoTienTuDongDien = soTienThu,

                    KyHieuQuyenBienLaiId = quyen?.Id ?? string.Empty,
                    KyHieuQuyen = !string.IsNullOrWhiteSpace(kyHieuQuyen) ? kyHieuQuyen : (quyen?.Ma ?? string.Empty),
                    SoBienLai = !string.IsNullOrWhiteSpace(soBienLai) ? soBienLai : bienKiemSoat,
                    NgayBienLai = ngayThucHien,
                    SoTienThu = soTienThu,
                    HoTenNguoiNop = !string.IsNullOrWhiteSpace(hoTenNguoiNop) ? hoTenNguoiNop : "Người nộp tiền",
                    SoCccd = soCccd,
                    MaHoSoDvc = maHoSoDvc,
                    GhiChu = ghiChu,
                    DaXuatKho = true,
                    TrangThai = "HoanTat",
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };

                await _dbContext.XuatKhoBienSos.AddAsync(xuatKho, cancellationToken);
                bienSo.XuatKhoId = xuatKho.Id;

                // 10. Cập nhật dòng chi tiết đợt nhập (nếu có liên kết)
                if (!string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId))
                {
                    var chiTiet = await _dbContext.ChiTietDotNhapBienSos
                        .FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken);
                    if (chiTiet != null)
                    {
                        chiTiet.TrangThaiBienSoId = statusDaBam?.Id ?? chiTiet.TrangThaiBienSoId;
                        chiTiet.TrangThaiBienSoTen = statusDaBam?.Ten ?? "Đã bấm (Đã cấp)";
                        chiTiet.MauSacHienThi = statusDaBam?.MauSacHienThi ?? "secondary";
                        chiTiet.Modified = DateTime.UtcNow;
                    }
                }

                // 11. Cập nhật Số hiện tại của Quyển biên lai
                if (quyen != null && long.TryParse(soBienLai, out var soBL))
                {
                    if (!quyen.SoHienTai.HasValue || soBL > quyen.SoHienTai.Value)
                    {
                        quyen.SoHienTai = soBL;
                    }
                }

                response.SoLuongThanhCong++;
                response.TongTienThu += soTienThu;
                response.DanhSachBienSoXuatThanhCong.Add(bienKiemSoat);
            }
            catch (Exception ex)
            {
                response.SoLuongLoi++;
                response.DanhSachLoi.Add(new ImportXuatKhoLoiItem
                {
                    Dong = row,
                    BienKiemSoat = bienKiemSoat,
                    LyDoLoi = $"Lỗi xử lý: {ex.Message}"
                });
            }
        }

        // Tạo thông báo kết quả
        var thongBaoParts = new List<string>();
        if (response.DanhSachBienSoChuaCoTrongKho.Count > 0)
        {
            thongBaoParts.Add($"Cảnh báo: Có {response.DanhSachBienSoChuaCoTrongKho.Count} biển số chưa có trong kho: {string.Join(", ", response.DanhSachBienSoChuaCoTrongKho)}.");
        }

        if (request.ChiKiemTra == true)
        {
            if (response.SoLuongThanhCong > 0)
            {
                thongBaoParts.Add($"Kiểm tra thành công: Có {response.SoLuongThanhCong}/{response.TongSoDong} biển số hợp lệ và sẵn sàng xuất kho.");
            }
            if (response.SoLuongBoQua > 0)
            {
                thongBaoParts.Add($"Bỏ qua {response.SoLuongBoQua} dòng (trùng lặp hoặc đã xuất kho trước đó).");
            }
            if (response.SoLuongThanhCong == 0 && response.DanhSachBienSoChuaCoTrongKho.Count > 0)
            {
                thongBaoParts.Insert(0, "Tất cả các biển số trong tệp Excel đều chưa có trong kho.");
            }
            if (thongBaoParts.Count == 0)
            {
                thongBaoParts.Add("Không tìm thấy dữ liệu biển số hợp lệ trong tệp Excel.");
            }
            response.ThongBao = string.Join(" ", thongBaoParts);
            return response;
        }

        if (response.SoLuongThanhCong > 0)
        {
            thongBaoParts.Add($"Xuất kho thành công {response.SoLuongThanhCong}/{response.TongSoDong} biển số.");
        }
        if (response.SoLuongBoQua > 0)
        {
            thongBaoParts.Add($"Bỏ qua {response.SoLuongBoQua} dòng (trùng lặp hoặc đã xuất kho trước đó).");
        }
        if (response.SoLuongThanhCong == 0 && response.DanhSachBienSoChuaCoTrongKho.Count > 0)
        {
            thongBaoParts.Insert(0, "Không có biển số nào được xuất kho do các biển số trong tệp đều chưa có trong kho.");
        }
        response.ThongBao = string.Join(" ", thongBaoParts);

        if (response.SoLuongThanhCong > 0)
        {
            // 12. Lưu thay đổi vào cơ sở dữ liệu
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 13. Cập nhật lại số lượng của các đợt nhập kho bị ảnh hưởng (nếu có)
            foreach (var dotNhapId in affectedDotNhapIds)
            {
                var dotNhap = await _dbContext.DotNhapKhos.FirstOrDefaultAsync(x => x.Id == dotNhapId, cancellationToken);
                if (dotNhap != null)
                {
                    var dotNhapKhoList = await _dbContext.KhoBienSos
                        .Where(x => x.DotNhapKhoId == dotNhap.Id)
                        .ToListAsync(cancellationToken);

                    dotNhap.SoBienDaBam = dotNhapKhoList.Count(x => x.DaXuatKho);
                    dotNhap.SoBienDuocBam = dotNhapKhoList.Count(x => !x.DaXuatKho && !x.LaBienDauGia);
                    dotNhap.Modified = DateTime.UtcNow;
                }
            }

            if (affectedDotNhapIds.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // 14. Ghi vết vào Nhật ký hoạt động (Audit Log)
            try
            {
                await _logHoatDongService.GhiLogAsync(
                    loaiNghiepVu: "XuatKho",
                    hanhDong: "ThemMoi",
                    maChungTu: $"IMPORT_{DateTime.Now:yyyyMMdd_HHmmss}",
                    noiDung: $"Nhập xuất kho tự động từ tệp Excel '{safeFileName}': Thành công {response.SoLuongThanhCong}/{response.TongSoDong} biển số, Bỏ qua {response.SoLuongBoQua}, Lỗi {response.SoLuongLoi}. Chưa có trong kho: {response.DanhSachBienSoChuaCoTrongKho.Count}. Tổng lệ phí: {response.TongTienThu:N0}đ.",
                    chiTietThayDoi: $"LoaiXeId: {loaiXe.Id} ({loaiXe.Ten}), Tệp: {safeFileName}, Số biển xuất: {response.SoLuongThanhCong}",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi nhật ký hoạt động khi import xuất kho từ Excel: {Message}", ex.Message);
            }
        }

        return response;
    }

    /// <summary>
    /// API 10: (GET) Lấy thông tin tệp tin Excel mẫu xuất kho biển số xe (MauBaoCao/XuatKhoBienSo.xlsx).
    /// Output trả về các tham số filename và path (mã hóa Base64) là input cho API tải file /api/FileUpload/download.
    /// </summary>
    public async Task<ExportMauXuatKhoResponse> ExportFileMauXuatKhoAsync(CancellationToken cancellationToken = default)
    {
        var configuredFolder = string.IsNullOrWhiteSpace(_fileStorageSettings.StorageFolder) ? "Uploads" : _fileStorageSettings.StorageFolder;
        var storageDir = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        if (!Directory.Exists(storageDir))
        {
            Directory.CreateDirectory(storageDir);
        }

        var templateFileName = "XuatKhoBienSo.xlsx";
        var targetFilePath = Path.Combine(storageDir, templateFileName);
        var sourceTemplate = Path.Combine(_environment.ContentRootPath, "MauBaoCao", templateFileName);

        // 1. Đảm bảo file tồn tại ở MauBaoCao và sao chép sang StorageFolder
        if (File.Exists(sourceTemplate))
        {
            File.Copy(sourceTemplate, targetFilePath, overwrite: true);
        }
        else
        {
            var mauBaoCaoDir = Path.Combine(_environment.ContentRootPath, "MauBaoCao");
            if (!Directory.Exists(mauBaoCaoDir))
            {
                Directory.CreateDirectory(mauBaoCaoDir);
            }

            var excelBytes = GenerateMauXuatKhoExcelBytes();
            await File.WriteAllBytesAsync(sourceTemplate, excelBytes, cancellationToken);
            await File.WriteAllBytesAsync(targetFilePath, excelBytes, cancellationToken);
        }

        // 2. Mã hóa Base64 filename và path theo chuẩn chung của hệ thống (dùng cho /api/FileUpload/download)
        var encodedFileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(templateFileName));
        var encodedPath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(targetFilePath));
        var downloadUrl = $"/api/FileUpload/download?filename={Uri.EscapeDataString(encodedFileName)}&path={Uri.EscapeDataString(encodedPath)}";

        _logger.LogInformation("Export mẫu xuất kho: Trả về input cho download file: filename='{FileName}', path='{Path}'", encodedFileName, encodedPath);

        return new ExportMauXuatKhoResponse
        {
            FileName = encodedFileName,
            Path = encodedPath,
            DownloadUrl = downloadUrl,
            OriginalFileName = templateFileName
        };
    }

    /// <summary>
    /// Lấy dữ liệu file mẫu xuất kho để tải trực tiếp từ Controller (FileResult).
    /// </summary>
    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadFileMauXuatKhoAsync(CancellationToken cancellationToken = default)
    {
        var templateFileName = "XuatKhoBienSo.xlsx";
        var sourceTemplate = Path.Combine(_environment.ContentRootPath, "MauBaoCao", templateFileName);

        byte[] bytes;
        if (File.Exists(sourceTemplate))
        {
            bytes = await File.ReadAllBytesAsync(sourceTemplate, cancellationToken);
        }
        else
        {
            bytes = GenerateMauXuatKhoExcelBytes();
            var mauBaoCaoDir = Path.Combine(_environment.ContentRootPath, "MauBaoCao");
            if (!Directory.Exists(mauBaoCaoDir))
            {
                Directory.CreateDirectory(mauBaoCaoDir);
            }
            await File.WriteAllBytesAsync(sourceTemplate, bytes, cancellationToken);
        }

        return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", templateFileName);
    }

    /// <summary>
    /// Tạo cấu trúc và dữ liệu mẫu tệp Excel xuất kho biển số xe theo chuẩn EPPlus.
    /// </summary>
    private static byte[] GenerateMauXuatKhoExcelBytes()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("XuatKho");

        // 1. Tiêu đề
        ws.Cells[1, 1].Value = "DANH SÁCH BIỂN SỐ XE XUẤT KHO";
        ws.Cells[1, 1, 1, 17].Merge = true;
        ws.Cells[1, 1].Style.Font.Bold = true;
        ws.Cells[1, 1].Style.Font.Size = 16;
        ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        ws.Cells[1, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
        ws.Row(1).Height = 35;

        // 2. Phụ đề
        ws.Cells[2, 1].Value = "(Áp dụng cho tệp mẫu nhập xuất kho biển số xe và biên lai thu phí)";
        ws.Cells[2, 1, 2, 17].Merge = true;
        ws.Cells[2, 1].Style.Font.Italic = true;
        ws.Cells[2, 1].Style.Font.Size = 10;
        ws.Cells[2, 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
        ws.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        ws.Row(2).Height = 20;

        // 3. Dòng Header (Dòng 3)
        var headers = new[]
        {
            "STT", "Mã hồ sơ DVC", "Loại thủ tục", "Tính chất", "Lệ phí (VNĐ)",
            "Biển kiểm soát", "Khu vực", "Nhãn hiệu", "Số khung", "Số máy",
            "Ký hiệu quyển", "Số biên lai", "Họ tên người nộp", "Số CCCD",
            "Ngày thực hiện", "Căn cứ pháp lý", "Ghi chú"
        };

        ws.Row(3).Height = 28;
        for (int c = 1; c <= headers.Length; c++)
        {
            var cell = ws.Cells[3, c];
            cell.Value = headers[c - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.Size = 11;
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 230, 242));
            cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
        }

        // 4. Dữ liệu mẫu (Rows 4, 5, 6)
        var sampleRows = new List<object[]>
        {
            new object[] { 1, "DVC-2026-0001", "Cấp biển (Có xuất kho số)", "Xuất 01 biển từ kho 'Biển được bấm'", 150000, "30A-999.99", "Khu vực I", "Toyota", "RL123456789", "SM987654321", "Q01", "0000001", "Nguyễn Văn A", "001234567890", "19/10/2026", "Thông tư 155/2025/TT-BTC", "Xuất kho theo thủ tục cấp biển" },
            new object[] { 2, "DVC-2026-0002", "Cấp biển (Có xuất kho số)", "Xuất 01 biển từ kho 'Biển được bấm'", 150000, "30A-888.88", "Khu vực I", "Honda", "RL987654321", "SM123456789", "Q01", "0000002", "Trần Thị B", "001098765432", "19/10/2026", "Thông tư 155/2025/TT-BTC", "Xuất kho ô tô con" },
            new object[] { 3, "DVC-2026-0003", "Cấp lại biển số", "Xuất 01 biển từ kho 'Biển được bấm'", 100000, "29B-123.45", "Khu vực I", "Hyundai", "RL555666777", "SM888999000", "Q01", "0000003", "Lê Văn C", "001087654321", "19/10/2026", "Thông tư 155/2025/TT-BTC", "Cấp lại biển số hỏng" }
        };

        for (int i = 0; i < sampleRows.Count; i++)
        {
            int rowIdx = 4 + i;
            ws.Row(rowIdx).Height = 22;
            var data = sampleRows[i];
            for (int c = 1; c <= data.Length; c++)
            {
                var cell = ws.Cells[rowIdx, c];
                cell.Value = data[c - 1];
                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                if (c == 1) cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                else if (c == 5)
                {
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    cell.Style.Numberformat.Format = "#,##0";
                }
                else if (c == 6)
                {
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    cell.Style.Font.Bold = true;
                }
                else if (c == 11 || c == 12 || c == 14 || c == 15)
                {
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
            }
        }

        // Độ rộng cột
        int[] colWidths = { 8, 18, 28, 32, 16, 18, 16, 16, 20, 18, 16, 16, 22, 18, 18, 26, 30 };
        for (int c = 1; c <= colWidths.Length; c++)
        {
            ws.Column(c).Width = colWidths[c - 1];
        }

        return package.GetAsByteArray();
    }
}


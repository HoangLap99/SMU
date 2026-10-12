using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

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

    public XuatKhoService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<XuatKhoService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
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
            .FirstOrDefaultAsync(x => x.Id == mucThuLePhiId, cancellationToken);

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

        // 2. Kiểm tra Quyển biên lai (KyHieuQuyenBienLai)
        var quyen = await _dbContext.KyHieuQuyenBienLais
            .FirstOrDefaultAsync(x => x.Id == request.KyHieuQuyenBienLaiId, cancellationToken);

        if (quyen == null)
        {
            throw new ArgumentException($"Không tìm thấy Ký hiệu quyển biên lai với Id '{request.KyHieuQuyenBienLaiId}'.");
        }

        // 3. Kiểm tra chặn trùng số biên lai trong cùng một quyển
        var checkTrungSo = await _dbContext.XuatKhoBienSos
            .AnyAsync(x => x.KyHieuQuyenBienLaiId == request.KyHieuQuyenBienLaiId 
                        && x.SoBienLai == request.SoBienLai.Trim() 
                        && x.Id != request.Id, cancellationToken);

        if (checkTrungSo)
        {
            throw new InvalidOperationException($"Số biên lai '{request.SoBienLai}' trong quyển '{quyen.Ma}' đã được sử dụng. Vui lòng kiểm tra lại.");
        }

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
                    oldBienSo.DaXuatKho = false;
                    oldBienSo.NgayXuatKho = null;
                    oldBienSo.XuatKhoId = null;
                    oldBienSo.TrangThai = "DuocBam";
                    oldBienSo.TrangThaiTen = "Biển được bấm (Tồn kho)";

                    if (!string.IsNullOrWhiteSpace(oldBienSo.ChiTietDotNhapBienSoId))
                    {
                        var oldChiTiet = await _dbContext.ChiTietDotNhapBienSos
                            .FirstOrDefaultAsync(x => x.Id == oldBienSo.ChiTietDotNhapBienSoId, cancellationToken);
                        if (oldChiTiet != null)
                        {
                            oldChiTiet.TrangThaiBienSoTen = "Biển được bấm (Tồn kho)";
                            oldChiTiet.MauSacHienThi = "success";
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
        }

        // 5. NẾU DaXuatKho == true: THAY ĐỔI DỮ LIỆU CỦA KhoBienSo
        if (request.DaXuatKho)
        {
            bienSo.DaXuatKho = true;
            bienSo.NgayXuatKho = request.NgayThucHien;
            bienSo.XuatKhoId = xuatKho.Id;
            bienSo.TrangThai = "DaBam";
            bienSo.TrangThaiTen = "Đã bấm (Đã cấp)";

            // 6. Cập nhật dòng chi tiết đợt nhập (nếu có liên kết)
            if (!string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId))
            {
                var chiTiet = await _dbContext.ChiTietDotNhapBienSos
                    .FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken);
                if (chiTiet != null)
                {
                    chiTiet.TrangThaiBienSoTen = "Đã bấm (Đã cấp)";
                    chiTiet.MauSacHienThi = "secondary";
                }
            }

            // 7. Cập nhật Số hiện tại của Quyển biên lai
            if (long.TryParse(request.SoBienLai.Trim(), out var soMoi))
            {
                if (!quyen.SoHienTai.HasValue || soMoi > quyen.SoHienTai.Value)
                {
                    quyen.SoHienTai = soMoi;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã lưu thành công phiếu xuất kho '{Id}' cho biển số '{BienKiemSoat}', số biên lai '{SoBienLai}', DaXuatKho = {DaXuatKho}",
            xuatKho.Id, xuatKho.BienKiemSoat, xuatKho.SoBienLai, xuatKho.DaXuatKho);

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
                bienSo.DaXuatKho = false;
                bienSo.NgayXuatKho = null;
                bienSo.XuatKhoId = null;
                bienSo.TrangThai = "DuocBam";
                bienSo.TrangThaiTen = "Biển được bấm (Tồn kho)";

                if (!string.IsNullOrWhiteSpace(bienSo.ChiTietDotNhapBienSoId))
                {
                    var chiTiet = await _dbContext.ChiTietDotNhapBienSos
                        .FirstOrDefaultAsync(x => x.Id == bienSo.ChiTietDotNhapBienSoId, cancellationToken);
                    if (chiTiet != null)
                    {
                        chiTiet.TrangThaiBienSoTen = "Biển được bấm (Tồn kho)";
                        chiTiet.MauSacHienThi = "success";
                    }
                }
            }
        }

        _dbContext.XuatKhoBienSos.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã xóa phiếu xuất kho '{Id}' (DaXuatKho = {DaXuatKho}) và hoàn tác biển số '{BienKiemSoat}'",
            entity.Id, entity.DaXuatKho, entity.BienKiemSoat);

        return true;
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
}

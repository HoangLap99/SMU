using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai các nghiệp vụ Lập biên lai cấp lại đăng ký xe:
/// 1. Thêm mới / Cập nhật biên lai (Hỗ trợ Lưu nháp và Xác nhận phát hành)
/// 2. Xác nhận hoàn tất biên lai (Khóa phiếu, không cho sửa hay xóa nữa)
/// 3. Tuyệt đối không thay đổi thông tin của KhoBienSo (không xuất kho, không giảm tồn)
/// 4. Quản lý danh sách, chi tiết, xóa bản nháp và lấy số biên lai tiếp theo
/// </summary>
public class CapLaiDangKyXeService : ICapLaiDangKyXeService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CapLaiDangKyXeService> _logger;
    private readonly ILogHoatDongService _logHoatDongService;

    public CapLaiDangKyXeService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CapLaiDangKyXeService> logger,
        ILogHoatDongService logHoatDongService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _logHoatDongService = logHoatDongService;
    }

    /// <summary>
    /// Thêm mới hoặc Cập nhật biên lai cấp lại đăng ký xe.
    /// Hỗ trợ cả Lưu nháp (XacNhanHoanTat = false) và Xác nhận phát hành ngay (XacNhanHoanTat = true).
    /// Khi đã xác nhận, phiếu sẽ bị KHÓA hoàn toàn.
    /// Không làm thay đổi bất kỳ trạng thái nào trong KhoBienSo.
    /// </summary>
    public async Task<CapLaiDangKyXeResponse> SetCapLaiDangKyXeAsync(
        SetCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var customerId = request.CustomerId ?? _currentUserService.CustomerId;
        bool isAdd = string.IsNullOrWhiteSpace(request.Id);

        // 1. Kiểm tra nếu là cập nhật: Bản ghi phải tồn tại và KHÔNG được ở trạng thái đã xác nhận
        CapLaiDangKyXe? existing = null;
        if (!isAdd)
        {
            existing = await _dbContext.CapLaiDangKyXes
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (existing == null)
            {
                throw new ArgumentException($"Không tìm thấy biên lai cấp lại đăng ký xe với Id '{request.Id}'.");
            }

            if (existing.DaXacNhan || existing.TrangThai == "DaXacNhan")
            {
                throw new InvalidOperationException("Biên lai cấp lại đăng ký xe đã được xác nhận (phát hành), không được phép chỉnh sửa nữa.");
            }
        }

        // 2. Kiểm tra Ký hiệu quyển biên lai (KyHieuQuyenBienLai)
        var quyen = await _dbContext.KyHieuQuyenBienLais
            .FirstOrDefaultAsync(x => x.Id == request.KyHieuQuyenBienLaiId, cancellationToken);

        if (quyen == null)
        {
            throw new ArgumentException($"Không tìm thấy Ký hiệu quyển biên lai với Id '{request.KyHieuQuyenBienLaiId}'.");
        }

        // 3. Kiểm tra chặn trùng số biên lai trong cùng một quyển (kiểm tra cả trong bảng CapLaiDangKyXe và XuatKhoBienSo)
        var soBienLaiTrim = request.SoBienLai.Trim();
        var checkTrungCapLai = await _dbContext.CapLaiDangKyXes
            .AnyAsync(x => x.KyHieuQuyenBienLaiId == request.KyHieuQuyenBienLaiId 
                        && x.SoBienLai == soBienLaiTrim 
                        && x.Id != request.Id, cancellationToken);

        if (checkTrungCapLai)
        {
            throw new InvalidOperationException($"Số biên lai '{request.SoBienLai}' trong quyển '{quyen.Ma}' đã được sử dụng trong hệ thống cấp lại đăng ký xe.");
        }

        var checkTrungXuatKho = await _dbContext.XuatKhoBienSos
            .AnyAsync(x => x.KyHieuQuyenBienLaiId == request.KyHieuQuyenBienLaiId 
                        && x.SoBienLai == soBienLaiTrim, cancellationToken);

        if (checkTrungXuatKho)
        {
            throw new InvalidOperationException($"Số biên lai '{request.SoBienLai}' trong quyển '{quyen.Ma}' đã được sử dụng trong phiếu xuất kho biển số.");
        }

        // 4. Lấy thông tin snapshot loại xe nếu có
        string loaiPhuongTien = request.LoaiPhuongTien.Trim();
        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            var loaiXe = await _dbContext.LoaiXes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.LoaiXeId, cancellationToken);
            if (loaiXe != null && string.IsNullOrWhiteSpace(request.LoaiPhuongTien))
            {
                loaiPhuongTien = loaiXe.Ten;
            }
        }

        bool isConfirm = request.XacNhanHoanTat == true;
        var currentUserName = _currentUserService.FullName ?? _currentUserService.Username;

        CapLaiDangKyXe entity;

        if (isAdd)
        {
            entity = new CapLaiDangKyXe
            {
                CustomerId = customerId,
                KyHieuQuyenBienLaiId = request.KyHieuQuyenBienLaiId,
                KyHieuQuyen = quyen.Ma ?? string.Empty,
                SoBienLai = soBienLaiTrim,
                NgayBienLai = request.NgayBienLai,

                BienKiemSoat = request.BienKiemSoat.Trim().ToUpperInvariant(),
                LoaiXeId = request.LoaiXeId,
                LoaiPhuongTien = loaiPhuongTien,

                MucThuLePhiId = request.MucThuLePhiId,
                NoiDungThuPhi = request.NoiDungThuPhi.Trim(),
                SoTienThu = request.SoTienThu,
                HinhThucThuTien = !string.IsNullOrWhiteSpace(request.HinhThucThuTien) ? request.HinhThucThuTien.Trim() : "Tiền mặt",

                HoTenChuXe = request.HoTenChuXe.Trim(),
                SoCccd = request.SoCccd?.Trim(),
                MaHoSoDvc = request.MaHoSoDvc?.Trim(),
                LyDoCapLai = request.LyDoCapLai?.Trim(),

                TrangThai = isConfirm ? "DaXacNhan" : "LuuNhap",
                DaXacNhan = isConfirm,
                NgayXacNhan = isConfirm ? DateTime.UtcNow : null,
                NguoiXacNhan = isConfirm ? currentUserName : null
            };

            await _dbContext.CapLaiDangKyXes.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity = existing!;
            entity.KyHieuQuyenBienLaiId = request.KyHieuQuyenBienLaiId;
            entity.KyHieuQuyen = quyen.Ma ?? entity.KyHieuQuyen;
            entity.SoBienLai = soBienLaiTrim;
            entity.NgayBienLai = request.NgayBienLai;

            entity.BienKiemSoat = request.BienKiemSoat.Trim().ToUpperInvariant();
            entity.LoaiXeId = request.LoaiXeId;
            entity.LoaiPhuongTien = loaiPhuongTien;

            entity.MucThuLePhiId = request.MucThuLePhiId;
            entity.NoiDungThuPhi = request.NoiDungThuPhi.Trim();
            entity.SoTienThu = request.SoTienThu;
            entity.HinhThucThuTien = !string.IsNullOrWhiteSpace(request.HinhThucThuTien) ? request.HinhThucThuTien.Trim() : entity.HinhThucThuTien;

            entity.HoTenChuXe = request.HoTenChuXe.Trim();
            entity.SoCccd = request.SoCccd?.Trim();
            entity.MaHoSoDvc = request.MaHoSoDvc?.Trim();
            entity.LyDoCapLai = request.LyDoCapLai?.Trim();

            if (isConfirm)
            {
                entity.TrangThai = "DaXacNhan";
                entity.DaXacNhan = true;
                entity.NgayXacNhan = DateTime.UtcNow;
                entity.NguoiXacNhan = currentUserName;
            }
        }

        // 5. Cập nhật Số hiện tại của Quyển biên lai nếu số mới lớn hơn
        if (long.TryParse(soBienLaiTrim, out var soMoi))
        {
            if (!quyen.SoHienTai.HasValue || soMoi > quyen.SoHienTai.Value)
            {
                quyen.SoHienTai = soMoi;
            }
        }

        // LƯU Ý QUAN TRỌNG: TUYỆT ĐỐI KHÔNG THAY ĐỔI DỮ LIỆU BẢNG KhoBienSo
        // (Không xuất kho, không giảm tồn kho, không đổi trạng thái của biển số)

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "CapLaiDangKy",
            isAdd ? (entity.DaXacNhan ? "XacNhan" : "ThemMoi") : "CapNhat",
            entity.SoBienLai,
            isAdd
                ? $"Lập biên lai cấp lại đăng ký xe '{entity.KyHieuQuyen}-{entity.SoBienLai}' cho biển {entity.BienKiemSoat} ({entity.HoTenChuXe}), thu {entity.SoTienThu:N0}đ."
                : $"Cập nhật biên lai cấp lại đăng ký xe '{entity.KyHieuQuyen}-{entity.SoBienLai}' cho biển {entity.BienKiemSoat}.",
            entity.Id,
            entity.BienKiemSoat,
            entity.TrangThai,
            entity.DaXacNhan ? "Đã xác nhận (Đã phát hành)" : "Bản nháp",
            null,
            entity.CustomerId,
            entity.LyDoCapLai,
            cancellationToken);

        _logger.LogInformation("Đã lưu biên lai cấp lại đăng ký xe '{Id}' cho biển số '{BienKiemSoat}', số biên lai '{SoBienLai}', trạng thái '{TrangThai}'",
            entity.Id, entity.BienKiemSoat, entity.SoBienLai, entity.TrangThai);

        return MapToResponse(entity);
    }

    /// <summary>
    /// Xác nhận hoàn tất biên lai cấp lại đăng ký xe (chuyển từ Lưu nháp sang Đã xác nhận / Khóa phiếu).
    /// Sau khi xác nhận, biên lai sẽ KHÔNG cho phép sửa hay xóa nữa.
    /// </summary>
    public async Task<CapLaiDangKyXeResponse> XacNhanAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id biên lai không được để trống.", nameof(id));
        }

        var entity = await _dbContext.CapLaiDangKyXes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy biên lai cấp lại đăng ký xe với Id '{id}'.");
        }

        if (entity.DaXacNhan || entity.TrangThai == "DaXacNhan")
        {
            throw new InvalidOperationException("Biên lai cấp lại đăng ký xe này đã được xác nhận trước đó.");
        }

        entity.TrangThai = "DaXacNhan";
        entity.DaXacNhan = true;
        entity.NgayXacNhan = DateTime.UtcNow;
        entity.NguoiXacNhan = _currentUserService.FullName ?? _currentUserService.Username;

        // Cập nhật quyển biên lai nếu cần
        if (long.TryParse(entity.SoBienLai.Trim(), out var soMoi))
        {
            var quyen = await _dbContext.KyHieuQuyenBienLais
                .FirstOrDefaultAsync(x => x.Id == entity.KyHieuQuyenBienLaiId, cancellationToken);
            if (quyen != null && (!quyen.SoHienTai.HasValue || soMoi > quyen.SoHienTai.Value))
            {
                quyen.SoHienTai = soMoi;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "CapLaiDangKy",
            "XacNhan",
            entity.SoBienLai,
            $"Xác nhận phát hành biên lai cấp lại đăng ký xe '{entity.KyHieuQuyen}-{entity.SoBienLai}' cho biển {entity.BienKiemSoat} ({entity.HoTenChuXe}). Khóa biên lai hoàn tất.",
            entity.Id,
            entity.BienKiemSoat,
            entity.TrangThai,
            "Đã xác nhận (Đã phát hành)",
            null,
            entity.CustomerId,
            entity.LyDoCapLai,
            cancellationToken);

        _logger.LogInformation("Người dùng '{User}' đã xác nhận hoàn tất biên lai cấp lại đăng ký xe '{Id}' (Biển số: {BienKiemSoat})",
            entity.NguoiXacNhan, entity.Id, entity.BienKiemSoat);

        return MapToResponse(entity);
    }

    /// <summary>
    /// Lấy danh sách biên lai cấp lại đăng ký xe có phân trang và bộ lọc tìm kiếm.
    /// </summary>
    public async Task<PagedResult<CapLaiDangKyXeResponse>> LayDanhSachAsync(
        PagedCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken = default)
    {
        request ??= new PagedCapLaiDangKyXeRequest();
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.CapLaiDangKyXes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(x => x.BienKiemSoat.ToLower().Contains(kw)
                                  || x.SoBienLai.ToLower().Contains(kw)
                                  || x.HoTenChuXe.ToLower().Contains(kw)
                                  || (x.SoCccd != null && x.SoCccd.ToLower().Contains(kw))
                                  || (x.MaHoSoDvc != null && x.MaHoSoDvc.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            var tt = request.TrangThai.Trim();
            query = query.Where(x => x.TrangThai == tt);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiPhuongTien))
        {
            query = query.Where(x => x.LoaiPhuongTien == request.LoaiPhuongTien);
        }

        if (!string.IsNullOrWhiteSpace(request.KyHieuQuyenBienLaiId))
        {
            query = query.Where(x => x.KyHieuQuyenBienLaiId == request.KyHieuQuyenBienLaiId);
        }

        if (request.TuNgay.HasValue)
        {
            query = query.Where(x => x.NgayBienLai >= request.TuNgay.Value);
        }

        if (request.DenNgay.HasValue)
        {
            query = query.Where(x => x.NgayBienLai <= request.DenNgay.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var currentPage = request.CurrentPage > 0 ? request.CurrentPage : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var items = await query
            .OrderByDescending(x => x.NgayBienLai)
            .ThenByDescending(x => x.Created)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CapLaiDangKyXeResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Lấy chi tiết một biên lai cấp lại đăng ký xe theo Id.
    /// </summary>
    public async Task<CapLaiDangKyXeResponse?> LayChiTietAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var entity = await _dbContext.CapLaiDangKyXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : MapToResponse(entity);
    }

    /// <summary>
    /// Xóa một biên lai cấp lại đăng ký xe.
    /// Chỉ cho phép xóa khi đang ở trạng thái Lưu nháp. Nếu đã Xác nhận (phát hành), chặn tuyệt đối không cho xóa.
    /// </summary>
    public async Task<bool> XoaAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id biên lai không được để trống.", nameof(id));
        }

        var entity = await _dbContext.CapLaiDangKyXes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy biên lai cấp lại đăng ký xe với Id '{id}'.");
        }

        if (entity.DaXacNhan || entity.TrangThai == "DaXacNhan")
        {
            throw new InvalidOperationException("Biên lai cấp lại đăng ký xe đã được xác nhận (phát hành), không được phép xóa.");
        }

        _dbContext.CapLaiDangKyXes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "CapLaiDangKy",
            "Xoa",
            entity.SoBienLai,
            $"Xóa bản nháp biên lai cấp lại đăng ký xe '{entity.KyHieuQuyen}-{entity.SoBienLai}' cho biển {entity.BienKiemSoat}.",
            entity.Id,
            entity.BienKiemSoat,
            "DaXoa",
            "Đã xóa",
            null,
            entity.CustomerId,
            entity.LyDoCapLai,
            cancellationToken);

        _logger.LogInformation("Đã xóa bản nháp biên lai cấp lại đăng ký xe '{Id}'", id);
        return true;
    }

    /// <summary>
    /// Lấy thông tin quyển biên lai và gợi ý số biên lai tiếp theo theo KyHieuQuyenBienLaiId.
    /// </summary>
    public async Task<SoBienLaiTiepTheoResponse?> LaySoBienLaiTiepTheoAsync(
        string kyHieuQuyenBienLaiId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(kyHieuQuyenBienLaiId)) return null;

        var quyen = await _dbContext.KyHieuQuyenBienLais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == kyHieuQuyenBienLaiId, cancellationToken);

        if (quyen == null) return null;

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

    private static CapLaiDangKyXeResponse MapToResponse(CapLaiDangKyXe entity)
    {
        return new CapLaiDangKyXeResponse
        {
            Id = entity.Id ?? string.Empty,
            CustomerId = entity.CustomerId,

            KyHieuQuyenBienLaiId = entity.KyHieuQuyenBienLaiId,
            KyHieuQuyen = entity.KyHieuQuyen,
            SoBienLai = entity.SoBienLai,
            NgayBienLai = entity.NgayBienLai,

            BienKiemSoat = entity.BienKiemSoat,
            LoaiXeId = entity.LoaiXeId,
            LoaiPhuongTien = entity.LoaiPhuongTien,

            MucThuLePhiId = entity.MucThuLePhiId,
            NoiDungThuPhi = entity.NoiDungThuPhi,
            SoTienThu = entity.SoTienThu,
            HinhThucThuTien = entity.HinhThucThuTien,

            HoTenChuXe = entity.HoTenChuXe,
            SoCccd = entity.SoCccd,
            MaHoSoDvc = entity.MaHoSoDvc,
            LyDoCapLai = entity.LyDoCapLai,

            TrangThai = entity.TrangThai,
            DaXacNhan = entity.DaXacNhan,
            NgayXacNhan = entity.NgayXacNhan,
            NguoiXacNhan = entity.NguoiXacNhan,

            Created = entity.Created,
            CreatedById = entity.CreatedById,
            CreatedByName = entity.CreatedByName,
            Modified = entity.Modified,
            ModifileById = entity.ModifileById,
            ModifileByName = entity.ModifileByName
        };
    }
}

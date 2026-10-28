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
using System.Globalization;
using System.Text.RegularExpressions;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai các nghiệp vụ Lập biên lai cấp lại đăng ký xe:
/// 1. Thêm mới / Cập nhật biên lai (Hỗ trợ Lưu nháp và Xác nhận phát hành)
/// 2. Xác nhận hoàn tất biên lai (Khóa phiếu, không cho sửa hay xóa nữa)
/// 3. Tuyệt đối không thay đổi thông tin của KhoBienSo (không xuất kho, không giảm tồn)
/// 4. Quản lý danh sách, chi tiết, xóa bản nháp và lấy số biên lai tiếp theo
/// 5. Import dữ liệu cấp lại đăng ký xe từ tệp Excel MauBaoCao/CapLaiBienDangKy.xlsx
/// </summary>
public class CapLaiDangKyXeService : ICapLaiDangKyXeService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CapLaiDangKyXeService> _logger;
    private readonly ILogHoatDongService _logHoatDongService;
    private readonly FileStorageSettings _fileStorageSettings;
    private readonly IWebHostEnvironment _environment;

    public CapLaiDangKyXeService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CapLaiDangKyXeService> logger,
        ILogHoatDongService logHoatDongService,
        IOptions<FileStorageSettings> fileStorageOptions,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _logHoatDongService = logHoatDongService;
        _fileStorageSettings = fileStorageOptions?.Value ?? new FileStorageSettings();
        _environment = environment;
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
            quyen = new KyHieuQuyenBienLai();
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
                SoBienLai = request.SoBienLai,
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
            entity.SoBienLai = request.SoBienLai;
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



    /// <summary>
    /// API 10: (GET) Lấy thông tin tệp tin Excel mẫu xuất kho biển số xe (MauBaoCao/XuatKhoBienSo.xlsx).
    /// Output trả về các tham số filename và path (mã hóa Base64) là input cho API tải file /api/FileUpload/download.
    /// </summary>
    public async Task<ExportMauXuatKhoResponse> ExportFileMauCapLaiDangKyAsync(CancellationToken cancellationToken = default)
    {
        var configuredFolder = string.IsNullOrWhiteSpace(_fileStorageSettings.StorageFolder) ? "Uploads" : _fileStorageSettings.StorageFolder;
        var storageDir = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        if (!Directory.Exists(storageDir))
        {
            Directory.CreateDirectory(storageDir);
        }

        var templateFileName = "CapLaiBienDangKy.xlsx";
        var targetFilePath = Path.Combine(storageDir, templateFileName);
        var sourceTemplate = Path.Combine(_environment.ContentRootPath, "MauBaoCao", templateFileName);

        // 1. Đảm bảo file tồn tại ở MauBaoCao và sao chép sang StorageFolder
        if (File.Exists(sourceTemplate))
        {
            File.Copy(sourceTemplate, targetFilePath, overwrite: true);
        }
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        if (File.Exists(targetFilePath))
        {
            using var package = new ExcelPackage(new FileInfo(targetFilePath));
            var sheet1 = package.Workbook.Worksheets["Danh mục"];
            if (sheet1 != null)
            {
                int nRow = 3;
                var mucThuLePhi = await _dbContext.MucThuLePhis.AsNoTracking().ToListAsync(cancellationToken);
                foreach (var item in mucThuLePhi)
                {
                    sheet1.Cells[nRow, 1].Value = item.Ten;
                    nRow++;
                }

                var kyHieuQuyenBienLais = await _dbContext.KyHieuQuyenBienLais.AsNoTracking().ToListAsync(cancellationToken);
                foreach (var item in kyHieuQuyenBienLais)
                {
                    sheet1.Cells[nRow, 2].Value = item.Ten ?? item.Ma;
                    nRow++;
                }

              
                await package.SaveAsync(cancellationToken);
            }
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
    /// Import danh sách biên lai cấp lại đăng ký xe từ tệp Excel (mẫu MauBaoCao/CapLaiBienDangKy.xlsx).
    /// Quy tắc nghiệp vụ bắt buộc:
    /// 1. Biển số không lấy trong kho (không đối soát và không phụ thuộc bảng KhoBienSo).
    /// 2. Tuyệt đối không thay đổi dữ liệu trong kho (không xuất kho, không giảm tồn kho).
    /// 3. Mặc định DaXacNhan = true (tự động khóa biên lai, phát hành ngay).
    /// </summary>
    public async Task<ImportCapLaiDangKyXeResponse> ImportTuExcelAsync(
        ImportCapLaiDangKyXeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Dữ liệu yêu cầu không được rỗng.");
        }

        if (request.File == null && string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("Vui lòng cung cấp tệp Excel (File) hoặc tên tệp tin (FileName) cần import.", nameof(request.FileName));
        }

        // 1. Xác định thư mục lưu trữ và đường dẫn tệp Excel
        var configuredFolder = string.IsNullOrWhiteSpace(_fileStorageSettings.StorageFolder) ? "Uploads" : _fileStorageSettings.StorageFolder;
        var destinationFolder = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        string filePath;

        // Nếu client upload file trực tiếp (multipart/form-data)
        if (request.File != null && request.File.Length > 0)
        {
            var safeExt = Path.GetExtension(request.File.FileName);
            if (string.IsNullOrWhiteSpace(safeExt)) safeExt = ".xlsx";
            var uniqueFileName = $"CapLaiImport_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{safeExt}";
            filePath = Path.Combine(destinationFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }
        }
        else
        {
            var safeFileName = Path.GetFileName(request.FileName!);
            filePath = Path.Combine(destinationFolder, safeFileName);

            if (!File.Exists(filePath))
            {
                if (File.Exists(request.FileName!))
                {
                    filePath = request.FileName!;
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
                        var altPath = Path.Combine(_environment.ContentRootPath, request.FileName!.TrimStart('/', '\\'));
                        if (File.Exists(altPath))
                        {
                            filePath = altPath;
                        }
                        else
                        {
                            var matchingFiles = Directory.Exists(destinationFolder)
                                ? Directory.GetFiles(destinationFolder, $"*{safeFileName}*")
                                : Array.Empty<string>();
                            if (matchingFiles.Length > 0)
                            {
                                filePath = matchingFiles[0];
                            }
                            else
                            {
                                throw new FileNotFoundException($"Không tìm thấy tệp tin '{safeFileName}' trong thư mục lưu trữ hoặc MauBaoCao.", safeFileName);
                            }
                        }
                    }
                }
            }
        }

        // 2. Mở file Excel bằng EPPlus (dùng FileShare.ReadWrite để không bị khóa nếu file đang mở)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var package = new ExcelPackage(fileStream);

        var ws = package.Workbook.Worksheets.FirstOrDefault(w =>
                    w.Name.Equals("CapLai", StringComparison.OrdinalIgnoreCase) ||
                    w.Name.Equals("CapDangKyLai", StringComparison.OrdinalIgnoreCase) ||
                    w.Name.Equals("CapLaiDangKy", StringComparison.OrdinalIgnoreCase) ||
                    w.Name.Equals("XuatKho", StringComparison.OrdinalIgnoreCase) ||
                    w.Name.Equals("Sheet1", StringComparison.OrdinalIgnoreCase))
                 ?? package.Workbook.Worksheets.FirstOrDefault(w => !w.Name.Equals("Danh mục", StringComparison.OrdinalIgnoreCase))
                 ?? package.Workbook.Worksheets.FirstOrDefault();

        if (ws == null || ws.Dimension == null)
        {
            return new ImportCapLaiDangKyXeResponse
            {
                TongSoDong = 0,
                ThongBao = "Tệp Excel không chứa dữ liệu hoặc không có trang tính hợp lệ."
            };
        }

        // 3. Vị trí cột mặc định theo file MauBaoCao/CapLaiBienDangKy.xlsx (Dòng 3 có 13 cột)
        // 1: STT, 2: Ngày biên lai, 3: Biển số xe(*), 4: Mức thu lệ phí(*), 5: Ký hiệu quyển biên lai,
        // 6: Số biên lai, 7: Số tiền thu(VNĐ), 8: Họ tên người nộp tiền(*), 9: Số CCCD,
        // 10: Mã hồ sơ dịch vụ công, 11: Hình thức thu tiền, 12: Nội dung thu phí, 13: Lý do cấp lại đăng ký xe
        int headerRow = 3;
        int colStt = 1;
        int colNgayBienLai = 2;
        int colBienSo = 3;
        int colMucThuLePhi = 4;
        int colKyHieuQuyen = 5;
        int colSoBienLai = 6;
        int colSoTienThu = 7;
        int colHoTenNguoiNop = 8;
        int colSoCccd = 9;
        int colMaHoSoDvc = 10;
        int colHinhThucThuTien = 11;
        int colNoiDungThuPhi = 12;
        int colLyDoCapLai = 13;
        int colLoaiPhuongTien = -1;

        // Quét thông minh nhận diện dòng header thực tế
        int maxScanRow = Math.Min(10, ws.Dimension.End.Row);
        bool foundDynamicHeader = false;

        for (int r = 1; r <= maxScanRow; r++)
        {
            int matchedHeaderCells = 0;
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
            {
                var text = ws.Cells[r, c].Text?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (text == "stt" || text.Contains("thứ tự") ||
                    text.Contains("biển số") || text.Contains("biển kiểm soát") || text == "bks" ||
                    text.Contains("mức thu") || text.Contains("lệ phí") ||
                    text.Contains("quyển") || text.Contains("số biên lai") || text.Contains("ngày biên lai") ||
                    text.Contains("người nộp") || text.Contains("họ tên") ||
                    text.Contains("cccd") || text.Contains("mã hồ sơ") || text.Contains("dvc") ||
                    text.Contains("tiền thu") || text.Contains("hình thức") || text.Contains("nội dung") ||
                    text.Contains("lý do") || text.Contains("ghi chú"))
                {
                    matchedHeaderCells++;
                }
            }

            if (matchedHeaderCells >= 2)
            {
                headerRow = r;
                foundDynamicHeader = true;
                break;
            }
        }

        // Tự động gán lại vị trí cột nếu nhận diện được tiêu đề động
        if (foundDynamicHeader)
        {
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
            {
                var rawText = ws.Cells[headerRow, c].Text?.Trim().ToLowerInvariant() ?? "";
                if (string.IsNullOrWhiteSpace(rawText)) continue;

                var text = Regex.Replace(rawText, @"[\(\)\*\:]", "").Trim();

                if (text == "stt" || text.Contains("thứ tự")) colStt = c;
                else if (text.Contains("ngày biên lai") || text.Contains("ngày bl") || text.Contains("ngày thu") || text.Contains("ngày lập") || text.Contains("ngày thực hiện") || text.Contains("thời gian")) colNgayBienLai = c;
                else if (text.Contains("biển số") || text.Contains("biển kiểm soát") || text == "bks" || text.StartsWith("biển")) colBienSo = c;
                else if (text.Contains("mức thu") || (text.Contains("lệ phí") && !text.Contains("tiền"))) colMucThuLePhi = c;
                else if (text.Contains("ký hiệu quyển") || text.Contains("quyển biên lai") || text.Contains("quyển") || text == "quyen") colKyHieuQuyen = c;
                else if (text.Contains("số biên lai") || text.Contains("số bl") || text == "so bl") colSoBienLai = c;
                else if (text.Contains("số tiền") || text.Contains("tiền thu") || text.Contains("tiền")) colSoTienThu = c;
                else if (text.Contains("người nộp") || text.Contains("họ tên") || text.Contains("chủ xe") || text.Contains("chủ phương tiện")) colHoTenNguoiNop = c;
                else if (text.Contains("cccd") || text.Contains("cmnd") || text.Contains("định danh")) colSoCccd = c;
                else if (text.Contains("hồ sơ") || text.Contains("dvc") || text.Contains("mã hs")) colMaHoSoDvc = c;
                else if (text.Contains("hình thức") || text.Contains("phương thức")) colHinhThucThuTien = c;
                else if (text.Contains("nội dung")) colNoiDungThuPhi = c;
                else if (text.Contains("lý do") || text.Contains("ghi chú")) colLyDoCapLai = c;
                else if (text.Contains("loại xe") || text.Contains("loại phương tiện") || text.Contains("phương tiện")) colLoaiPhuongTien = c;
            }
        }

        if (ws.Dimension.End.Row <= headerRow)
        {
            return new ImportCapLaiDangKyXeResponse
            {
                TongSoDong = 0,
                ThongBao = "Tệp Excel chỉ chứa dòng tiêu đề, không có dữ liệu biên lai cấp lại đăng ký nào."
            };
        }

        // 4. Nạp danh mục cần thiết để liên kết
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;
        var currentUserName = _currentUserService.FullName ?? _currentUserService.Username ?? "Quản trị viên";

        var listMucThu = await _dbContext.MucThuLePhis.AsNoTracking().ToListAsync(cancellationToken);
        var listQuyen = await _dbContext.KyHieuQuyenBienLais.ToListAsync(cancellationToken);
        var listLoaiXe = await _dbContext.LoaiXes.AsNoTracking().ToListAsync(cancellationToken);

        var mucThuCache = new Dictionary<string, MucThuLePhi?>(StringComparer.OrdinalIgnoreCase);
        var quyenCache = new Dictionary<string, KyHieuQuyenBienLai?>(StringComparer.OrdinalIgnoreCase);
        var loaiXeCache = new Dictionary<string, LoaiXe?>(StringComparer.OrdinalIgnoreCase);

        DateTime? ParseExcelDate(int r, int colIdx)
        {
            if (colIdx <= 0) return null;
            var cell = ws.Cells[r, colIdx];
            var val = cell.Value;
            if (val is DateTime dtVal) return dtVal;
            if (val is double dDate && dDate > 0 && dDate < 2958466)
            {
                try { return DateTime.FromOADate(dDate); } catch { }
            }
            var textDate = cell.Text?.Trim();
            if (string.IsNullOrWhiteSpace(textDate)) return null;

            string[] dateFormats = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
            if (DateTime.TryParseExact(textDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDt))
                return exactDt;
            if (DateTime.TryParse(textDate, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var viDt))
                return viDt;
            if (DateTime.TryParse(textDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invDt))
                return invDt;

            return null;
        }

        MucThuLePhi? ResolveMucThu(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return listMucThu.FirstOrDefault();
            var trimmed = raw.Trim();
            if (mucThuCache.TryGetValue(trimmed, out var cached)) return cached;

            var result = listMucThu.FirstOrDefault(x =>
                string.Equals(x.Ten, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Ma, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Id, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? listMucThu.FirstOrDefault(x =>
                    x.Ten.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains(x.Ten, StringComparison.OrdinalIgnoreCase))
                ?? listMucThu.FirstOrDefault();

            mucThuCache[trimmed] = result;
            return result;
        }

        KyHieuQuyenBienLai? ResolveQuyen(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return listQuyen.FirstOrDefault();
            var trimmed = raw.Trim();
            if (quyenCache.TryGetValue(trimmed, out var cached)) return cached;

            var result = listQuyen.FirstOrDefault(x =>
                string.Equals(x.Ma, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Ten, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Id, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? listQuyen.FirstOrDefault(x =>
                    x.Ma.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    x.Ten.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                ?? listQuyen.FirstOrDefault();

            quyenCache[trimmed] = result;
            return result;
        }

        LoaiXe? ResolveLoaiXe(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var trimmed = raw.Trim();
            if (loaiXeCache.TryGetValue(trimmed, out var cached)) return cached;

            var result = listLoaiXe.FirstOrDefault(x =>
                string.Equals(x.Ma, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Ten, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Id, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? listLoaiXe.FirstOrDefault(x =>
                    x.Ten.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains(x.Ten, StringComparison.OrdinalIgnoreCase));

            loaiXeCache[trimmed] = result;
            return result;
        }

        var response = new ImportCapLaiDangKyXeResponse();
        var processedPlatesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entitiesToAdd = new List<CapLaiDangKyXe>();
        long receiptOffset = 0;

        // 5. Quét từng dòng dữ liệu từ Excel
        for (int row = headerRow + 1; row <= ws.Dimension.End.Row; row++)
        {
            var rawBienSo = colBienSo > 0 ? ws.Cells[row, colBienSo].Text?.Trim() : null;
            if (string.IsNullOrWhiteSpace(rawBienSo))
            {
                continue;
            }

            // Bỏ qua nếu dòng là tiêu đề lặp lại
            if (rawBienSo.Equals("biển số xe", StringComparison.OrdinalIgnoreCase) ||
                rawBienSo.Equals("biển kiểm soát", StringComparison.OrdinalIgnoreCase) ||
                rawBienSo.Equals("bks", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            response.TongSoDong++;

            // Chuẩn hóa biển kiểm soát
            var bienKiemSoat = rawBienSo.ToUpperInvariant().Trim();
            var normalizedPlate = Regex.Replace(bienKiemSoat, @"[\s\.\-]+", "").ToUpperInvariant();

            // Kiểm tra trùng lặp trong cùng tệp Excel
            if (!processedPlatesInFile.Add(normalizedPlate))
            {
                response.SoLuongBoQua++;
                response.DanhSachLoi.Add(new ImportCapLaiLoiItem
                {
                    Dong = row,
                    BienKiemSoat = bienKiemSoat,
                    LyDoLoi = $"Biển số '{bienKiemSoat}' bị trùng lặp nhiều lần trong tệp Excel."
                });
                continue;
            }

            try
            {
                var parsedNgayBienLai = ParseExcelDate(row, colNgayBienLai);
                var ngayBienLai = parsedNgayBienLai ?? DateTime.UtcNow;

                var mucThuRaw = colMucThuLePhi > 0 ? ws.Cells[row, colMucThuLePhi].Text?.Trim() : null;
                var mucThu = ResolveMucThu(mucThuRaw);

                var kyHieuQuyenRaw = colKyHieuQuyen > 0 ? ws.Cells[row, colKyHieuQuyen].Text?.Trim() : string.Empty;
                var quyen = ResolveQuyen(kyHieuQuyenRaw);
                var kyHieuQuyen = !string.IsNullOrWhiteSpace(kyHieuQuyenRaw) ? kyHieuQuyenRaw : (quyen?.Ma ?? string.Empty);
                var kyHieuQuyenBienLaiId = quyen?.Id ?? string.Empty;

                var soBienLaiRaw = colSoBienLai > 0 ? ws.Cells[row, colSoBienLai].Text?.Trim() : string.Empty;
                string soBienLai;
                if (!string.IsNullOrWhiteSpace(soBienLaiRaw))
                {
                    soBienLai = soBienLaiRaw;
                }
                else if (quyen != null)
                {
                    long currentSo = quyen.SoHienTai ?? ((quyen.TuSo ?? 1) - 1);
                    long nextSo = currentSo + 1 + receiptOffset++;
                    soBienLai = nextSo.ToString("D7");
                }
                else
                {
                    soBienLai = (++receiptOffset).ToString("D7");
                }

                // Đọc số tiền lệ phí thu (mặc định 50.000đ theo TT 155/2025/TT-BTC nếu rỗng hoặc = 0)
                decimal soTienThu = 0;
                bool hasExplicitMoney = false;
                if (colSoTienThu > 0)
                {
                    var valMoney = ws.Cells[row, colSoTienThu].Value;
                    if (valMoney is double dVal) { soTienThu = Convert.ToDecimal(dVal); hasExplicitMoney = true; }
                    else if (valMoney is decimal decVal) { soTienThu = decVal; hasExplicitMoney = true; }
                    else if (valMoney is int iVal) { soTienThu = iVal; hasExplicitMoney = true; }
                    else if (valMoney is long lVal) { soTienThu = lVal; hasExplicitMoney = true; }
                    else
                    {
                        var textMoney = ws.Cells[row, colSoTienThu].Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(textMoney))
                        {
                            var cleanMoney = textMoney.Replace(",", "").Replace(".", "").Replace("đ", "").Replace("₫", "").Replace("VND", "", StringComparison.OrdinalIgnoreCase).Replace("VNĐ", "", StringComparison.OrdinalIgnoreCase).Trim();
                            if (decimal.TryParse(cleanMoney, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMoney) ||
                                decimal.TryParse(cleanMoney, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out parsedMoney))
                            {
                                soTienThu = parsedMoney;
                                hasExplicitMoney = true;
                            }
                        }
                    }
                }

                if (!hasExplicitMoney || soTienThu <= 0)
                {
                    soTienThu = 50000;
                }

                var hoTenChuXe = colHoTenNguoiNop > 0 ? ws.Cells[row, colHoTenNguoiNop].Text?.Trim() : null;
                if (string.IsNullOrWhiteSpace(hoTenChuXe)) hoTenChuXe = "Người nộp tiền";

                var soCccd = colSoCccd > 0 ? ws.Cells[row, colSoCccd].Text?.Trim() : null;
                var maHoSoDvc = colMaHoSoDvc > 0 ? ws.Cells[row, colMaHoSoDvc].Text?.Trim() : null;

                var hinhThucThu = colHinhThucThuTien > 0 ? ws.Cells[row, colHinhThucThuTien].Text?.Trim() : null;
                if (string.IsNullOrWhiteSpace(hinhThucThu)) hinhThucThu = "Tiền mặt";

                var noiDungThu = colNoiDungThuPhi > 0 ? ws.Cells[row, colNoiDungThuPhi].Text?.Trim() : null;
                if (string.IsNullOrWhiteSpace(noiDungThu))
                {
                    noiDungThu = mucThu?.Ten ?? "Cấp lại riêng giấy chứng nhận đăng ký xe (mất, hư hỏng - không cấp biển mới)";
                }

                var lyDoCapLai = colLyDoCapLai > 0 ? ws.Cells[row, colLyDoCapLai].Text?.Trim() : null;
                if (string.IsNullOrWhiteSpace(lyDoCapLai))
                {
                    lyDoCapLai = "Cấp lại chứng nhận đăng ký xe bị mất/hỏng theo đơn";
                }

                // Xác định Loại xe và Loại phương tiện
                var rawLoaiPhuongTien = colLoaiPhuongTien > 0 ? ws.Cells[row, colLoaiPhuongTien].Text?.Trim() : null;
                string? loaiXeId = request.LoaiXeId ?? mucThu?.LoaiXeId;
                string loaiPhuongTien = "Ô tô";

                if (!string.IsNullOrWhiteSpace(rawLoaiPhuongTien))
                {
                    loaiPhuongTien = rawLoaiPhuongTien;
                }
                else if (!string.IsNullOrWhiteSpace(request.LoaiPhuongTien))
                {
                    loaiPhuongTien = request.LoaiPhuongTien;
                }
                else if (!string.IsNullOrWhiteSpace(loaiXeId))
                {
                    var loaiXe = ResolveLoaiXe(loaiXeId);
                    if (loaiXe != null) loaiPhuongTien = loaiXe.Ten;
                }

                // NGUYÊN TẮC QUAN TRỌNG:
                // 1. Biển số không lấy trong kho (không tìm kiếm và không đòi hỏi biển số phải tồn tại trong KhoBienSo).
                // 2. Không thay đổi dữ liệu trong kho (không cập nhật trạng thái hay xuất kho trong KhoBienSo).
                // 3. Mặc định DaXacNhan = true (tự động khóa biên lai, phát hành ngay).
                bool daXacNhan = request.DaXacNhan;

                var entity = new CapLaiDangKyXe
                {
                    CustomerId = customerId,
                    KyHieuQuyenBienLaiId = kyHieuQuyenBienLaiId,
                    KyHieuQuyen = kyHieuQuyen,
                    SoBienLai = soBienLai,
                    NgayBienLai = ngayBienLai,
                    BienKiemSoat = bienKiemSoat,
                    LoaiXeId = loaiXeId,
                    LoaiPhuongTien = loaiPhuongTien,
                    MucThuLePhiId = mucThu?.Id,
                    NoiDungThuPhi = noiDungThu,
                    SoTienThu = soTienThu,
                    HinhThucThuTien = hinhThucThu,
                    HoTenChuXe = hoTenChuXe,
                    SoCccd = string.IsNullOrWhiteSpace(soCccd) ? null : soCccd,
                    MaHoSoDvc = string.IsNullOrWhiteSpace(maHoSoDvc) ? null : maHoSoDvc,
                    LyDoCapLai = string.IsNullOrWhiteSpace(lyDoCapLai) ? null : lyDoCapLai,
                    TrangThai = daXacNhan ? "DaXacNhan" : "LuuNhap",
                    DaXacNhan = daXacNhan,
                    NgayXacNhan = daXacNhan ? DateTime.UtcNow : null,
                    NguoiXacNhan = daXacNhan ? currentUserName : null
                };

                entitiesToAdd.Add(entity);
                response.SoLuongThanhCong++;
                response.TongTienThu += soTienThu;
                response.DanhSachBienSoThanhCong.Add(bienKiemSoat);
            }
            catch (Exception ex)
            {
                response.SoLuongLoi++;
                response.DanhSachLoi.Add(new ImportCapLaiLoiItem
                {
                    Dong = row,
                    BienKiemSoat = bienKiemSoat,
                    LyDoLoi = $"Lỗi xử lý dòng dữ liệu: {ex.Message}"
                });
            }
        }

        // 6. Lưu vào cơ sở dữ liệu nếu không phải chế độ kiểm tra trước (dry-run)
        if (request.ChiKiemTra != true && entitiesToAdd.Count > 0)
        {
            await _dbContext.CapLaiDangKyXes.AddRangeAsync(entitiesToAdd, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Ghi nhật ký hoạt động
            await _logHoatDongService.GhiLogAsync(
                "CapLaiDangKy",
                "ImportExcel",
                Path.GetFileName(filePath),
                $"Import thành công {entitiesToAdd.Count} biên lai cấp lại đăng ký xe từ file '{Path.GetFileName(filePath)}'. Mặc định Đã xác nhận (DaXacNhan=true).",
                null,
                null,
                "DaXacNhan",
                "Đã xác nhận (Đã phát hành)",
                null,
                customerId,
                "Import Excel Cấp lại đăng ký xe",
                cancellationToken);
        }

        // 7. Tổng kết thông báo trả về
        response.ThongBao = request.ChiKiemTra == true
            ? $"[Kiểm tra trước] Tệp Excel hợp lệ với {response.TongSoDong} dòng dữ liệu ({response.SoLuongThanhCong} dòng sẵn sàng import, {response.SoLuongBoQua} dòng bỏ qua, {response.SoLuongLoi} dòng lỗi). Chưa lưu vào CSDL."
            : $"Import thành công {response.SoLuongThanhCong}/{response.TongSoDong} biên lai cấp lại đăng ký xe (Mặc định Đã xác nhận/Khóa phiếu, biển số không lấy trong kho và không thay đổi kho). Tổng tiền thu: {response.TongTienThu:N0} VNĐ.";

        _logger.LogInformation("Import Cấp lại đăng ký xe: Thành công {ThanhCong}/{TongSo} dòng từ file '{FilePath}', tổng tiền: {TongTien:N0}đ",
            response.SoLuongThanhCong, response.TongSoDong, filePath, response.TongTienThu);

        return response;
    }
}

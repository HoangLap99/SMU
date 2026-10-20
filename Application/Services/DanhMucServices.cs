using System.Drawing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Lớp cơ sở trừu tượng hiện thực hóa 5 API cốt lõi cho mọi danh mục:
/// 1. GetPagedList: Lấy danh sách (Keyword, Customer của user, Phân trang, Sắp xếp ThuTu, Ma).
/// 2. Set: Vừa thêm mới và cập nhật bản ghi (Upsert).
/// 3. DeleteMultiple: Xóa theo danh sách Id.
/// 4. ImportExcel: Import danh mục từ Excel bằng EPPlus 6.0.8.
/// 5. ExportExcel: Export danh mục nhiều dữ liệu ra Excel bằng EPPlus 6.0.8.
/// </summary>
public abstract class BaseDanhMucService<TEntity, TResponse, TSetRequest> 
    : IBaseDanhMucService<TResponse, TSetRequest>
    where TEntity : DanhMucBase, new()
    where TResponse : BaseDanhMucResponse, new()
    where TSetRequest : BaseSetDanhMucRequest
{
    protected readonly PostgresDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly ILogger Logger;

    protected BaseDanhMucService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger logger)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
        CurrentUserService = currentUserService;
        Logger = logger;
    }

    /// <summary>
    /// Tạo truy vấn cơ sở kèm theo các Include cần thiết. Các lớp dẫn xuất có thể ghi đè để Include thêm quan hệ.
    /// </summary>
    protected virtual IQueryable<TEntity> GetBaseQuery()
    {
        return DbSet.AsNoTracking().AsQueryable();
    }

    #region 0. Lấy danh sách không phân trang (Chỉ nhận CustomerId)

    public virtual async Task<IReadOnlyList<TResponse>> GetAllAsync(
        string? customerId = null, 
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserService.UserId;
        var query = GetBaseQuery();

        //// 1. Lọc theo CustomerId nếu được truyền vào
        //if (!string.IsNullOrWhiteSpace(customerId))
        //{
        //    query = query.Where(x => x.CustomerId == customerId);
        //}
        //else if (!string.IsNullOrWhiteSpace(CurrentUserService.CustomerId))
        //{
        //    var cid = CurrentUserService.CustomerId;
        //    query = query.Where(x => x.CustomerId == null || x.CustomerId == cid);
        //}
        //else if (!string.IsNullOrWhiteSpace(currentUserId))
        //{
        //    var userCustomerIds = await DbContext.Users
        //        .AsNoTracking()
        //        .Where(u => u.Id == currentUserId && u.IsActive && u.CustomerId != null)
        //        .Select(u => u.CustomerId!)
        //        .ToListAsync(cancellationToken);

        //    if (userCustomerIds.Count > 0)
        //    {
        //        query = query.Where(x => x.CustomerId == null || userCustomerIds.Contains(x.CustomerId));
        //    }
        //}

        // 2. Sắp xếp danh sách theo: Thứ tự (ThuTu), sau đó đến Mã (Ma)
        var list = await query
            .Where(x=>x.IsActive)
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.Ma)
            .ToListAsync(cancellationToken);

        return list.Select(MapToResponse).ToList();
    }

    #endregion

    #region 1. Lấy danh sách (Phân trang, Keyword, Customer của User, Sắp xếp ThuTu, Ma)

    public virtual async Task<PagedResult<TResponse>> GetPagedListAsync(
        PagedDanhMucRequest request, 
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserService.UserId;
        var query = GetBaseQuery();

        // 1. Lọc theo Customer: Nếu không truyền CustomerId thì lấy danh sách đơn vị của user đang đăng nhập
        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            query = query.Where(x => x.CustomerId == request.CustomerId);
        }
        else if (!string.IsNullOrWhiteSpace(CurrentUserService.CustomerId))
        {
            var cid = CurrentUserService.CustomerId;
            query = query.Where(x => x.CustomerId == null || x.CustomerId == cid);
        }
        else if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            var userCustomerIds = await DbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId && u.IsActive && u.CustomerId != null)
                .Select(u => u.CustomerId!)
                .ToListAsync(cancellationToken);

            if (userCustomerIds.Count > 0)
            {
                query = query.Where(x => x.CustomerId == null || userCustomerIds.Contains(x.CustomerId));
            }
        }

        // 2. Lọc theo trạng thái hoạt động (nếu có)
        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        // 3. Tìm kiếm theo từ khóa (Mã hoặc Tên)
        if (!string.IsNullOrWhiteSpace(request.KeyWord))
        {
            var kw = request.KeyWord.Trim().ToLower();
            query = query.Where(x => x.Ma.ToLower().Contains(kw) || x.Ten.ToLower().Contains(kw));
        }

        // 4. Sắp xếp bắt buộc theo: Thứ tự (ThuTu), sau đó đến Mã (Ma)
        query = query.OrderBy(x => x.ThuTu).ThenBy(x => x.Ma);

        // 5. Phân trang
        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.CurrentPage > 0 ? request.CurrentPage : 1;
        var size = request.PageSize > 0 ? request.PageSize : 10;

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResponse>
        {
            CurrentPage = page,
            PageSize = size,
            TotalCount = totalCount,
            Items = items.Select(MapToResponse).ToList()
        };
    }

    #endregion

    #region 2. Set (Vừa thêm mới vừa cập nhật)

    public virtual async Task<TResponse> SetAsync(
        TSetRequest request, 
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserService.UserId;
        var customerId = request.CustomerId ?? CurrentUserService.CustomerId;

        // Nếu request không truyền CustomerId, tự động gắn Customer đầu tiên mà user đang tham gia
        if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(currentUserId))
        {
            customerId = await DbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId && u.IsActive)
                .Select(u => u.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var normalizedMa = request.Ma.Trim();

        // Kiểm tra xem Id đã tồn tại trong DB chưa
        TEntity? entity = null;
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            entity = await DbSet.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        }

        // Kiểm tra trùng mã trong cùng 1 Customer
        var duplicateQuery = DbSet.Where(x => x.Ma.ToLower() == normalizedMa.ToLower() && x.CustomerId == customerId);
        if (entity != null)
        {
            duplicateQuery = duplicateQuery.Where(x => x.Id != entity.Id);
        }

        if (await duplicateQuery.AnyAsync(cancellationToken))
        {
            throw new BadHttpRequestException($"Mã danh mục '{normalizedMa}' đã tồn tại trong đơn vị.");
        }

        if (entity != null)
        {
            // Cập nhật bản ghi hiện tại
            entity.CustomerId = customerId;
            entity.Ma = normalizedMa;
            entity.Ten = request.Ten.Trim();
            entity.MoTa = request.MoTa?.Trim();
            entity.ThuTu = request.ThuTu;
            entity.IsActive = request.IsActive;

            MapSetSpecific(request, entity);
            Logger.LogInformation("Cập nhật danh mục {Type} Id={Id}", typeof(TEntity).Name, entity.Id);
        }
        else
        {
            // Thêm mới bản ghi
            entity = new TEntity
            {
                CustomerId = customerId,
                Ma = normalizedMa,
                Ten = request.Ten.Trim(),
                MoTa = request.MoTa?.Trim(),
                ThuTu = request.ThuTu,
                IsActive = request.IsActive
            };

            MapSetSpecific(request, entity);
            await DbSet.AddAsync(entity, cancellationToken);
            Logger.LogInformation("Tạo mới danh mục {Type} Ma={Ma} cho Customer={CustomerId}", typeof(TEntity).Name, entity.Ma, customerId);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(entity);
    }

    #endregion

    #region 3. Delete theo listId danh mục

    public virtual async Task<int> DeleteMultipleAsync(
        List<string> ids, 
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0) return 0;

        var cleanIds = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        var entities = await DbSet.Where(x => cleanIds.Contains(x.Id!)).ToListAsync(cancellationToken);
        if (entities.Count == 0) return 0;

        DbSet.RemoveRange(entities);
        var deletedCount = await DbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Đã xóa {Count} bản ghi danh mục {Type}", entities.Count, typeof(TEntity).Name);
        return entities.Count;
    }

    #endregion

    #region 4. Import Excel (EPPlus 6.0.8)

    public virtual async Task<ImportResult> ImportExcelAsync(
        string fileName, 
        string? customerId = null, 
        CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath(fileName);
        var currentUserId = CurrentUserService.UserId;

        if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(currentUserId))
        {
            customerId = await DbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId && u.IsActive)
                .Select(u => u.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var result = new ImportResult();
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null || worksheet.Dimension == null)
        {
            throw new BadHttpRequestException("Tệp tin Excel không có dữ liệu hoặc không đúng định dạng.");
        }

        var totalRows = worksheet.Dimension.End.Row;
        result.TotalRows = Math.Max(0, totalRows - 1); // Bỏ dòng tiêu đề (Header row 1)

        for (int row = 2; row <= totalRows; row++)
        {
            var ma = worksheet.Cells[row, 1].Text?.Trim();
            var ten = worksheet.Cells[row, 2].Text?.Trim();

            if (string.IsNullOrWhiteSpace(ma) || string.IsNullOrWhiteSpace(ten))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: Mã hoặc Tên danh mục không được để trống.");
                continue;
            }

            try
            {
                var moTa = worksheet.Cells[row, 3].Text?.Trim();
                int.TryParse(worksheet.Cells[row, 4].Text?.Trim(), out var thuTu);
                var isActiveText = worksheet.Cells[row, 5].Text?.Trim().ToLower();
                var isActive = string.IsNullOrEmpty(isActiveText) || isActiveText == "1" || isActiveText == "true" || isActiveText == "có";

                // Kiểm tra xem đã có bản ghi này chưa (theo Ma và CustomerId)
                var existing = await DbSet.FirstOrDefaultAsync(x => x.Ma.ToLower() == ma.ToLower() && x.CustomerId == customerId, cancellationToken);
                if (existing != null)
                {
                    existing.Ten = ten;
                    existing.MoTa = moTa;
                    existing.ThuTu = thuTu;
                    existing.IsActive = isActive;
                    ReadSpecificExcelColumns(worksheet, row, existing);
                }
                else
                {
                    var newEntity = new TEntity
                    {
                        CustomerId = customerId,
                        Ma = ma,
                        Ten = ten,
                        MoTa = moTa,
                        ThuTu = thuTu,
                        IsActive = isActive
                    };
                    ReadSpecificExcelColumns(worksheet, row, newEntity);
                    await DbSet.AddAsync(newEntity, cancellationToken);
                }

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: {ex.Message}");
            }
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Import Excel cho {Type} hoàn tất: Thành công {Success}, Thất bại {Error}", 
            typeof(TEntity).Name, result.SuccessCount, result.ErrorCount);

        return result;
    }

    #endregion

    #region 5. Export Excel nhiều dữ liệu (EPPlus 6.0.8)

    public virtual async Task<byte[]> ExportExcelAsync(
        ExportDanhMucRequest request, 
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserService.UserId;
        var query = GetBaseQuery();

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            query = query.Where(x => x.CustomerId == request.CustomerId);
        }
        else if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            var userCustomerIds = await DbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId && u.IsActive && u.CustomerId != null)
                .Select(u => u.CustomerId!)
                .ToListAsync(cancellationToken);

            if (userCustomerIds.Count > 0)
            {
                query = query.Where(x => x.CustomerId == null || userCustomerIds.Contains(x.CustomerId));
            }
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.KeyWord))
        {
            var kw = request.KeyWord.Trim().ToLower();
            query = query.Where(x => x.Ma.ToLower().Contains(kw) || x.Ten.ToLower().Contains(kw));
        }

        var list = await query
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.Ma)
            .ToListAsync(cancellationToken);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(GetCatalogTitle());

        // Header cột
        var headers = GetExcelHeaders();
        for (int col = 1; col <= headers.Count; col++)
        {
            var cell = worksheet.Cells[1, col];
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 120)); // Xanh Navy chuyên nghiệp
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        worksheet.Row(1).Height = 28;

        // Điền dữ liệu
        int currentRow = 2;
        foreach (var item in list)
        {
            worksheet.Cells[currentRow, 1].Value = item.Ma;
            worksheet.Cells[currentRow, 2].Value = item.Ten;
            worksheet.Cells[currentRow, 3].Value = item.MoTa;
            worksheet.Cells[currentRow, 4].Value = item.ThuTu;
            worksheet.Cells[currentRow, 5].Value = item.IsActive ? "Đang hoạt động" : "Ngừng hoạt động";

            WriteSpecificExcelColumns(worksheet, currentRow, item);
            currentRow++;
        }

        // Kẻ border và tự động giãn độ rộng cột
        if (list.Count > 0)
        {
            var dataRange = worksheet.Cells[1, 1, currentRow - 1, headers.Count];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        worksheet.Cells.AutoFitColumns();

        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    #endregion

    #region Helper Methods

    protected void PopulateBaseResponse(TEntity entity, BaseDanhMucResponse response)
    {
        response.Id = entity.Id ?? string.Empty;
        response.CustomerId = entity.CustomerId;
        response.Ma = entity.Ma;
        response.Ten = entity.Ten;
        response.MoTa = entity.MoTa;
        response.ThuTu = entity.ThuTu;
        response.IsActive = entity.IsActive;
        response.Created = entity.Created;
        response.CreatedByName = entity.CreatedByName;
        response.Modified = entity.Modified;
        response.ModifiedByName = entity.ModifileByName;
    }

    private string ResolveFilePath(string fileName)
    {
        if (File.Exists(fileName)) return fileName;

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", fileName);
        if (File.Exists(uploadsFolder)) return uploadsFolder;

        var baseUploads = Path.Combine(AppContext.BaseDirectory, "Uploads", fileName);
        if (File.Exists(baseUploads)) return baseUploads;

        var rootFile = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        if (File.Exists(rootFile)) return rootFile;

        throw new FileNotFoundException($"Không tìm thấy tệp tin Excel '{fileName}' trên máy chủ.");
    }

    protected abstract string GetCatalogTitle();
    protected abstract List<string> GetExcelHeaders();
    protected abstract TResponse MapToResponse(TEntity entity);
    protected abstract void MapSetSpecific(TSetRequest request, TEntity entity);
    protected abstract void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, TEntity entity);
    protected abstract void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, TEntity entity);

    #endregion
}

#region 1. Dịch vụ Đầu biển số (dau_bien_so)

public class DauBienSoService : BaseDanhMucService<DauBienSo, DauBienSoResponse, SetDauBienSoRequest>, IDauBienSoService
{
    public DauBienSoService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<DauBienSoService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Đầu biển số";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã đầu biển số", "Tỉnh / Thành phố", "Mô tả", "Thứ tự", "Trạng thái", "Ký hiệu sê-ri", "Mã tỉnh thành"
    };

    protected override DauBienSoResponse MapToResponse(DauBienSo entity)
    {
        var res = new DauBienSoResponse
        {
            KyHieuSeries = entity.KyHieuSeries,
            MaTinhThanh = entity.MaTinhThanh
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetDauBienSoRequest request, DauBienSo entity)
    {
        entity.KyHieuSeries = request.KyHieuSeries?.Trim();
        entity.MaTinhThanh = request.MaTinhThanh?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, DauBienSo entity)
    {
        entity.KyHieuSeries = ws.Cells[row, 6].Text?.Trim();
        entity.MaTinhThanh = ws.Cells[row, 7].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, DauBienSo entity)
    {
        ws.Cells[row, 6].Value = entity.KyHieuSeries;
        ws.Cells[row, 7].Value = entity.MaTinhThanh;
    }
}

#endregion

#region 2. Dịch vụ Màu biển số (mau_bien_so)

public class MauBienSoService : BaseDanhMucService<MauBienSo, MauBienSoResponse, SetMauBienSoRequest>, IMauBienSoService
{
    public MauBienSoService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<MauBienSoService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Màu biển số";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã màu", "Tên màu biển số", "Mô tả", "Thứ tự", "Trạng thái", "Mã màu Hex", "Đối tượng sử dụng"
    };

    protected override MauBienSoResponse MapToResponse(MauBienSo entity)
    {
        var res = new MauBienSoResponse
        {
            MaMauHex = entity.MaMauHex,
            DoiTuongSuDung = entity.DoiTuongSuDung
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetMauBienSoRequest request, MauBienSo entity)
    {
        entity.MaMauHex = request.MaMauHex?.Trim();
        entity.DoiTuongSuDung = request.DoiTuongSuDung?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, MauBienSo entity)
    {
        entity.MaMauHex = ws.Cells[row, 6].Text?.Trim();
        entity.DoiTuongSuDung = ws.Cells[row, 7].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, MauBienSo entity)
    {
        ws.Cells[row, 6].Value = entity.MaMauHex;
        ws.Cells[row, 7].Value = entity.DoiTuongSuDung;
    }
}

#endregion

#region 3. Dịch vụ Trạng thái biển số (trang_thai_bien_so)

public class TrangThaiBienSoService : BaseDanhMucService<TrangThaiBienSo, TrangThaiBienSoResponse, SetTrangThaiBienSoRequest>, ITrangThaiBienSoService
{
    public TrangThaiBienSoService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<TrangThaiBienSoService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Trạng thái biển số";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã trạng thái", "Tên trạng thái", "Mô tả", "Thứ tự", "Trạng thái", "Cho phép cấp mới", "Màu sắc hiển thị"
    };

    protected override TrangThaiBienSoResponse MapToResponse(TrangThaiBienSo entity)
    {
        var res = new TrangThaiBienSoResponse
        {
            ChoPhepCapMoi = entity.ChoPhepCapMoi,
            MauSacHienThi = entity.MauSacHienThi
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetTrangThaiBienSoRequest request, TrangThaiBienSo entity)
    {
        entity.ChoPhepCapMoi = request.ChoPhepCapMoi;
        entity.MauSacHienThi = request.MauSacHienThi?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, TrangThaiBienSo entity)
    {
        var text = ws.Cells[row, 6].Text?.Trim().ToLower();
        entity.ChoPhepCapMoi = text == "1" || text == "true" || text == "có";
        entity.MauSacHienThi = ws.Cells[row, 7].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, TrangThaiBienSo entity)
    {
        ws.Cells[row, 6].Value = entity.ChoPhepCapMoi ? "Có" : "Không";
        ws.Cells[row, 7].Value = entity.MauSacHienThi;
    }
}

#endregion

#region 4. Dịch vụ Loại xe (loai_xe)

public class LoaiXeService : BaseDanhMucService<LoaiXe, LoaiXeResponse, SetLoaiXeRequest>, ILoaiXeService
{
    public LoaiXeService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<LoaiXeService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Loại xe";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã loại xe", "Tên loại xe", "Mô tả", "Thứ tự", "Trạng thái", "Nhóm xe", "Số chỗ ngồi", "Tải trọng (tấn)"
    };

    protected override LoaiXeResponse MapToResponse(LoaiXe entity)
    {
        var res = new LoaiXeResponse
        {
            NhomXe = entity.NhomXe,
            SoChoNgoiToiDa = entity.SoChoNgoiToiDa,
            TaiTrongToiDa = entity.TaiTrongToiDa
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetLoaiXeRequest request, LoaiXe entity)
    {
        entity.NhomXe = request.NhomXe?.Trim();
        entity.SoChoNgoiToiDa = request.SoChoNgoiToiDa;
        entity.TaiTrongToiDa = request.TaiTrongToiDa;
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, LoaiXe entity)
    {
        entity.NhomXe = ws.Cells[row, 6].Text?.Trim();
        if (int.TryParse(ws.Cells[row, 7].Text?.Trim(), out var cho)) entity.SoChoNgoiToiDa = cho;
        if (double.TryParse(ws.Cells[row, 8].Text?.Trim(), out var tai)) entity.TaiTrongToiDa = tai;
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, LoaiXe entity)
    {
        ws.Cells[row, 6].Value = entity.NhomXe;
        ws.Cells[row, 7].Value = entity.SoChoNgoiToiDa;
        ws.Cells[row, 8].Value = entity.TaiTrongToiDa;
    }
}

#endregion

#region 5. Dịch vụ Loại thủ tục (loai_thu_tuc)

public class LoaiThuTucService : BaseDanhMucService<LoaiThuTuc, LoaiThuTucResponse, SetLoaiThuTucRequest>, ILoaiThuTucService
{
    public LoaiThuTucService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<LoaiThuTucService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Loại thủ tục";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã thủ tục", "Tên thủ tục", "Mô tả", "Thứ tự", "Trạng thái", "Thời hạn xử lý", "Lệ phí mặc định", "Căn cứ pháp lý"
    };

    protected override LoaiThuTucResponse MapToResponse(LoaiThuTuc entity)
    {
        var res = new LoaiThuTucResponse
        {
            ThoiHanXuLy = entity.ThoiHanXuLy,
            LePhiMacDinh = entity.LePhiMacDinh,
            CanCuPhapLy = entity.CanCuPhapLy
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetLoaiThuTucRequest request, LoaiThuTuc entity)
    {
        entity.ThoiHanXuLy = request.ThoiHanXuLy?.Trim();
        entity.LePhiMacDinh = request.LePhiMacDinh;
        entity.CanCuPhapLy = request.CanCuPhapLy?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, LoaiThuTuc entity)
    {
        entity.ThoiHanXuLy = ws.Cells[row, 6].Text?.Trim();
        if (decimal.TryParse(ws.Cells[row, 7].Text?.Trim(), out var fee)) entity.LePhiMacDinh = fee;
        entity.CanCuPhapLy = ws.Cells[row, 8].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, LoaiThuTuc entity)
    {
        ws.Cells[row, 6].Value = entity.ThoiHanXuLy;
        ws.Cells[row, 7].Value = entity.LePhiMacDinh;
        ws.Cells[row, 8].Value = entity.CanCuPhapLy;
    }
}

#endregion

#region 6. Dịch vụ Khu vực áp dụng (khu_vuc_ap_dung)

public class KhuVucApDungService : BaseDanhMucService<KhuVucApDung, KhuVucApDungResponse, SetKhuVucApDungRequest>, IKhuVucApDungService
{
    public KhuVucApDungService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<KhuVucApDungService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Khu vực áp dụng";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã khu vực", "Tên khu vực", "Mô tả", "Thứ tự", "Trạng thái", "Phạm vi áp dụng", "Hệ số phí"
    };

    protected override KhuVucApDungResponse MapToResponse(KhuVucApDung entity)
    {
        var res = new KhuVucApDungResponse
        {
            PhamVi = entity.PhamVi,
            HeSoPhi = entity.HeSoPhi
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetKhuVucApDungRequest request, KhuVucApDung entity)
    {
        entity.PhamVi = request.PhamVi?.Trim();
        entity.HeSoPhi = request.HeSoPhi;
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, KhuVucApDung entity)
    {
        entity.PhamVi = ws.Cells[row, 6].Text?.Trim();
        if (double.TryParse(ws.Cells[row, 7].Text?.Trim(), out var heSo)) entity.HeSoPhi = heSo;
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, KhuVucApDung entity)
    {
        ws.Cells[row, 6].Value = entity.PhamVi;
        ws.Cells[row, 7].Value = entity.HeSoPhi;
    }
}

#endregion

#region 7. Dịch vụ Ký hiệu quyển biên lai (ky_hieu_quyen_bien_lai)

public class KyHieuQuyenBienLaiService : BaseDanhMucService<KyHieuQuyenBienLai, KyHieuQuyenBienLaiResponse, SetKyHieuQuyenBienLaiRequest>, IKyHieuQuyenBienLaiService
{
    public KyHieuQuyenBienLaiService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<KyHieuQuyenBienLaiService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Ký hiệu quyển biên lai";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Ký hiệu quyển", "Tên quyển biên lai", "Mô tả", "Thứ tự", "Trạng thái", "Từ số", "Đến số", "Số hiện tại", "Năm phát hành", "Đơn vị phát hành"
    };

    protected override KyHieuQuyenBienLaiResponse MapToResponse(KyHieuQuyenBienLai entity)
    {
        var res = new KyHieuQuyenBienLaiResponse
        {
            TuSo = entity.TuSo,
            DenSo = entity.DenSo,
            SoHienTai = entity.SoHienTai,
            NamPhatHanh = entity.NamPhatHanh,
            DonViPhatHanh = entity.DonViPhatHanh
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetKyHieuQuyenBienLaiRequest request, KyHieuQuyenBienLai entity)
    {
        entity.TuSo = request.TuSo;
        entity.DenSo = request.DenSo;
        entity.SoHienTai = request.SoHienTai;
        entity.NamPhatHanh = request.NamPhatHanh;
        entity.DonViPhatHanh = request.DonViPhatHanh?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, KyHieuQuyenBienLai entity)
    {
        if (long.TryParse(ws.Cells[row, 6].Text?.Trim(), out var tu)) entity.TuSo = tu;
        if (long.TryParse(ws.Cells[row, 7].Text?.Trim(), out var den)) entity.DenSo = den;
        if (long.TryParse(ws.Cells[row, 8].Text?.Trim(), out var hienTai)) entity.SoHienTai = hienTai;
        if (int.TryParse(ws.Cells[row, 9].Text?.Trim(), out var nam)) entity.NamPhatHanh = nam;
        entity.DonViPhatHanh = ws.Cells[row, 10].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, KyHieuQuyenBienLai entity)
    {
        ws.Cells[row, 6].Value = entity.TuSo;
        ws.Cells[row, 7].Value = entity.DenSo;
        ws.Cells[row, 8].Value = entity.SoHienTai;
        ws.Cells[row, 9].Value = entity.NamPhatHanh;
        ws.Cells[row, 10].Value = entity.DonViPhatHanh;
    }
}

#endregion

#region 8. Dịch vụ Tính chất đợt nhập (tinh_chat_dot_nhap)

public class TinhChatDotNhapService : BaseDanhMucService<TinhChatDotNhap, TinhChatDotNhapResponse, SetTinhChatDotNhapRequest>, ITinhChatDotNhapService
{
    public TinhChatDotNhapService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<TinhChatDotNhapService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Tính chất đợt nhập";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã tính chất", "Tên tính chất đợt nhập", "Mô tả", "Thứ tự", "Trạng thái", "Phân loại đợt nhập"
    };

    protected override TinhChatDotNhapResponse MapToResponse(TinhChatDotNhap entity)
    {
        var res = new TinhChatDotNhapResponse
        {
            LoaiDotNhap = entity.LoaiDotNhap
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetTinhChatDotNhapRequest request, TinhChatDotNhap entity)
    {
        entity.LoaiDotNhap = request.LoaiDotNhap?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, TinhChatDotNhap entity)
    {
        entity.LoaiDotNhap = ws.Cells[row, 6].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, TinhChatDotNhap entity)
    {
        ws.Cells[row, 6].Value = entity.LoaiDotNhap;
    }
}

#endregion

#region 9. Dịch vụ Gói thuê bao (goi_thue_bao)

public class GoiThueBaoService : BaseDanhMucService<GoiThueBao, GoiThueBaoResponse, SetGoiThueBaoRequest>, IGoiThueBaoService
{
    public GoiThueBaoService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<GoiThueBaoService> logger) 
        : base(dbContext, currentUserService, logger) { }

    /// <summary>
    /// Đối với Gói thuê bao: là danh mục chung hệ thống, không cần lọc theo CustomerId.
    /// </summary>
    public override async Task<IReadOnlyList<GoiThueBaoResponse>> GetAllAsync(
        string? customerId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        var list = await query
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.Ma)
            .ToListAsync(cancellationToken);

        return list.Select(MapToResponse).ToList();
    }

    protected override string GetCatalogTitle() => "Gói thuê bao";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã gói", "Tên gói thuê bao", "Mô tả", "Thứ tự", "Trạng thái", "Giới hạn user"
    };

    protected override GoiThueBaoResponse MapToResponse(GoiThueBao entity)
    {
        var res = new GoiThueBaoResponse
        {
            GioiHanUser = entity.GioiHanUser
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetGoiThueBaoRequest request, GoiThueBao entity)
    {
        entity.GioiHanUser = request.GioiHanUser;
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, GoiThueBao entity)
    {
        if (int.TryParse(ws.Cells[row, 6].Text?.Trim(), out var maxUser))
        {
            entity.GioiHanUser = maxUser;
        }
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, GoiThueBao entity)
    {
        ws.Cells[row, 6].Value = entity.GioiHanUser;
    }
}

#endregion

#region 10. Dịch vụ Mức thu lệ phí (muc_thu_le_phi)

public class MucThuLePhiService : BaseDanhMucService<MucThuLePhi, MucThuLePhiResponse, SetMucThuLePhiRequest>, IMucThuLePhiService
{
    public MucThuLePhiService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<MucThuLePhiService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Mức thu lệ phí";

    protected override IQueryable<MucThuLePhi> GetBaseQuery()
    {
        return DbSet.AsNoTracking()
            .Include(x => x.ChiTietMucThuKhuVuc);
    }

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã mức thu", "Nội dung thu lệ phí", "Mô tả", "Thứ tự", "Trạng thái",
        "Mã loại xe", "Tên loại xe", "Mã loại thủ tục", "Tên loại thủ tục",
        "Khoảng trị giá xe", "Căn cứ pháp lý", "Bắt buộc chọn trị giá xe", "Ghi chú hướng dẫn nghiệp vụ", "Chi tiết mức thu theo khu vực"
    };

    public override async Task<MucThuLePhiResponse> SetAsync(SetMucThuLePhiRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Ten) && !string.IsNullOrWhiteSpace(request.NoiDungThuLePhi))
        {
            request.Ten = request.NoiDungThuLePhi.Trim();
        }

        if (string.IsNullOrWhiteSpace(request.Ma))
        {
            request.Ma = $"MTLP_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(100, 999)}";
        }

        // Tự động điền NhomXe / PhanLoaiThuTuc nếu client truyền ID loại xe hoặc loại thủ tục
        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            var loaiXe = await DbContext.LoaiXes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.LoaiXeId, cancellationToken);
            if (loaiXe != null && string.IsNullOrWhiteSpace(request.NhomXe))
            {
                request.NhomXe = loaiXe.Ten;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiThuTucId))
        {
            var loaiThuTuc = await DbContext.LoaiThuTucs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.LoaiThuTucId, cancellationToken);
            if (loaiThuTuc != null && string.IsNullOrWhiteSpace(request.PhanLoaiThuTuc))
            {
                request.PhanLoaiThuTuc = loaiThuTuc.Ten;
            }
        }

        var currentUserId = CurrentUserService.UserId;
        var customerId = request.CustomerId;

        if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(currentUserId))
        {
            customerId = await DbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId && u.IsActive)
                .Select(u => u.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var normalizedMa = request.Ma.Trim();

        MucThuLePhi? entity = null;
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            entity = await DbSet
                .Include(x => x.ChiTietMucThuKhuVuc)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        }

        // Kiểm tra trùng mã trong cùng 1 Customer
        var duplicateQuery = DbSet.Where(x => x.Ma.ToLower() == normalizedMa.ToLower() && x.CustomerId == customerId);
        if (entity != null)
        {
            duplicateQuery = duplicateQuery.Where(x => x.Id != entity.Id);
        }

        if (await duplicateQuery.AnyAsync(cancellationToken))
        {
            throw new BadHttpRequestException($"Mã danh mục '{normalizedMa}' đã tồn tại trong đơn vị.");
        }

        if (entity != null)
        {
            // Cập nhật bản ghi hiện tại
            entity.CustomerId = customerId;
            entity.Ma = normalizedMa;
            entity.Ten = request.Ten.Trim();
            entity.MoTa = request.MoTa?.Trim();
            entity.ThuTu = request.ThuTu;
            entity.IsActive = request.IsActive;

            entity.LoaiXeId = request.LoaiXeId;
            entity.NhomXe = request.NhomXe?.Trim();
            entity.LoaiThuTucId = request.LoaiThuTucId;
            entity.PhanLoaiThuTuc = request.PhanLoaiThuTuc?.Trim();
            entity.KhoangTriGiaXe = request.KhoangTriGiaXe?.Trim();
            entity.CanCuPhapLy = request.CanCuPhapLy?.Trim();
            entity.BatBuocChonTriGiaXe = request.BatBuocChonTriGiaXe;
            entity.GhiChuHuongDan = request.GhiChuHuongDan?.Trim();

            // Xóa chi tiết cũ và thay bằng danh sách mới
            var oldDetails = await DbContext.MucThuLePhiKhuVucs
                .Where(x => x.MucThuLePhiId == entity.Id)
                .ToListAsync(cancellationToken);
            DbContext.MucThuLePhiKhuVucs.RemoveRange(oldDetails);

            if (request.ChiTietMucThuKhuVuc != null && request.ChiTietMucThuKhuVuc.Count > 0)
            {
                foreach (var item in request.ChiTietMucThuKhuVuc)
                {
                    if (!string.IsNullOrWhiteSpace(item.KhuVucApDungId))
                    {
                        entity.ChiTietMucThuKhuVuc.Add(new MucThuLePhiKhuVuc
                        {
                            MucThuLePhiId = entity.Id ?? string.Empty,
                            KhuVucApDungId = item.KhuVucApDungId,
                            MucThu = item.MucThu,
                            GhiChu = item.GhiChu?.Trim()
                        });
                    }
                }
            }

            Logger.LogInformation("Cập nhật danh mục MucThuLePhi Id={Id}", entity.Id);
        }
        else
        {
            // Thêm mới bản ghi
            entity = new MucThuLePhi
            {
                CustomerId = customerId,
                Ma = normalizedMa,
                Ten = request.Ten.Trim(),
                MoTa = request.MoTa?.Trim(),
                ThuTu = request.ThuTu,
                IsActive = request.IsActive,
                LoaiXeId = request.LoaiXeId,
                NhomXe = request.NhomXe?.Trim(),
                LoaiThuTucId = request.LoaiThuTucId,
                PhanLoaiThuTuc = request.PhanLoaiThuTuc?.Trim(),
                KhoangTriGiaXe = request.KhoangTriGiaXe?.Trim(),
                CanCuPhapLy = request.CanCuPhapLy?.Trim(),
                BatBuocChonTriGiaXe = request.BatBuocChonTriGiaXe,
                GhiChuHuongDan = request.GhiChuHuongDan?.Trim()
            };

            if (request.ChiTietMucThuKhuVuc != null && request.ChiTietMucThuKhuVuc.Count > 0)
            {
                foreach (var item in request.ChiTietMucThuKhuVuc)
                {
                    if (!string.IsNullOrWhiteSpace(item.KhuVucApDungId))
                    {
                        entity.ChiTietMucThuKhuVuc.Add(new MucThuLePhiKhuVuc
                        {
                            KhuVucApDungId = item.KhuVucApDungId,
                            MucThu = item.MucThu,
                            GhiChu = item.GhiChu?.Trim()
                        });
                    }
                }
            }

            await DbSet.AddAsync(entity, cancellationToken);
            Logger.LogInformation("Tạo mới danh mục MucThuLePhi Ma={Ma} cho Customer={CustomerId}", entity.Ma, customerId);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        // Nạp lại dữ liệu đầy đủ kèm quan hệ để trả về client
        var fullEntity = await GetBaseQuery()
            .FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);

        return MapToResponse(fullEntity ?? entity);
    }

    protected override MucThuLePhiResponse MapToResponse(MucThuLePhi entity)
    {
        var res = new MucThuLePhiResponse
        {
            LoaiXeId = entity.LoaiXeId,

            LoaiThuTucId = entity.LoaiThuTucId,

            KhoangTriGiaXe = entity.KhoangTriGiaXe,
            CanCuPhapLy = entity.CanCuPhapLy,
            BatBuocChonTriGiaXe = entity.BatBuocChonTriGiaXe,
            GhiChuHuongDan = entity.GhiChuHuongDan,
            ChiTietMucThuKhuVuc = entity.ChiTietMucThuKhuVuc.Select(ct => new MucThuKhuVucItemResponse
            {
                Id = ct.Id,
                KhuVucApDungId = ct.KhuVucApDungId,
                MucThu = ct.MucThu,
                GhiChu = ct.GhiChu
            }).ToList()
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetMucThuLePhiRequest request, MucThuLePhi entity)
    {
        if (!string.IsNullOrWhiteSpace(request.NoiDungThuLePhi))
        {
            entity.Ten = request.NoiDungThuLePhi.Trim();
        }
        entity.LoaiXeId = request.LoaiXeId;
        entity.NhomXe = request.NhomXe?.Trim();
        entity.LoaiThuTucId = request.LoaiThuTucId;
        entity.PhanLoaiThuTuc = request.PhanLoaiThuTuc?.Trim();
        entity.KhoangTriGiaXe = request.KhoangTriGiaXe?.Trim();
        entity.CanCuPhapLy = request.CanCuPhapLy?.Trim();
        entity.BatBuocChonTriGiaXe = request.BatBuocChonTriGiaXe;
        entity.GhiChuHuongDan = request.GhiChuHuongDan?.Trim();
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, MucThuLePhi entity)
    {
        var maLoaiXe = ws.Cells[row, 6].Text?.Trim();
        var tenLoaiXe = ws.Cells[row, 7].Text?.Trim();
        if (!string.IsNullOrWhiteSpace(maLoaiXe))
        {
            var lx = DbContext.LoaiXes.AsNoTracking().FirstOrDefault(x => x.Ma.ToLower() == maLoaiXe.ToLower() && x.CustomerId == entity.CustomerId);
            if (lx != null) entity.LoaiXeId = lx.Id;
        }
        else if (!string.IsNullOrWhiteSpace(tenLoaiXe))
        {
            var lx = DbContext.LoaiXes.AsNoTracking().FirstOrDefault(x => x.Ten.ToLower() == tenLoaiXe.ToLower() && x.CustomerId == entity.CustomerId);
            if (lx != null) entity.LoaiXeId = lx.Id;
        }
        entity.NhomXe = tenLoaiXe;

        var maThuTuc = ws.Cells[row, 8].Text?.Trim();
        var tenThuTuc = ws.Cells[row, 9].Text?.Trim();
        if (!string.IsNullOrWhiteSpace(maThuTuc))
        {
            var tt = DbContext.LoaiThuTucs.AsNoTracking().FirstOrDefault(x => x.Ma.ToLower() == maThuTuc.ToLower() && x.CustomerId == entity.CustomerId);
            if (tt != null) entity.LoaiThuTucId = tt.Id;
        }
        else if (!string.IsNullOrWhiteSpace(tenThuTuc))
        {
            var tt = DbContext.LoaiThuTucs.AsNoTracking().FirstOrDefault(x => x.Ten.ToLower() == tenThuTuc.ToLower() && x.CustomerId == entity.CustomerId);
            if (tt != null) entity.LoaiThuTucId = tt.Id;
        }
        entity.PhanLoaiThuTuc = tenThuTuc;

        entity.KhoangTriGiaXe = ws.Cells[row, 10].Text?.Trim();
        entity.CanCuPhapLy = ws.Cells[row, 11].Text?.Trim();
        var bbText = ws.Cells[row, 12].Text?.Trim().ToLower();
        entity.BatBuocChonTriGiaXe = bbText == "1" || bbText == "true" || bbText == "có";
        entity.GhiChuHuongDan = ws.Cells[row, 13].Text?.Trim();
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, MucThuLePhi entity)
    {
      
        ws.Cells[row, 10].Value = entity.KhoangTriGiaXe;
        ws.Cells[row, 11].Value = entity.CanCuPhapLy;
        ws.Cells[row, 12].Value = entity.BatBuocChonTriGiaXe ? "Có" : "Không";
        ws.Cells[row, 13].Value = entity.GhiChuHuongDan;
        //ws.Cells[row, 14].Value = string.Join("; ", entity.ChiTietMucThuKhuVuc.Select(x => $"{(x.KhuVucApDung != null ? x.KhuVucApDung.Ten : x.KhuVucApDungId)}: {x.MucThu:N0} VNĐ"));
    }
}

#endregion

#region 12. Dịch vụ Cấp bậc (CapBac)

public class CapBacService : BaseDanhMucService<CapBac, CapBacResponse, SetCapBacRequest>, ICapBacService
{
    public CapBacService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<CapBacService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Cấp bậc";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã cấp bậc", "Tên cấp bậc", "Mô tả", "Thứ tự", "Trạng thái", "Nhóm cấp bậc", "Hệ số tham chiếu"
    };

    protected override CapBacResponse MapToResponse(CapBac entity)
    {
        var res = new CapBacResponse
        {
            NhomCapBac = entity.NhomCapBac,
            HeSoThamChieu = entity.HeSoThamChieu
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetCapBacRequest request, CapBac entity)
    {
        entity.NhomCapBac = request.NhomCapBac?.Trim();
        entity.HeSoThamChieu = request.HeSoThamChieu;
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, CapBac entity)
    {
        entity.NhomCapBac = ws.Cells[row, 6].Text?.Trim();
        if (double.TryParse(ws.Cells[row, 7].Text?.Trim(), out var heSo))
        {
            entity.HeSoThamChieu = heSo;
        }
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, CapBac entity)
    {
        ws.Cells[row, 6].Value = entity.NhomCapBac;
        ws.Cells[row, 7].Value = entity.HeSoThamChieu;
    }
}

#endregion

#region 13. Dịch vụ Vai trò phân quyền (VaiTroPhanQuyen)

public class VaiTroPhanQuyenService : BaseDanhMucService<VaiTroPhanQuyen, VaiTroPhanQuyenResponse, SetVaiTroPhanQuyenRequest>, IVaiTroPhanQuyenService
{
    public VaiTroPhanQuyenService(PostgresDbContext dbContext, ICurrentUserService currentUserService, ILogger<VaiTroPhanQuyenService> logger) 
        : base(dbContext, currentUserService, logger) { }

    protected override string GetCatalogTitle() => "Vai trò phân quyền";

    protected override List<string> GetExcelHeaders() => new()
    {
        "Mã vai trò", "Tên vai trò", "Mô tả", "Thứ tự", "Trạng thái", "Quyền hạn", "Quản trị tối cao (IsAdmin)"
    };

    protected override VaiTroPhanQuyenResponse MapToResponse(VaiTroPhanQuyen entity)
    {
        var res = new VaiTroPhanQuyenResponse
        {
            QuyenHan = entity.QuyenHan,
            IsAdmin = entity.IsAdmin
        };
        PopulateBaseResponse(entity, res);
        return res;
    }

    protected override void MapSetSpecific(SetVaiTroPhanQuyenRequest request, VaiTroPhanQuyen entity)
    {
        entity.QuyenHan = request.QuyenHan?.Trim();
        entity.IsAdmin = request.IsAdmin;
    }

    protected override void ReadSpecificExcelColumns(ExcelWorksheet ws, int row, VaiTroPhanQuyen entity)
    {
        entity.QuyenHan = ws.Cells[row, 6].Text?.Trim();
        var adminText = ws.Cells[row, 7].Text?.Trim().ToLower();
        entity.IsAdmin = adminText == "1" || adminText == "true" || adminText == "có";
    }

    protected override void WriteSpecificExcelColumns(ExcelWorksheet ws, int row, VaiTroPhanQuyen entity)
    {
        ws.Cells[row, 6].Value = entity.QuyenHan;
        ws.Cells[row, 7].Value = entity.IsAdmin ? "Có" : "Không";
    }
}

#endregion


using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ danh mục chuẩn hóa:
/// 1. Lấy toàn bộ danh sách không phân trang theo CustomerId (với GoiThueBao không cần truyền).
/// 2. Lấy danh sách phân trang, tìm kiếm theo keyword, Customer của user, sắp xếp theo ThuTu, Ma.
/// 3. Set (vừa thêm mới vừa cập nhật).
/// 4. Delete theo listId danh mục.
/// 5. Import Excel (FileName, EPPlus 6.0.8).
/// 6. Export Excel nhiều dữ liệu (EPPlus 6.0.8).
/// </summary>
public interface IBaseDanhMucService<TResponse, in TSetRequest>
{
    /// <summary>
    /// Lấy toàn bộ danh sách không phân trang, chỉ lọc theo CustomerId (với GoiThueBao không cần truyền).
    /// </summary>
    Task<IReadOnlyList<TResponse>> GetAllAsync(string? customerId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<TResponse>> GetPagedListAsync(PagedDanhMucRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> SetAsync(TSetRequest request, CancellationToken cancellationToken = default);
    Task<int> DeleteMultipleAsync(List<string> ids, CancellationToken cancellationToken = default);
    Task<ImportResult> ImportExcelAsync(string fileName, string? customerId = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportExcelAsync(ExportDanhMucRequest request, CancellationToken cancellationToken = default);
}

public interface IDauBienSoService : IBaseDanhMucService<DauBienSoResponse, SetDauBienSoRequest>
{
}

public interface IMauBienSoService : IBaseDanhMucService<MauBienSoResponse, SetMauBienSoRequest>
{
}

public interface ITrangThaiBienSoService : IBaseDanhMucService<TrangThaiBienSoResponse, SetTrangThaiBienSoRequest>
{
}

public interface ILoaiXeService : IBaseDanhMucService<LoaiXeResponse, SetLoaiXeRequest>
{
}

public interface ILoaiThuTucService : IBaseDanhMucService<LoaiThuTucResponse, SetLoaiThuTucRequest>
{
}

public interface IKhuVucApDungService : IBaseDanhMucService<KhuVucApDungResponse, SetKhuVucApDungRequest>
{
}

public interface IKyHieuQuyenBienLaiService : IBaseDanhMucService<KyHieuQuyenBienLaiResponse, SetKyHieuQuyenBienLaiRequest>
{
}

public interface ITinhChatDotNhapService : IBaseDanhMucService<TinhChatDotNhapResponse, SetTinhChatDotNhapRequest>
{
}

public interface IGoiThueBaoService : IBaseDanhMucService<GoiThueBaoResponse, SetGoiThueBaoRequest>
{
}

public interface IMucThuLePhiService : IBaseDanhMucService<MucThuLePhiResponse, SetMucThuLePhiRequest>
{
}

public interface ICapBacService : IBaseDanhMucService<CapBacResponse, SetCapBacRequest>
{
}

public interface IVaiTroPhanQuyenService : IBaseDanhMucService<VaiTroPhanQuyenResponse, SetVaiTroPhanQuyenRequest>
{
}




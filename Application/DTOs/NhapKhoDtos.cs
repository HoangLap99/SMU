using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

/// <summary>
/// Yêu cầu Thêm mới và Update đợt nhập kho (POST).
/// Nếu update và DaNhapKho == true thì sẽ lưu danh sách biển số vào bảng KhoBienSo (bỏ qua biển đấu giá LaBienDauGia == true).
/// </summary>
public class SetDotNhapKhoRequest
{
    /// <summary>
    /// ID đợt nhập kho (nếu truyền thì Cập nhật phiếu đang có, nếu null thì Thêm mới).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị (nếu null sẽ lấy từ người dùng hiện tại).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Mã đợt nhập (Ví dụ: "NK-2026-08-01-DAU").
    /// </summary>
    [Required(ErrorMessage = "Mã đợt nhập là bắt buộc")]
    [StringLength(100)]
    public string MaDotNhap { get; set; } = string.Empty;

    /// <summary>
    /// Ngày nhập kho.
    /// </summary>
    public DateTime NgayNhapKho { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID Danh mục Loại xe (chọn từ danh mục LoaiXe).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Loại xe")]
    public string LoaiXeId { get; set; } = string.Empty;

    /// <summary>
    /// ID Danh mục Màu biển số (chọn từ danh mục MauBienSo).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Màu biển số")]
    public string MauBienSoId { get; set; } = string.Empty;

    /// <summary>
    /// Ký hiệu dải đầu biển số (droplist chọn nhiều từ danh mục DauBienSo, ví dụ: ["30B", "29RM", "29E"]).
    /// </summary>
    public List<string> KyHieuDaiDauBien { get; set; } = new();

    /// <summary>
    /// Cán bộ tiếp nhận đợt nhập (Ví dụ: "Trung Tá - Mai Cao").
    /// </summary>
    public string? CanBoTiepNhan { get; set; }

    /// <summary>
    /// ID Danh mục Tính chất đợt nhập (chọn từ danh mục TinhChatDotNhap, ví dụ: "Khai báo tồn đầu").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Tính chất đợt nhập")]
    public string TinhChatDotNhapId { get; set; } = string.Empty;

    /// <summary>
    /// Số công văn / Căn cứ nhập kho (Ví dụ: "BB-TK-0108/CAP").
    /// </summary>
    public string? SoCongVan { get; set; }

    /// <summary>
    /// Ghi chú nghiệp vụ.
    /// </summary>
    public string? GhiChu { get; set; }

    /// <summary>
    /// Trạng thái đã nhập kho hay chưa. Nếu update và DaNhapKho == true thì sẽ lưu vào bảng KhoBienSo.
    /// </summary>
    public bool DaNhapKho { get; set; } = false;

    /// <summary>
    /// Danh sách biển số chi tiết riêng của đợt nhập kho này.
    /// </summary>
    public List<BienSoChiTietItemRequest>? DanhSachBienSo { get; set; }

    #region Tùy chọn sinh dải số tự động (nếu không truyền danh sách cụ thể)

    /// <summary>
    /// Tiền tố dải số (Ví dụ: "30B").
    /// </summary>
    public string? TienToDaiSo { get; set; }

    /// <summary>
    /// Từ số (Ví dụ: 20700).
    /// </summary>
    public int? TuSo { get; set; }

    /// <summary>
    /// Đến số (Ví dụ: 20799).
    /// </summary>
    public int? DenSo { get; set; }

    #endregion
}

/// <summary>
/// Alias tương thích ngược cho SetDotNhapKhoRequest.
/// </summary>
public class GhiLaiDotNhapKhoRequest : SetDotNhapKhoRequest
{
}

/// <summary>
/// Yêu cầu từng biển số khi thêm mới hoặc cập nhật đợt nhập.
/// </summary>
public class BienSoChiTietItemRequest
{
    public string? Id { get; set; }
    public int ThuTu { get; set; } = 1;

    [Required]
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// ID Trạng thái biển số (lấy từ Danh mục TrangThaiBienSo).
    /// </summary>
    public string? TrangThaiBienSoId { get; set; }
    public string? TrangThaiBienSoTen { get; set; }
    public string? MauSacHienThi { get; set; }

    /// <summary>
    /// Đánh dấu biển số có đem đấu giá hay không (true: Đem đấu giá; false: Không đem đấu giá / Biển bấm ngẫu nhiên).
    /// </summary>
    public bool LaBienDauGia { get; set; } = false;

    public string? GhiChu { get; set; }
}

/// <summary>
/// Yêu cầu lấy danh sách đợt nhập kho có phân trang và bộ lọc (POST).
/// </summary>
public class PagedDotNhapKhoRequest
{
    public int CurrentPage { get; set; } = 1;
    public int PageIndex { get => CurrentPage; set => CurrentPage = value; }
    public int PageSize { get; set; } = 20;
    //public string? KeyWord { get; set; }
    public string? Keyword { get; set;  }
    public string? CustomerId { get; set; }
    public string? LoaiXeId { get; set; }
    public string? MauBienSoId { get; set; }
    public string? TinhChatDotNhapId { get; set; }
    public bool? DaNhapKho { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Alias tương thích ngược cho PagedDotNhapKhoRequest.
/// </summary>
public class FilterDotNhapKhoRequest : PagedDotNhapKhoRequest
{
}

/// <summary>
/// Thông tin chi tiết một đợt nhập kho biển số xe.
/// </summary>
public class DotNhapKhoResponse
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string MaDotNhap { get; set; } = string.Empty;
    public DateTime NgayNhapKho { get; set; }
    
    public string? LoaiXeId { get; set; }
    public string? LoaiXeTen { get; set; }

    public string? MauBienSoId { get; set; }
    public string? MauBienSoTen { get; set; }
    public string? MauBienSoHex { get; set; }

    /// <summary>
    /// Ký hiệu dải đầu biển số (droplist chọn nhiều từ danh mục DauBienSo).
    /// </summary>
    public List<string> KyHieuDaiDauBien { get; set; } = new();

    /// <summary>
    /// Chuỗi hiển thị các đầu biển số (Ví dụ: "30B, 29RM, 29E").
    /// </summary>
    public string KyHieuDaiDauBienHienThi => KyHieuDaiDauBien.Count > 0 ? string.Join(", ", KyHieuDaiDauBien) : string.Empty;

    public string? CanBoTiepNhan { get; set; }
    
    public string? TinhChatDotNhapId { get; set; }
    public string? TinhChatDotNhapTen { get; set; }

    public string? SoCongVan { get; set; }
    public string? GhiChu { get; set; }

    public int TongSoBien { get; set; }
    public int SoBienDuocBam { get; set; }
    public int SoBienDaBam { get; set; }
    public int SoBienChoDauGia { get; set; }

    /// <summary>
    /// Trạng thái phiếu: "LuuTam" (Bản nháp) hoặc "DaNhapKho" (Đã nhập kho).
    /// </summary>
    public string TrangThai { get; set; } = "LuuTam";
    public string TrangThaiPhieuTen { get; set; } = "Bản nháp";

    /// <summary>
    /// Đánh dấu đã nhập kho thành công hay chưa.
    /// </summary>
    public bool DaNhapKho { get; set; } = false;

    public DateTime? NgayNhapKhoThucTe { get; set; }
    public string? NguoiNhapKho { get; set; }

    public DateTime Created { get; set; }
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Danh sách các biển số xe thuộc đợt nhập này.
    /// </summary>
    public List<KhoBienSoItemResponse> DanhSachBienSo { get; set; } = new();
}

/// <summary>
/// Thông tin từng biển số xe trong đợt nhập kho / kho lưu trữ.
/// </summary>
public class KhoBienSoItemResponse
{
    public string Id { get; set; } = string.Empty;
    public int STT { get; set; }
    public string BienKiemSoat { get; set; } = string.Empty;
    public string? TrangThaiBienSoId { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string TrangThaiTen { get; set; } = string.Empty;
    public string? MauSacHienThi { get; set; }

    /// <summary>
    /// Đánh dấu biển số có đem đấu giá hay không (true: Đem đấu giá; false: Không đem đấu giá / Biển bấm ngẫu nhiên).
    /// </summary>
    public bool LaBienDauGia { get; set; } = false;

    public string? GhiChu { get; set; }
    public DateTime NgayNhapKho { get; set; }
    public DateTime? Modified { get; set; }
    public bool DaLuuKho { get; set; } = false;
}

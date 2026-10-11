using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Biển số xe lưu trong kho (Vật tư biển số tồn kho).
/// Bảng lưu trữ: KhoBienSo
/// </summary>
[Table("KhoBienSo")]
public class KhoBienSo : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị sở hữu kho.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// ID Đợt nhập kho tương ứng.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string DotNhapKhoId { get; set; } = string.Empty;

    /// <summary>
    /// ID Dòng chi tiết biển số trong phiếu đợt nhập tương ứng.
    /// </summary>
    [StringLength(36)]
    public string? ChiTietDotNhapBienSoId { get; set; }

    /// <summary>
    /// ID Danh mục Trạng thái biển số (lấy từ Danh mục TrangThaiBienSo).
    /// </summary>
    [StringLength(36)]
    public string? TrangThaiBienSoId { get; set; }

    /// <summary>
    /// Số thứ tự biển số trong đợt nhập (1, 2, 3...).
    /// </summary>
    public int ThuTu { get; set; } = 1;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Ví dụ: "30B-207.66", "29A-123.45").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái của biển số:
    /// - "DuocBam": Biển được bấm (Tồn kho)
    /// - "ChoDauGia": Chờ đấu giá (Kho đấu giá)
    /// - "DaBam": Đã bấm (Đã cấp)
    /// - "ThuHoi": Đã thu hồi
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TrangThai { get; set; } = "DuocBam";

    /// <summary>
    /// Tên hiển thị của trạng thái (Ví dụ: "Biển được bấm (Tồn kho)").
    /// </summary>
    [StringLength(100)]
    public string TrangThaiTen { get; set; } = "Biển được bấm (Tồn kho)";

    /// <summary>
    /// ID Danh mục Loại xe.
    /// </summary>
    [StringLength(36)]
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// ID Danh mục Màu biển số.
    /// </summary>
    [StringLength(36)]
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// ID Danh mục Đầu biển số tương ứng.
    /// </summary>
    [StringLength(36)]
    public string? DauBienSoId { get; set; }

    /// <summary>
    /// Đánh dấu biển số có đem đấu giá hay không (true: Đem đấu giá; false: Không đem đấu giá / Biển bấm ngẫu nhiên).
    /// </summary>
    public bool LaBienDauGia { get; set; } = false;

    /// <summary>
    /// Ghi chú nghiệp vụ / lịch sử biến động của biển số.
    /// </summary>
    [StringLength(2000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Ngày nhập vào kho.
    /// </summary>
    public DateTime NgayNhapKho { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Đánh dấu biển số đã được xuất kho hay chưa.
    /// </summary>
    public bool DaXuatKho { get; set; } = false;

    /// <summary>
    /// Ngày thực hiện xuất kho.
    /// </summary>
    public DateTime? NgayXuatKho { get; set; }

    /// <summary>
    /// ID phiếu xuất kho tương ứng (tham chiếu tới XuatKhoBienSo).
    /// </summary>
    [StringLength(36)]
    public string? XuatKhoId { get; set; }
}


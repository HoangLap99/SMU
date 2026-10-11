using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Chi tiết danh sách biển số riêng của từng đợt nhập kho (lưu ở bước "Ghi lại").
/// Bảng lưu trữ: ChiTietDotNhapBienSo
/// </summary>
[Table("ChiTietDotNhapBienSo")]
public class ChiTietDotNhapBienSo : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị sở hữu.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// ID Phiếu đợt nhập kho cha.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string DotNhapKhoId { get; set; } = string.Empty;

    /// <summary>
    /// Số thứ tự dòng biển số trong đợt nhập (1, 2, 3...).
    /// </summary>
    public int ThuTu { get; set; } = 1;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Ví dụ: "30B-207.66").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// ID trạng thái biển số (chọn từ Danh mục TrangThaiBienSo).
    /// </summary>
    [StringLength(36)]
    public string? TrangThaiBienSoId { get; set; }

    /// <summary>
    /// Tên hiển thị trạng thái lấy từ Danh mục TrangThaiBienSo (Ví dụ: "Biển được bấm (Tồn kho)", "Chờ đấu giá").
    /// </summary>
    [StringLength(100)]
    public string TrangThaiBienSoTen { get; set; } = "Biển được bấm (Tồn kho)";

    /// <summary>
    /// Mã màu trạng thái lấy từ Danh mục TrangThaiBienSo (Ví dụ: "success", "warning").
    /// </summary>
    [StringLength(50)]
    public string? MauSacHienThi { get; set; } = "success";

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
    /// Ghi chú nghiệp vụ riêng cho biển số này.
    /// </summary>
    [StringLength(2000)]
    public string? GhiChu { get; set; }
}

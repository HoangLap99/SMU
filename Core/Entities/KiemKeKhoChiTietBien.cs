using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Chi tiết trạng thái kiểm kê từng biển số (Có mặt / Báo thiếu / Phát hiện thừa).
/// Bảng lưu trữ: KiemKeKhoChiTietBien
/// </summary>
[Table("KiemKeKhoChiTietBien")]
public class KiemKeKhoChiTietBien : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// ID Phiếu kiểm kê kho tương ứng.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string KiemKeKhoId { get; set; } = string.Empty;

    /// <summary>
    /// ID Dòng nhóm dải đầu biển (nếu có).
    /// </summary>
    [StringLength(36)]
    public string? KiemKeKhoNhomDauBienId { get; set; }

    /// <summary>
    /// Ký hiệu dải đầu biển (Ví dụ: "29E-794").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Ví dụ: "29E-794.01", "29E-794.02").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// ID Bản ghi trong bảng KhoBienSo (null nếu là biển phát hiện thừa chưa có trong kho).
    /// </summary>
    [StringLength(36)]
    public string? KhoBienSoId { get; set; }

    /// <summary>
    /// Nguồn gốc biển số:
    /// - "TrongKho": Biển số đã có sẵn trong bảng KhoBienSo của phần mềm.
    /// - "BienThua": Biển số phát hiện thừa thực tế (chưa có trong phần mềm).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LoaiBien { get; set; } = "TrongKho";

    /// <summary>
    /// Trạng thái kiểm đếm của biển số:
    /// - "CoMat": Có mặt trong kho (khớp).
    /// - "Thieu": Biển trong sổ sách nhưng kiểm đếm báo thiếu.
    /// - "Thua": Biển phát hiện thừa thực tế.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TrangThaiKiemKe { get; set; } = "CoMat";

    /// <summary>
    /// Ghi chú chi tiết cho biển số này (Ví dụ: "Thiếu biển vật lý", "Phát hiện thừa trong thùng").
    /// </summary>
    [StringLength(1000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Navigation property tới phiếu kiểm kê cha.
    /// </summary>
    [ForeignKey("KiemKeKhoId")]
    public virtual KiemKeKho? KiemKeKho { get; set; }
}

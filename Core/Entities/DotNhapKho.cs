using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Đợt nhập kho biển số xe (Vật tư quản lý kho).
/// Bảng lưu trữ: DotNhapKho
/// </summary>
[Table("DotNhapKho")]
public class DotNhapKho : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị sở hữu kho.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Mã đợt nhập (Ví dụ: "NK-2026-08-01-DAU").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string MaDotNhap { get; set; } = string.Empty;

    /// <summary>
    /// Ngày nhập kho.
    /// </summary>
    public DateTime NgayNhapKho { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID Danh mục Loại xe (chọn từ danh mục LoaiXe).
    /// </summary>
    [StringLength(36)]
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// ID Danh mục Màu biển số (chọn từ danh mục MauBienSo).
    /// </summary>
    [StringLength(36)]
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Ký hiệu dải đầu biển số (droplist chọn nhiều từ danh mục DauBienSo, lưu dạng JSON chuỗi các đầu biển được chọn).
    /// Ví dụ: ["30B", "29RM", "29E"].
    /// </summary>
    [StringLength(1000)]
    public string? KyHieuDaiDauBien { get; set; }

    /// <summary>
    /// Cán bộ tiếp nhận đợt nhập (Ví dụ: "Trung Tá - Mai Cao").
    /// </summary>
    [StringLength(250)]
    public string? CanBoTiepNhan { get; set; }

    /// <summary>
    /// ID Danh mục Tính chất đợt nhập (chọn từ danh mục TinhChatDotNhap, ví dụ: "Khai báo tồn đầu").
    /// </summary>
    [StringLength(36)]
    public string? TinhChatDotNhapId { get; set; }

    /// <summary>
    /// Số công văn / Căn cứ nhập kho (Ví dụ: "BB-TK-0108/CAP").
    /// </summary>
    [StringLength(150)]
    public string? SoCongVan { get; set; }

    /// <summary>
    /// Ghi chú nghiệp vụ (Ví dụ: "Khai báo chốt tồn ban đầu chuyển đổi số hóa hệ thống").
    /// </summary>
    [StringLength(1000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Tổng số lượng biển số trong đợt nhập.
    /// </summary>
    public int TongSoBien { get; set; } = 0;

    /// <summary>
    /// Số lượng biển được bấm (tồn kho bấm ngẫu nhiên).
    /// </summary>
    public int SoBienDuocBam { get; set; } = 0;

    /// <summary>
    /// Số lượng biển đã bấm (đã cấp cho phương tiện).
    /// </summary>
    public int SoBienDaBam { get; set; } = 0;

    /// <summary>
    /// Số lượng biển chờ đấu giá (đã chuyển sang kho đấu giá).
    /// </summary>
    public int SoBienChoDauGia { get; set; } = 0;

    /// <summary>
    /// Trạng thái của phiếu đợt nhập kho:
    /// - "LuuTam": Mới ghi lại bản nháp, cho phép chỉnh sửa đợt và danh sách biển số.
    /// - "DaNhapKho": Đã chính thức nhập kho lưu trữ vật tư, KHÓA phiếu không cho chỉnh sửa nữa.
    /// </summary>
    [StringLength(50)]
    public string TrangThai { get; set; } = "LuuTam";

    /// <summary>
    /// Cờ đánh dấu đã nhấn "Nhập kho" (true: Đã nhập kho -> Khóa không cho chỉnh sửa phiếu).
    /// </summary>
    public bool DaNhapKho { get; set; } = false;

    /// <summary>
    /// Thời điểm thực tế nhấn nút "Nhập kho".
    /// </summary>
    public DateTime? NgayNhapKhoThucTe { get; set; }

    /// <summary>
    /// Họ tên hoặc Username người đã thực hiện thao tác "Nhập kho".
    /// </summary>
    [StringLength(250)]
    public string? NguoiNhapKho { get; set; }
}

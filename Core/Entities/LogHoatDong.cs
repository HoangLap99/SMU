using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể lưu trữ Nhật ký hoạt động / Audit Log cho 4 phân hệ cốt lõi:
/// 1. Nhập kho biển số xe (DotNhapKho)
/// 2. Xuất kho biển số xe &amp; Biên lai thu tiền (XuatKhoBienSo)
/// 3. Kiểm kê kho biển số xe (KiemKeKho)
/// 4. Cấp lại đăng ký xe (CapLaiDangKyXe)
/// Bảng lưu trữ: LogHoatDong
/// </summary>
[Table("LogHoatDong")]
public class LogHoatDong : Audit
{
    /// <summary>
    /// Mã định danh Khách hàng / Đơn vị thực hiện thao tác.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Tên Khách hàng / Đơn vị tại thời điểm thực hiện thao tác.
    /// </summary>
    [StringLength(250)]
    public string? CustomerName { get; set; }

    /// <summary>
    /// Phân hệ / Loại nghiệp vụ:
    /// - "NhapKho" (Nhập kho biển số xe)
    /// - "XuatKho" (Xuất kho &amp; Biên lai thu tiền)
    /// - "KiemKe" (Kiểm kê kho biển số)
    /// - "CapLaiDangKy" (Cấp lại đăng ký xe)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LoaiNghiepVu { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị loại nghiệp vụ: "Nhập kho", "Xuất kho", "Kiểm kê kho", "Cấp lại đăng ký xe".
    /// </summary>
    [StringLength(100)]
    public string LoaiNghiepVuTen { get; set; } = string.Empty;

    /// <summary>
    /// Hành động thao tác: "ThemMoi", "CapNhat", "XacNhan", "Huy", "Xoa".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string HanhDong { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị hành động: "Tạo mới đợt nhập", "Cập nhật phiếu xuất kho", "Xác nhận nhập kho", "Hủy phiếu xuất kho", "Xác nhận hoàn tất kiểm kê"...
    /// </summary>
    [StringLength(150)]
    public string HanhDongTen { get; set; } = string.Empty;

    /// <summary>
    /// ID chứng từ liên quan (Id của DotNhapKho, XuatKhoBienSo, KiemKeKho, CapLaiDangKyXe).
    /// </summary>
    [StringLength(36)]
    public string? ChungTuId { get; set; }

    /// <summary>
    /// Mã chứng từ / Số biên lai (Ví dụ: "NK-2026-08-01-DAU", "0000113", "KK-2026-10-10-921").
    /// </summary>
   
    [StringLength(100)]
    public string MaChungTu { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát liên quan (Ví dụ: "30B-207.66", hoặc dải "30B-207.00 - 30B-207.99").
    /// </summary>
    [StringLength(250)]
    public string? BienKiemSoat { get; set; }

    /// <summary>
    /// Tóm tắt nội dung chi tiết của hoạt động nghiệp vụ.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>
    /// Chi tiết thay đổi hoặc dữ liệu bổ sung (JSON hoặc text mô tả).
    /// </summary>
    public string? ChiTietThayDoi { get; set; }

    /// <summary>
    /// Trạng thái chứng từ tại thời điểm ghi log (Ví dụ: "LuuTam", "DaNhapKho", "HoanTat", "DaHuy", "LuuNhap", "DaXacNhan", "DaXoa").
    /// </summary>
    [StringLength(50)]
    public string TrangThai { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị trạng thái chứng từ (Ví dụ: "Bản nháp", "Đã nhập kho", "Hoàn tất", "Đã hủy", "Đã xác nhận", "Đã xóa").
    /// </summary>
    [StringLength(100)]
    public string TrangThaiTen { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm diễn ra hoạt động (UTC).
    /// </summary>
    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Mã định danh người thực hiện thao tác.
    /// </summary>
    [StringLength(36)]
    public string? NguoiThucHienId { get; set; }

    /// <summary>
    /// Tên hoặc Username người dùng thực hiện thao tác.
    /// </summary>
    [StringLength(250)]
    public string? NguoiThucHien { get; set; }

    /// <summary>
    /// Địa chỉ IP của máy khách khi thực hiện thao tác (nếu có).
    /// </summary>
    [StringLength(100)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Ghi chú nghiệp vụ thêm.
    /// </summary>
    [StringLength(1000)]
    public string? GhiChu { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Xuất kho biển số xe: Lập thủ tục &amp; Biên lai thu tiền
/// Quản lý việc xuất 01 biển số từ kho "Biển được bấm", thu lệ phí theo TT 155/2025/TT-BTC và phát hành biên lai.
/// Bảng lưu trữ: XuatKhoBienSo
/// </summary>
[Table("XuatKhoBienSo")]
public class XuatKhoBienSo : Audit
{
    /// <summary>
    /// ID Khách hàng / Đơn vị thực hiện xuất kho.
    /// </summary>
    [StringLength(36)]
    public string? CustomerId { get; set; }

    #region 1. Thông tin thủ tục & Phương tiện

    /// <summary>
    /// ID Danh mục Loại thủ tục thực hiện (LoaiThuTuc, ví dụ: "Cấp biển (Có xuất kho số)").
    /// </summary>
    
    [StringLength(36)]
    public string LoaiThuTucId { get; set; } = string.Empty;

    /// <summary>
    /// Tên loại thủ tục lưu snapshot (Ví dụ: "Cấp biển (Có xuất kho số)").
    /// </summary>
    [StringLength(250)]
    public string? LoaiThuTucTen { get; set; }

    /// <summary>
    /// Ngày thực hiện thủ tục xuất kho.
    /// </summary>
    public DateTime NgayThucHien { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tính chất xuất kho (Ví dụ: "Xuất 01 biển từ kho 'Biển được bấm'").
    /// </summary>
    [StringLength(250)]
    public string TinhChatXuatKho { get; set; } = "Xuất 01 biển từ kho 'Biển được bấm'";

    /// <summary>
    /// ID bản ghi Biển số trong kho (tham chiếu KhoBienSo).
    /// </summary>
    [Required]
    [StringLength(36)]
    public string KhoBienSoId { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh xuất kho (Snapshot, ví dụ: "30B-207.66").
    /// </summary>
    [StringLength(50)]
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// ID Loại xe.
    /// </summary>
    [StringLength(36)]
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Tên Loại xe (Ví dụ: "Ô tô").
    /// </summary>
    [StringLength(100)]
    public string? LoaiXeTen { get; set; }

    /// <summary>
    /// ID Màu biển số.
    /// </summary>
    [StringLength(36)]
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Tên Màu biển số (Ví dụ: "Biển Trắng").
    /// </summary>
    [StringLength(100)]
    public string? MauBienSoTen { get; set; }

    /// <summary>
    /// ID Đầu biển số.
    /// </summary>
    [StringLength(36)]
    public string? DauBienSoId { get; set; }

    /// <summary>
    /// Ký hiệu Đầu biển số (Ví dụ: "30B-207" hoặc "30B").
    /// </summary>
    [StringLength(100)]
    public string? DauBienSoTen { get; set; }

    #endregion

    #region 2. Bảng giá lệ phí theo Thông tư 155/2025/TT-BTC

    /// <summary>
    /// ID Danh mục Mức thu lệ phí (MucThuLePhi).
    /// </summary>
    [StringLength(36)]
    public string MucThuLePhiId { get; set; } = string.Empty;

    /// <summary>
    /// Tên / Nội dung thu lệ phí snapshot (Ví dụ: "[Ô tô] Cấp mới đăng ký kèm biển số Ô tô chở người từ 9 chỗ trở xuống").
    /// </summary>
    [StringLength(500)]
    public string? MucThuLePhiTen { get; set; }

    /// <summary>
    /// ID Danh mục Khu vực áp dụng (KhuVucApDung, ví dụ: "Khu vực I (Hà Nội, TP.HCM)").
    /// </summary>
    [StringLength(36)]
    public string KhuVucApDungId { get; set; } = string.Empty;

    /// <summary>
    /// Tên khu vực áp dụng snapshot.
    /// </summary>
    [StringLength(250)]
    public string? KhuVucApDungTen { get; set; }

    /// <summary>
    /// ID bản ghi Chi tiết mức thu theo khu vực (MucThuLePhiKhuVuc).
    /// </summary>
    [StringLength(36)]
    public string? MucThuLePhiKhuVucId { get; set; }

    /// <summary>
    /// Điều kiện khoảng trị giá xe tính lệ phí đã chọn (Ví dụ: "Trị giá trên 40 triệu đồng (Mức thu: 20.000.000đ)").
    /// </summary>
    [StringLength(250)]
    public string? DieuKienTriGiaXe { get; set; }

    /// <summary>
    /// Căn cứ pháp lý tính lệ phí (Ví dụ: "Thông tư 155/2025/TT-BTC (Khu vực I áp dụng cho TP Hà Nội và TP Hồ Chí Minh)").
    /// </summary>
    [StringLength(500)]
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Số tiền lệ phí tự động điền theo biểu phí (VNĐ).
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal SoTienTuDongDien { get; set; } = 0;

    #endregion

    #region 3. Thông tin biên lai & Người nộp tiền

    /// <summary>
    /// ID Danh mục Ký hiệu quyển biên lai (KyHieuQuyenBienLai).
    /// </summary>
    [StringLength(36)]
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;

    /// <summary>
    /// Ký hiệu quyển biên lai snapshot (Ví dụ: "AA/26E").
    /// </summary>
    [StringLength(50)]
    public string KyHieuQuyen { get; set; } = string.Empty;

    /// <summary>
    /// Số biên lai thu tiền (Ví dụ: "0000113").
    /// </summary>
    [StringLength(50)]
    public string SoBienLai { get; set; } = string.Empty;

    /// <summary>
    /// Ngày lập biên lai.
    /// </summary>
    public DateTime NgayBienLai { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Số tiền thu thực tế trên biên lai (VNĐ).
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal SoTienThu { get; set; } = 0;

    /// <summary>
    /// Họ và tên người nộp tiền (Ví dụ: "Nguyễn Văn An").
    /// </summary>
    [Required]
    [StringLength(250)]
    public string HoTenNguoiNop { get; set; } = string.Empty;

    /// <summary>
    /// Số CCCD / Mã định danh cá nhân của người nộp tiền.
    /// </summary>
    [StringLength(50)]
    public string? SoCccd { get; set; }

    /// <summary>
    /// Mã hồ sơ Dịch vụ công (Ví dụ: "HS-2026-00123").
    /// </summary>
    [StringLength(100)]
    public string? MaHoSoDvc { get; set; }

    /// <summary>
    /// Ghi chú bổ sung (hồ sơ hoặc căn cứ văn bản...).
    /// </summary>
    [StringLength(2000)]
    public string? GhiChu { get; set; }

    /// <summary>
    /// Trạng thái phiếu xuất kho ("HoanTat", "LuuTam", "DaHuy").
    /// </summary>
    [StringLength(50)]
    public string TrangThai { get; set; } = "HoanTat";

    /// <summary>
    /// Đánh dấu đã thực hiện xuất kho hay chưa (true: Đã xuất kho -> Cập nhật KhoBienSo; false: Lưu tạm / Lập thủ tục chưa xuất kho).
    /// </summary>
    public bool DaXuatKho { get; set; } = false;

    #endregion
}

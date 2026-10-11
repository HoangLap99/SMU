using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Ký hiệu quyển biên lai (Ví dụ: BL-01, AA/24P...).
/// Bảng lưu trữ: KyHieuQuyenBienLai
/// </summary>
[Table("KyHieuQuyenBienLai")]
public class KyHieuQuyenBienLai : DanhMucBase
{
    /// <summary>
    /// Số thứ tự bắt đầu của quyển biên lai (Ví dụ: 1).
    /// </summary>
    public long? TuSo { get; set; }

    /// <summary>
    /// Số thứ tự kết thúc của quyển biên lai (Ví dụ: 50).
    /// </summary>
    public long? DenSo { get; set; }

    /// <summary>
    /// Số biên lai hiện tại đang viết/phát hành đến.
    /// </summary>
    public long? SoHienTai { get; set; }

    /// <summary>
    /// Năm phát hành quyển biên lai (Ví dụ: 2026).
    /// </summary>
    public int? NamPhatHanh { get; set; }

    /// <summary>
    /// Đơn vị quản lý và phát hành quyển biên lai.
    /// </summary>
    [StringLength(250)]
    public string? DonViPhatHanh { get; set; }
}

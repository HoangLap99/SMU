using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMU.Core.Entities;

/// <summary>
/// Thực thể Danh mục Mức thu lệ phí (Quy định theo Thông tư 155/2025/TT-BTC của Bộ Tài chính).
/// Bảng lưu trữ: MucThuLePhi
/// </summary>
[Table("MucThuLePhi")]
public class MucThuLePhi : DanhMucBase
{
    /// <summary>
    /// ID tham chiếu tới Danh mục Loại xe (chọn từ danh mục Loại xe).
    /// </summary>
    [StringLength(36)]
    public string? LoaiXeId { get; set; }


    /// <summary>
    /// Tên nhóm xe / loại xe lưu trữ phụ trợ (nếu có).
    /// </summary>
    [StringLength(100)]
    public string? NhomXe { get; set; }

    /// <summary>
    /// ID tham chiếu tới Danh mục Loại thủ tục (chọn từ danh mục Loại thủ tục).
    /// </summary>
    [StringLength(36)]
    public string? LoaiThuTucId { get; set; }


    /// <summary>
    /// Tên phân loại thủ tục lưu trữ phụ trợ (nếu có).
    /// </summary>
    [StringLength(150)]
    public string? PhanLoaiThuTuc { get; set; }

    /// <summary>
    /// Khoảng trị giá xe nếu có (Ví dụ: "Trên 40.000.000 đồng", "Từ 15.000.000 đến 40.000.000 đồng"...).
    /// </summary>
    [StringLength(250)]
    public string? KhoangTriGiaXe { get; set; }

    /// <summary>
    /// Căn cứ pháp lý (Ví dụ: "Thông tư 155/2025/TT-BTC").
    /// </summary>
    [StringLength(250)]
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Bắt buộc cán bộ phải chọn khoảng trị giá xe khi tính tiền.
    /// </summary>
    public bool BatBuocChonTriGiaXe { get; set; } = false;

    /// <summary>
    /// Ghi chú hướng dẫn nghiệp vụ (Ví dụ: "Khu vực I áp dụng cho TP Hà Nội và TP Hồ Chí Minh").
    /// </summary>
    [StringLength(1000)]
    public string? GhiChuHuongDan { get; set; }

    /// <summary>
    /// Bảng chi tiết mức thu theo từng Khu vực áp dụng (1 cột chọn khu vực, 1 cột nhập giá trị).
    /// </summary>
    public List<MucThuLePhiKhuVuc> ChiTietMucThuKhuVuc { get; set; } = new List<MucThuLePhiKhuVuc>();
}

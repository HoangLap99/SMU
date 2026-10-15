using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

#region 1. DTOs Biển số khả dụng trong kho (chưa xuất kho)

/// <summary>
/// Yêu cầu lọc danh sách biển số khả dụng trong kho.
/// </summary>
public class FilterBienSoKhaDungRequest
{
    public string? Keyword { get; set; }
    public string? LoaiXeId { get; set; }
    public string? MauBienSoId { get; set; }
    public string? DauBienSoId { get; set; }
    public string? CustomerId { get; set; }
    public int PageSize { get; set; } = 100;
}

/// <summary>
/// Thông tin từng biển số khả dụng trong kho phục vụ chọn xuất kho.
/// Định dạng hiển thị chuẩn: "30B-207.66 • Ô tô (Biển Trắng - Đầu 30B-207)".
/// </summary>
public class BienSoKhaDungItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string BienKiemSoat { get; set; } = string.Empty;

    public string? LoaiXeId { get; set; }
    public string? LoaiXeTen { get; set; }

    public string? MauBienSoId { get; set; }
    public string? MauBienSoTen { get; set; }

    public string? DauBienSoId { get; set; }
    public string? DauBienSoTen { get; set; }

    /// <summary>
    /// Chuỗi hiển thị định dạng chuẩn trên giao diện:
    /// Ví dụ: "30B-207.66 • Ô tô (Biển Trắng - Đầu 30B-207)"
    /// </summary>
    public string ThongTinHienThi { get; set; } = string.Empty;

    public string TrangThai { get; set; } = "DuocBam";
    public string TrangThaiTen { get; set; } = "Biển được bấm (Tồn kho)";
    public DateTime NgayNhapKho { get; set; }
}

/// <summary>
/// Kết quả trả về danh sách biển số khả dụng và tổng số lượng còn trong kho.
/// </summary>
public class DanhSachBienSoKhaDungResponse
{
    /// <summary>
    /// Tổng số biển khả dụng còn trong kho (Ví dụ: "Còn 591 biển khả dụng trong kho").
    /// </summary>
    public int TongSoBienKhaDung { get; set; }

    /// <summary>
    /// Danh sách chi tiết các biển số khả dụng.
    /// </summary>
    public List<BienSoKhaDungItemResponse> Items { get; set; } = new();
}

#endregion

#region 2. DTOs Mức thu lệ phí theo Khu vực (Thông tư 155/2025/TT-BTC)

/// <summary>
/// Tùy chọn điều kiện khoảng trị giá xe tính lệ phí (nếu có nhiều mức).
/// </summary>
public class DieuKienTriGiaXeOption
{
    public string? MucThuLePhiId { get; set; }
    public string? MucThuLePhiKhuVucId { get; set; }
    public string KhoangTriGiaXe { get; set; } = string.Empty;
    public decimal MucThu { get; set; }

    /// <summary>
    /// Tiêu đề hiển thị trên nút chọn: "Trị giá trên 40 triệu đồng (Mức thu: 20.000.000đ)".
    /// </summary>
    public string TieuDeHienThi { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

/// <summary>
/// Kết quả trả về từ API tra cứu mức thu theo nội dung lệ phí và khu vực áp dụng.
/// </summary>
public class ThongTinMucThuTheoKhuVucResponse
{
    public string MucThuLePhiId { get; set; } = string.Empty;
    public string MucThuLePhiTen { get; set; } = string.Empty;

    public string KhuVucApDungId { get; set; } = string.Empty;
    public string KhuVucApDungTen { get; set; } = string.Empty;

    public string? MucThuLePhiKhuVucId { get; set; }

    /// <summary>
    /// Mức thu lệ phí (VNĐ) ứng với khu vực đã chọn.
    /// </summary>
    public decimal MucThu { get; set; }

    /// <summary>
    /// Căn cứ pháp lý (Ví dụ: "Thông tư 155/2025/TT-BTC").
    /// </summary>
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Ghi chú hướng dẫn nghiệp vụ (Ví dụ: "Khu vực I áp dụng cho TP Hà Nội và TP Hồ Chí Minh").
    /// </summary>
    public string? GhiChuHuongDan { get; set; }

    public bool BatBuocChonTriGiaXe { get; set; }
    public string? KhoangTriGiaXe { get; set; }

    /// <summary>
    /// Số tiền tự động điền (VNĐ).
    /// </summary>
    public decimal SoTienTuDongDien { get; set; }

    /// <summary>
    /// Danh sách các điều kiện trị giá xe nếu mục thu có nhiều mức lựa chọn.
    /// </summary>
    public List<DieuKienTriGiaXeOption> DanhSachDieuKienTriGia { get; set; } = new();
}

#endregion

#region 3. DTOs Ký hiệu quyển biên lai và số biên lai tiếp theo

/// <summary>
/// Kết quả trả về gợi ý số biên lai tiếp theo theo Ký hiệu quyển.
/// </summary>
public class SoBienLaiTiepTheoResponse
{
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;
    public string KyHieuQuyen { get; set; } = string.Empty;
    public long? TuSo { get; set; }
    public long? DenSo { get; set; }
    public long? SoHienTai { get; set; }

    /// <summary>
    /// Số biên lai tiếp theo được format định dạng chuỗi 7 chữ số (Ví dụ: "0000113").
    /// </summary>
    public string SoBienLaiGoiY { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng biên lai còn lại trong quyển.
    /// </summary>
    public long ConLai { get; set; }
}

#endregion

#region 4. DTOs Lập thủ tục &amp; Biên lai xuất kho (Set, Paged, Response)

/// <summary>
/// Yêu cầu Lập thủ tục &amp; Biên lai thu tiền (Xuất kho biển số).
/// </summary>
public class SetXuatKhoRequest
{
    /// <summary>
    /// ID phiếu xuất kho (null nếu thêm mới; có giá trị nếu cập nhật).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị (nếu null sẽ lấy tự động từ tài khoản hiện tại).
    /// </summary>
    public string? CustomerId { get; set; }

    #region 1. Thông tin thủ tục & Phương tiện

    /// <summary>
    /// ID Danh mục Loại thủ tục thực hiện (LoaiThuTuc).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Loại thủ tục thực hiện")]
    public string LoaiThuTucId { get; set; } = string.Empty;

    /// <summary>
    /// Ngày thực hiện thủ tục.
    /// </summary>
    public DateTime NgayThucHien { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tính chất xuất kho.
    /// </summary>
    public string? TinhChatXuatKho { get; set; } = "Xuất 01 biển từ kho 'Biển được bấm'";

    /// <summary>
    /// ID Biển số được chọn từ kho (KhoBienSo).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn 01 biển số từ kho 'Biển được bấm'")]
    public string KhoBienSoId { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Tùy chọn; nếu truyền khi cập nhật sẽ cập nhật đè vào cả XuatKhoBienSo và KhoBienSo).
    /// </summary>
    public string? BienKiemSoat { get; set; }

    /// <summary>
    /// ID Loại xe (Tùy chọn; nếu truyền khi cập nhật sẽ cập nhật đè vào cả XuatKhoBienSo và KhoBienSo).
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// ID Màu biển số (Tùy chọn; nếu truyền khi cập nhật sẽ cập nhật đè vào cả XuatKhoBienSo và KhoBienSo).
    /// </summary>
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// ID Đầu biển số (Tùy chọn; nếu truyền khi cập nhật sẽ cập nhật đè vào cả XuatKhoBienSo và KhoBienSo).
    /// </summary>
    public string? DauBienSoId { get; set; }

    #endregion

    #region 2. Bảng giá lệ phí theo Thông tư 155/2025/TT-BTC

    /// <summary>
    /// ID Danh mục Mức thu lệ phí (MucThuLePhi).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Nội dung thu lệ phí")]
    public string MucThuLePhiId { get; set; } = string.Empty;

    /// <summary>
    /// ID Danh mục Khu vực áp dụng (KhuVucApDung).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Khu vực áp dụng")]
    public string KhuVucApDungId { get; set; } = string.Empty;

    /// <summary>
    /// ID bản ghi Chi tiết mức thu theo khu vực (nếu có).
    /// </summary>
    public string? MucThuLePhiKhuVucId { get; set; }

    /// <summary>
    /// Khoảng trị giá xe đã chọn (Ví dụ: "Trị giá trên 40 triệu đồng (Mức thu: 20.000.000đ)").
    /// </summary>
    public string? DieuKienTriGiaXe { get; set; }

    /// <summary>
    /// Căn cứ pháp lý.
    /// </summary>
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Số tiền tự động điền.
    /// </summary>
    public decimal? SoTienTuDongDien { get; set; }

    #endregion

    #region 3. Thông tin biên lai & Người nộp tiền

    /// <summary>
    /// ID Danh mục Ký hiệu quyển biên lai (KyHieuQuyenBienLai).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Ký hiệu quyển biên lai")]
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;

    /// <summary>
    /// Số biên lai (Ví dụ: "0000113").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập Số biên lai")]
    public string SoBienLai { get; set; } = string.Empty;

    /// <summary>
    /// Ngày biên lai.
    /// </summary>
    public DateTime NgayBienLai { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Số tiền thu (VNĐ).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập Số tiền thu")]
    [Range(0, 1000000000000, ErrorMessage = "Số tiền thu không hợp lệ")]
    public decimal SoTienThu { get; set; }

    /// <summary>
    /// Họ và tên người nộp tiền (Ví dụ: "Nguyễn Văn An").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập Họ và tên người nộp tiền")]
    public string HoTenNguoiNop { get; set; } = string.Empty;

    /// <summary>
    /// Số CCCD / Mã định danh.
    /// </summary>
    public string? SoCccd { get; set; }

    /// <summary>
    /// Mã hồ sơ Dịch vụ công.
    /// </summary>
    public string? MaHoSoDvc { get; set; }

    /// <summary>
    /// Ghi chú bổ sung (tùy chọn).
    /// </summary>
    public string? GhiChu { get; set; }

    /// <summary>
    /// Trạng thái đã xuất kho hay chưa. Nếu DaXuatKho == true thì mới thay đổi dữ liệu của KhoBienSo.
    /// </summary>
    public bool DaXuatKho { get; set; } = false;

    #endregion
}

/// <summary>
/// Yêu cầu hủy phiếu xuất kho &amp; biên lai thu tiền.
/// </summary>
public class HuyPhieuXuatKhoRequest
{
    /// <summary>
    /// Lý do hủy phiếu xuất kho (tùy chọn).
    /// </summary>
    public string? LyDoHuy { get; set; }
}

/// <summary>
/// Yêu cầu lấy danh sách phiếu xuất kho có phân trang và bộ lọc.
/// </summary>
public class PagedXuatKhoRequest
{
    public int CurrentPage { get; set; } = 1;
    public int PageIndex { get => CurrentPage; set => CurrentPage = value; }
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Từ khóa tìm kiếm: Biển số, Họ tên người nộp, CCCD, Mã hồ sơ DVC, Số biên lai.
    /// </summary>
    public string? KeyWord { get; set; }

    public string? CustomerId { get; set; }
    public string? LoaiThuTucId { get; set; }
    public string? KyHieuQuyenBienLaiId { get; set; }
    public bool? DaXuatKho { get; set; }

    /// <summary>
    /// Lọc theo trạng thái phiếu xuất kho ("HoanTat", "LuuTam", "DaHuy"). Để trống lấy tất cả.
    /// </summary>
    public string? TrangThai { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Thông tin chi tiết một phiếu xuất kho &amp; biên lai thu tiền.
/// </summary>
public class XuatKhoResponse
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }

    #region 1. Thông tin thủ tục & Phương tiện
    public string LoaiThuTucId { get; set; } = string.Empty;
    public string? LoaiThuTucTen { get; set; }
    public DateTime NgayThucHien { get; set; }
    public string TinhChatXuatKho { get; set; } = string.Empty;

    public string KhoBienSoId { get; set; } = string.Empty;
    public string BienKiemSoat { get; set; } = string.Empty;

    public string? LoaiXeId { get; set; }
    public string? LoaiXeTen { get; set; }

    public string? MauBienSoId { get; set; }
    public string? MauBienSoTen { get; set; }

    public string? DauBienSoId { get; set; }
    public string? DauBienSoTen { get; set; }

    public string ThongTinBienSoHienThi { get; set; } = string.Empty;
    #endregion

    #region 2. Bảng giá lệ phí
    public string MucThuLePhiId { get; set; } = string.Empty;
    public string? MucThuLePhiTen { get; set; }

    public string KhuVucApDungId { get; set; } = string.Empty;
    public string? KhuVucApDungTen { get; set; }

    public string? MucThuLePhiKhuVucId { get; set; }
    public string? DieuKienTriGiaXe { get; set; }
    public string? CanCuPhapLy { get; set; }
    public decimal SoTienTuDongDien { get; set; }
    #endregion

    #region 3. Thông tin biên lai & Người nộp tiền
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;
    public string KyHieuQuyen { get; set; } = string.Empty;
    public string SoBienLai { get; set; } = string.Empty;
    public DateTime NgayBienLai { get; set; }
    public decimal SoTienThu { get; set; }

    public string HoTenNguoiNop { get; set; } = string.Empty;
    public string? SoCccd { get; set; }
    public string? MaHoSoDvc { get; set; }
    public string? GhiChu { get; set; }
    public string TrangThai { get; set; } = "HoanTat";

    /// <summary>
    /// Tên hiển thị trạng thái tiếng Việt ("Hoàn tất", "Lưu tạm", "Đã hủy").
    /// </summary>
    public string TrangThaiHienThi => TrangThai switch
    {
        "HoanTat" => "Hoàn tất",
        "LuuTam" => "Lưu tạm",
        "DaHuy" => "Đã hủy",
        _ => TrangThai
    };

    public bool DaXuatKho { get; set; }
    #endregion

    public DateTime Created { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? Modified { get; set; }
    public string? ModifileByName { get; set; }
}

#endregion

using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

/// <summary>
/// Yêu cầu tìm kiếm và phân trang danh sách nhật ký hoạt động (POST /api/LogHoatDong/danhsach hoặc GET).
/// </summary>
public class PagedLogHoatDongRequest
{
    public int CurrentPage { get; set; } = 1;
    public int PageIndex { get => CurrentPage; set => CurrentPage = value; }
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Lọc theo Phân hệ / Loại nghiệp vụ:
    /// - "TatCa" hoặc null/rỗng: Tất cả phân hệ
    /// - "NhapKho": Nhập kho biển số
    /// - "XuatKho": Xuất kho &amp; Biên lai thu tiền
    /// - "KiemKe": Kiểm kê kho biển số
    /// - "CapLaiDangKy": Cấp lại đăng ký xe
    /// </summary>
    public string? LoaiNghiepVu { get; set; }

    /// <summary>
    /// Lọc theo Hành động thao tác:
    /// - "TatCa" hoặc null/rỗng: Tất cả hành động
    /// - "ThemMoi": Tạo mới
    /// - "CapNhat": Cập nhật / Chỉnh sửa
    /// - "XacNhan": Xác nhận / Hoàn tất / Phát hành
    /// - "Huy": Hủy phiếu
    /// - "Xoa": Xóa phiếu
    /// </summary>
    public string? HanhDong { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm: Mã chứng từ, số biên lai, biển kiểm soát, người thực hiện, nội dung.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc theo ID chứng từ nghiệp vụ liên quan (Id của DotNhapKho, XuatKhoBienSo, KiemKeKho, CapLaiDangKyXe).
    /// </summary>
    public string? ChungTuId { get; set; }

    /// <summary>
    /// Lọc theo Biển kiểm soát cụ thể (Ví dụ: "30B-207.66").
    /// </summary>
    public string? BienKiemSoat { get; set; }

    /// <summary>
    /// Lọc theo ID Khách hàng / Đơn vị. Nếu để trống sẽ tự động lấy theo đơn vị của người dùng hiện tại (nếu có).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Lọc từ ngày diễn ra hoạt động.
    /// </summary>
    public DateTime? TuNgay { get; set; }

    /// <summary>
    /// Lọc đến ngày diễn ra hoạt động.
    /// </summary>
    public DateTime? DenNgay { get; set; }
}

/// <summary>
/// Alias tương thích cho PagedLogHoatDongRequest.
/// </summary>
public class FilterLogHoatDongRequest : PagedLogHoatDongRequest
{
}

/// <summary>
/// Thông tin chi tiết một bản ghi nhật ký hoạt động / audit log trả về cho Client.
/// </summary>
public class LogHoatDongResponse
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Mã phân hệ / loại nghiệp vụ ("NhapKho", "XuatKho", "KiemKe", "CapLaiDangKy").
    /// </summary>
    public string LoaiNghiepVu { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị loại nghiệp vụ ("Nhập kho", "Xuất kho", "Kiểm kê kho", "Cấp lại đăng ký xe").
    /// </summary>
    public string LoaiNghiepVuTen { get; set; } = string.Empty;

    /// <summary>
    /// Mã hành động ("ThemMoi", "CapNhat", "XacNhan", "Huy", "Xoa").
    /// </summary>
    public string HanhDong { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị hành động ("Tạo mới đợt nhập", "Xác nhận nhập kho", "Lập phiếu xuất kho", "Hủy phiếu xuất kho", ...).
    /// </summary>
    public string HanhDongTen { get; set; } = string.Empty;

    /// <summary>
    /// ID chứng từ liên kết (Id đợt nhập, Id phiếu xuất kho, Id kiểm kê, Id cấp lại).
    /// </summary>
    public string? ChungTuId { get; set; }

    /// <summary>
    /// Mã chứng từ / Số biên lai (Ví dụ: "NK-2026-08-01-DAU", "0000113", "KK-2026-10-10-921").
    /// </summary>
    public string MaChungTu { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát liên quan (nếu có).
    /// </summary>
    public string? BienKiemSoat { get; set; }

    /// <summary>
    /// Tóm tắt nội dung chi tiết thao tác nghiệp vụ.
    /// </summary>
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>
    /// Chi tiết thay đổi hoặc dữ liệu bổ sung.
    /// </summary>
    public string? ChiTietThayDoi { get; set; }

    /// <summary>
    /// Trạng thái chứng từ tại thời điểm log.
    /// </summary>
    public string TrangThai { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị trạng thái chứng từ.
    /// </summary>
    public string TrangThaiTen { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm diễn ra hoạt động.
    /// </summary>
    public DateTime ThoiGian { get; set; }

    /// <summary>
    /// ID người dùng thực hiện.
    /// </summary>
    public string? NguoiThucHienId { get; set; }

    /// <summary>
    /// Tên / Username người dùng thực hiện thao tác.
    /// </summary>
    public string? NguoiThucHien { get; set; }

    /// <summary>
    /// Mã đơn vị / Khách hàng.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Tên đơn vị / Khách hàng.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Địa chỉ IP (nếu có).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Ghi chú bổ sung.
    /// </summary>
    public string? GhiChu { get; set; }
}

/// <summary>
/// Thống kê tổng quan số lượng nhật ký hoạt động.
/// </summary>
public class ThongKeLogHoatDongResponse
{
    /// <summary>
    /// Tổng số lượt thao tác đã ghi log.
    /// </summary>
    public int TongSoLog { get; set; }

    /// <summary>
    /// Số lượng log phân hệ Nhập kho.
    /// </summary>
    public int SoLogNhapKho { get; set; }

    /// <summary>
    /// Số lượng log phân hệ Xuất kho.
    /// </summary>
    public int SoLogXuatKho { get; set; }

    /// <summary>
    /// Số lượng log phân hệ Kiểm kê kho.
    /// </summary>
    public int SoLogKiemKe { get; set; }

    /// <summary>
    /// Số lượng log phân hệ Cấp lại đăng ký xe.
    /// </summary>
    public int SoLogCapLaiDangKy { get; set; }

    /// <summary>
    /// Số lượt thao tác hôm nay.
    /// </summary>
    public int SoLogHomNay { get; set; }

    /// <summary>
    /// Số lượt thao tác trong tuần qua.
    /// </summary>
    public int SoLogTrongTuan { get; set; }

    /// <summary>
    /// Số lượt thao tác trong tháng này.
    /// </summary>
    public int SoLogTrongThang { get; set; }
}

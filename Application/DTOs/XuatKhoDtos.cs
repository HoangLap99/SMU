using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

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
   
    public string MucThuLePhiId { get; set; } = string.Empty;

    /// <summary>
    /// ID Danh mục Khu vực áp dụng (KhuVucApDung).
    /// </summary>
   
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
    
    public string KyHieuQuyenBienLaiId { get; set; } = string.Empty;

    /// <summary>
    /// Số biên lai (Ví dụ: "0000113").
    /// </summary>
 
    public string SoBienLai { get; set; } = string.Empty;

    /// <summary>
    /// Ngày biên lai.
    /// </summary>
    public DateTime NgayBienLai { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Số tiền thu (VNĐ).
    /// </summary>
    
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

    /// <summary>
    /// Cán bộ cấp / lập phiếu xuất kho (tùy chọn; nếu để trống sẽ tự động lấy theo thông tin người dùng đăng nhập).
    /// </summary>
    public string? CanBoCap { get; set; }

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

    /// <summary>
    /// Tên cán bộ cấp / lập phiếu xuất kho (đồng bộ CanBoCap với báo cáo xuất kho, alias của CreatedByName).
    /// </summary>
    public string? CanBoCap => CreatedByName;
}

#endregion

#region 10. Import Xuất kho từ tệp Excel

/// <summary>
/// Kết quả trả về của API export file mẫu xuất kho biển số xe.
/// Chứa các tham số phục vụ gọi trực tiếp API tải file: /api/FileUpload/download?filename=...&amp;path=...
/// </summary>
public class ExportMauXuatKhoResponse
{
    /// <summary>
    /// Tham số filename mã hóa Base64 cho API tải file: ?filename=...
    /// </summary>
    [JsonPropertyName("filename")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Tham số path mã hóa Base64 từ đường dẫn StorageFolder của appsettings: &amp;path=...
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn API hoàn chỉnh kèm query string để client tải file trực tiếp.
    /// </summary>
    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tên tệp tin gốc hiển thị khi tải về (mặc định: XuatKhoBienSo.xlsx).
    /// </summary>
    [JsonPropertyName("originalFileName")]
    public string OriginalFileName { get; set; } = "XuatKhoBienSo.xlsx";
}

/// <summary>
/// Yêu cầu nhập xuất kho biển số xe từ tệp Excel.
/// </summary>
public class ImportXuatKhoExcelRequest
{
    /// <summary>
    /// ID Danh mục Loại xe cần xuất kho (bắt buộc).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Loại xe.")]
    public string LoaiXeId { get; set; } = string.Empty;

    /// <summary>
    /// Tên tệp tin Excel mẫu đã lưu trong thư mục StorageFolder (appsettings.json).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập tên tệp tin Excel trong thư mục StorageFolder.")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Tùy chọn: Nếu đặt là true, hệ thống chỉ kiểm tra tính hợp lệ và thông báo danh sách biển số chưa có trong kho mà không thực hiện lưu xuất kho.
    /// </summary>
    public bool? ChiKiemTra { get; set; } = false;
}

/// <summary>
/// Kết quả sau khi nhập xuất kho từ tệp Excel.
/// </summary>
public class ImportXuatKhoExcelResponse
{
    /// <summary>
    /// Thông báo kết quả tổng hợp hoặc cảnh báo danh sách các biển số chưa có trong kho.
    /// </summary>
    public string ThongBao { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số dòng dữ liệu đọc được từ tệp Excel.
    /// </summary>
    public int TongSoDong { get; set; }

    /// <summary>
    /// Số lượng phiếu xuất kho tạo thành công.
    /// </summary>
    public int SoLuongThanhCong { get; set; }

    /// <summary>
    /// Số lượng dòng bị bỏ qua (ví dụ: đã xuất kho trước đó, thiếu số biển...).
    /// </summary>
    public int SoLuongBoQua { get; set; }

    /// <summary>
    /// Số lượng dòng bị lỗi khi xử lý (ví dụ: chưa có trong kho, định dạng sai...).
    /// </summary>
    public int SoLuongLoi { get; set; }

    /// <summary>
    /// Tổng số tiền lệ phí thu được từ các phiếu xuất kho thành công (VNĐ).
    /// </summary>
    public decimal TongTienThu { get; set; }

    /// <summary>
    /// Danh sách các biển kiểm soát đã xuất kho thành công.
    /// </summary>
    public List<string> DanhSachBienSoXuatThanhCong { get; set; } = [];

    /// <summary>
    /// Danh sách các biển kiểm soát trong tệp tin Excel CHƯA CÓ TRONG KHO (chưa được nhập kho).
    /// </summary>
    public List<string> DanhSachBienSoChuaCoTrongKho { get; set; } = [];

    /// <summary>
    /// Danh sách chi tiết các dòng bị lỗi hoặc bỏ qua.
    /// </summary>
    public List<ImportXuatKhoLoiItem> DanhSachLoi { get; set; } = [];
}

/// <summary>
/// Chi tiết dòng bị lỗi hoặc bỏ qua trong quá trình import Excel.
/// </summary>
public class ImportXuatKhoLoiItem
{
    /// <summary>
    /// Số thứ tự dòng trong tệp Excel.
    /// </summary>
    public int Dong { get; set; }

    /// <summary>
    /// Biển kiểm soát đọc được tại dòng đó (nếu có).
    /// </summary>
    public string? BienKiemSoat { get; set; }

    /// <summary>
    /// Lý do lỗi hoặc bỏ qua.
    /// </summary>
    public string LyDoLoi { get; set; } = string.Empty;
}

#endregion

#region 11. AI Import Xuất kho từ tệp tin bất kỳ (Word, Excel, Image/PNG, PDF, Text)

/// <summary>
/// Yêu cầu nhập xuất kho biển số xe sử dụng AI để đọc tệp tin bất kỳ (Word, Excel, PNG, JPG, PDF...).
/// Không cố định cấu trúc mẫu, AI tự động nhận diện và bóc tách các trường dữ liệu có trong tài liệu.
/// </summary>
public class ImportXuatKhoAiRequest
{
    /// <summary>
    /// Tệp tin bất kỳ tải lên trực tiếp (hỗ trợ .xlsx, .xls, .docx, .doc, .png, .jpg, .jpeg, .pdf, .txt, .csv...).
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Hoặc tên tệp tin đã lưu trong thư mục StorageFolder (nếu không truyền File trực tiếp).
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Tùy chọn chỉ định ID hoặc Mã Loại xe mặc định nếu tài liệu không đề cập loại xe.
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Tùy chọn chế độ chỉ kiểm tra (Dry-run / Preview): Nếu đặt true, hệ thống chỉ đọc và phân tích biển số, đối soát kho mà KHÔNG lưu thay đổi vào cơ sở dữ liệu.
    /// </summary>
    public bool? ChiKiemTra { get; set; } = false;

    /// <summary>
    /// Gợi ý hoặc chỉ dẫn bổ sung cho mô hình AI khi đọc tệp (tùy chọn).
    /// </summary>
    public string? Prompt { get; set; }
}

/// <summary>
/// Kết quả trả về của API nhập xuất kho biển số sử dụng AI.
/// </summary>
public class ImportXuatKhoAiResponse
{
    /// <summary>
    /// Trạng thái thực hiện thành công.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Tên tệp tin đã phân tích và import.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Định dạng tệp tin (.png, .docx, .xlsx, .pdf,...).
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Động cơ trích xuất đã dùng ("Gemini AI" hoặc "Heuristic Pattern Engine").
    /// </summary>
    public string EngineUsed { get; set; } = "Gemini AI";

    /// <summary>
    /// Thông báo tổng hợp kết quả nghiệp vụ (số lượng thành công, cảnh báo biển số chưa có trong kho...).
    /// </summary>
    public string ThongBao { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số dòng / bản ghi biển số xe mà AI trích xuất được từ tệp.
    /// </summary>
    public int TongSoDongTrichXuat { get; set; }

    /// <summary>
    /// Số lượng biển số xuất kho thành công (DaXuatKho = true).
    /// </summary>
    public int SoLuongThanhCong { get; set; }

    /// <summary>
    /// Số lượng biển số bị bỏ qua (đã xuất kho trước đó, trùng lặp trong tệp...).
    /// </summary>
    public int SoLuongBoQua { get; set; }

    /// <summary>
    /// Số lượng biển số bị lỗi (chưa có trong kho...).
    /// </summary>
    public int SoLuongLoi { get; set; }

    /// <summary>
    /// Tổng số tiền lệ phí thu được từ các biển số xuất kho thành công (VNĐ).
    /// </summary>
    public decimal TongTienThu { get; set; }

    /// <summary>
    /// Danh sách các biển kiểm soát đã xuất kho thành công.
    /// </summary>
    public List<string> DanhSachBienSoXuatThanhCong { get; set; } = [];

    /// <summary>
    /// Danh sách các biển kiểm soát trong tệp tin mà AI đọc được nhưng CHƯA CÓ TRONG KHO (chưa được nhập kho).
    /// </summary>
    public List<string> DanhSachBienSoChuaCoTrongKho { get; set; } = [];

    /// <summary>
    /// Danh sách chi tiết từng bản ghi dữ liệu AI trích xuất được kèm trạng thái đối soát trong kho.
    /// </summary>
    public List<ImportXuatKhoAiChiTietItem> ChiTietCacBanGhi { get; set; } = [];

    /// <summary>
    /// Danh sách chi tiết các dòng bị lỗi hoặc bỏ qua kèm lý do.
    /// </summary>
    public List<ImportXuatKhoLoiItem> DanhSachLoi { get; set; } = [];
}

/// <summary>
/// Chi tiết một dòng / bản ghi biển số trích xuất được từ AI và trạng thái xử lý kho.
/// </summary>
public class ImportXuatKhoAiChiTietItem
{
    /// <summary>
    /// Số thứ tự dòng.
    /// </summary>
    public int Stt { get; set; }

    /// <summary>
    /// Biển kiểm soát đọc được.
    /// </summary>
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Mã hồ sơ DVC đọc được (nếu có).
    /// </summary>
    public string? MaHoSoDvc { get; set; }

    /// <summary>
    /// Loại thủ tục đọc được (nếu có).
    /// </summary>
    public string? LoaiThuTuc { get; set; }

    /// <summary>
    /// Tính chất xuất kho đọc được (nếu có).
    /// </summary>
    public string? TinhChat { get; set; }

    /// <summary>
    /// Số tiền thu lệ phí đọc được (nếu có).
    /// </summary>
    public decimal? SoTienThu { get; set; }

    /// <summary>
    /// Khu vực áp dụng đọc được (nếu có).
    /// </summary>
    public string? KhuVuc { get; set; }

    /// <summary>
    /// Nhãn hiệu xe đọc được (nếu có).
    /// </summary>
    public string? NhanHieu { get; set; }

    /// <summary>
    /// Số khung đọc được (nếu có).
    /// </summary>
    public string? SoKhung { get; set; }

    /// <summary>
    /// Số máy đọc được (nếu có).
    /// </summary>
    public string? SoMay { get; set; }

    /// <summary>
    /// Ký hiệu quyển biên lai đọc được (nếu có).
    /// </summary>
    public string? KyHieuQuyen { get; set; }

    /// <summary>
    /// Số biên lai đọc được (nếu có).
    /// </summary>
    public string? SoBienLai { get; set; }

    /// <summary>
    /// Họ tên người nộp tiền đọc được (nếu có).
    /// </summary>
    public string? HoTenNguoiNop { get; set; }

    /// <summary>
    /// Số CCCD / CMND đọc được (nếu có).
    /// </summary>
    public string? SoCccd { get; set; }

    /// <summary>
    /// Ngày thực hiện đọc được (nếu có).
    /// </summary>
    public string? NgayThucHien { get; set; }

    /// <summary>
    /// Căn cứ pháp lý đọc được (nếu có).
    /// </summary>
    public string? CanCuPhapLy { get; set; }

    /// <summary>
    /// Ghi chú đọc được (nếu có).
    /// </summary>
    public string? GhiChu { get; set; }

    /// <summary>
    /// Đánh dấu biển số có tồn tại trong kho hay không.
    /// </summary>
    public bool TonTaiTrongKho { get; set; }

    /// <summary>
    /// Đánh dấu biển số đã xuất kho trước đó hay chưa.
    /// </summary>
    public bool DaXuatKhoTruocDo { get; set; }

    /// <summary>
    /// Đánh dấu xuất kho thành công trong phiên này.
    /// </summary>
    public bool XuatKhoThanhCong { get; set; }

    /// <summary>
    /// Trạng thái xử lý hiển thị trực quan (Ví dụ: "Xuất kho thành công", "Chưa có trong kho", "Đã xuất kho trước đó").
    /// </summary>
    public string TrangThaiXuLy { get; set; } = string.Empty;
}

#endregion


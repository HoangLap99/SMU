using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

#region 1. DTO Khởi tạo phiếu kiểm kê từ tồn kho hệ thống

/// <summary>
/// Yêu cầu khởi tạo dữ liệu phiếu kiểm kê từ bảng KhoBienSo đối với các biển chưa xuất.
/// </summary>
public class KhoiTaoKiemKeKhoRequest
{
    /// <summary>
    /// Loại phương tiện kiểm kê: "Ô tô", "Xe máy", "Rơ moóc" (Mặc định: "Ô tô").
    /// </summary>
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    /// <summary>
    /// ID Danh mục Loại xe (tùy chọn lọc).
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    public string? CustomerId { get; set; }
}

/// <summary>
/// Dữ liệu khởi tạo ban đầu cho giao diện "Lập phiếu kiểm kê kho biển số xe".
/// </summary>
public class KhoiTaoKiemKeKhoResponse
{
    /// <summary>
    /// Mã phiếu kiểm kê gợi ý tự động (Ví dụ: "KK-2026-10-10-921").
    /// </summary>
    public string MaPhieuKiemKe { get; set; } = string.Empty;

    /// <summary>
    /// Ngày kiểm kê (Mặc định ngày hôm nay).
    /// </summary>
    public DateTime NgayKiemKe { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian kiểm kê (Ví dụ: "17 giờ 15 phút" hoặc "17:15").
    /// </summary>
    public string ThoiGianKiemKe { get; set; } = string.Empty;

    /// <summary>
    /// Loại phương tiện kiểm kê ("Ô tô", "Xe máy", "Rơ moóc").
    /// </summary>
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    /// <summary>
    /// Địa điểm kiểm kê mặc định.
    /// </summary>
    public string DiaDiemKiemKe { get; set; } = "Tổ đăng ký xe, Công an phường Nghĩa Đô";

    /// <summary>
    /// Trưởng đoàn kiểm kê gợi ý.
    /// </summary>
    public string TruongDoanKiemKe { get; set; } = "Thiếu tá Bùi Văn Kiên";

    /// <summary>
    /// Cán bộ lập biên bản gợi ý.
    /// </summary>
    public string CanBoLapBienBan { get; set; } = "Bùi Văn Kiên";

    /// <summary>
    /// Tổng số lượng biển tồn hệ thống (Ví dụ: 299).
    /// </summary>
    public int TongTonHeThong { get; set; }

    /// <summary>
    /// Tổng số lượng thực tế kiểm đếm (Ban đầu mặc định = Tồn hệ thống).
    /// </summary>
    public int TongThucTe { get; set; }

    /// <summary>
    /// Chênh lệch tổng (Thực tế - Tồn hệ thống = 0).
    /// </summary>
    public int TongChenhLech { get; set; }

    /// <summary>
    /// Bảng danh sách các nhóm dải đầu biển tổng hợp từ kho.
    /// </summary>
    public List<KiemKeNhomDauBienItemDto> DanhSachNhomDauBien { get; set; } = new();
}

/// <summary>
/// Dòng chi tiết nhóm dải đầu biển trên bảng kiểm kê.
/// </summary>
public class KiemKeNhomDauBienItemDto
{
    public int STT { get; set; }

    /// <summary>
    /// Dải đầu biển (Ví dụ: "30B-207", "30B-369", "29E-794").
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Tên màu biển (Ví dụ: "Biển Trắng", "Biển Vàng").
    /// </summary>
    public string MauBien { get; set; } = "Biển Trắng";

    /// <summary>
    /// Mã màu hex hiển thị badge màu biển.
    /// </summary>
    public string? MauBienHex { get; set; }

    /// <summary>
    /// Số lượng tồn hệ thống (sổ sách phần mềm).
    /// </summary>
    public int TonHeThong { get; set; }

    /// <summary>
    /// Số lượng kiểm đếm thực tế (ô input số).
    /// </summary>
    public int ThucTe { get; set; }

    /// <summary>
    /// Chênh lệch (Thực tế - Tồn hệ thống).
    /// </summary>
    public int ChenhLech { get; set; }

    /// <summary>
    /// Số lượng biển có mặt thực tế trong kho.
    /// </summary>
    public int SoBienCoMat { get; set; }

    /// <summary>
    /// Số lượng biển báo thiếu.
    /// </summary>
    public int SoBienThieu { get; set; }

    /// <summary>
    /// Số lượng biển phát hiện thừa thực tế (chưa có trong phần mềm).
    /// </summary>
    public int SoBienThua { get; set; }

    /// <summary>
    /// Ghi chú (Mặc định: "Khớp sổ sách").
    /// </summary>
    public string GhiChu { get; set; } = "Khớp sổ sách";

    /// <summary>
    /// Danh sách biển trong kho của nhóm dải này (Có trạng thái để chuyển từ Có mặt sang thiếu).
    /// </summary>
    public List<BienTrongKhoItemDto> DanhSachBienTrongKho { get; set; } = new();

    /// <summary>
    /// Danh sách biển phát hiện thừa thực tế (Dựa vào biển số để tự xác định đầu biển số nào).
    /// </summary>
    public List<BienThuaItemDto> DanhSachBienThua { get; set; } = new();
}

#endregion

#region 2. DTO Popup Xác định biển thiếu/thừa theo dải đầu biển

/// <summary>
/// Yêu cầu lấy dữ liệu chi tiết cho popup "Xác định biển thiếu/thừa" của 1 dải đầu biển.
/// </summary>
public class ChiTietBienKiemKeRequest
{
    /// <summary>
    /// Đầu biển cần đối chiếu (Ví dụ: "29E-794", "30B-207").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng truyền đầu biển")]
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// Lọc thêm theo màu biển (tùy chọn).
    /// </summary>
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// ID Phiếu kiểm kê đang thao tác (nếu đã tạo/lưu nháp).
    /// </summary>
    public string? KiemKeKhoId { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm cụ thể trong popup (Ví dụ: "01" hoặc "794.01").
    /// </summary>
    public string? KeyWord { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    public string? CustomerId { get; set; }
}

/// <summary>
/// Dữ liệu hiển thị trên Popup "Xác định biển thiếu/thừa: Dải {DauBien} ({MauBien})".
/// </summary>
public class ChiTietBienKiemKeResponse
{
    /// <summary>
    /// Đầu biển đang đối chiếu (Ví dụ: "29E-794").
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// Tên màu biển (Ví dụ: "Vàng", "Trắng").
    /// </summary>
    public string MauBien { get; set; } = "Trắng";

    /// <summary>
    /// Tiêu đề popup (Ví dụ: "Xác định biển thiếu/thừa: Dải 29E-794 (Vàng)").
    /// </summary>
    public string TieuDe { get; set; } = string.Empty;

    /// <summary>
    /// Thẻ 1: Tồn sổ sách trong kho của dải này (Ví dụ: 39).
    /// </summary>
    public int TonSoSach { get; set; }

    /// <summary>
    /// Thẻ 2: Số biển có mặt thực tế (Ví dụ: 39).
    /// </summary>
    public int BienCoMat { get; set; }

    /// <summary>
    /// Thẻ 3: Số biển báo thiếu (Ví dụ: 0).
    /// </summary>
    public int BienBaoThieu { get; set; }

    /// <summary>
    /// Thẻ 4: Số biển phát hiện thừa thực tế (chưa có trong phần mềm) (Ví dụ: 0).
    /// </summary>
    public int BienPhatHienThua { get; set; }

    /// <summary>
    /// Tổng số thực tế tính toán (BienCoMat + BienPhatHienThua).
    /// </summary>
    public int ThucTe { get; set; }

    /// <summary>
    /// Chênh lệch (ThucTe - TonSoSach).
    /// </summary>
    public int ChenhLech { get; set; }

    /// <summary>
    /// Danh sách các biển số tồn trong kho của dải này (Nhấn để chuyển trạng thái "Có mặt" / "Thiếu").
    /// </summary>
    public List<BienTrongKhoItemDto> DanhSachBienTrongKho { get; set; } = new();

    /// <summary>
    /// Danh sách các biển phát hiện thừa thực tế (chưa có trong phần mềm).
    /// </summary>
    public List<BienThuaItemDto> DanhSachBienThua { get; set; } = new();
}

/// <summary>
/// Biển số tồn trong kho được hiển thị trong popup để đối chiếu.
/// </summary>
public class BienTrongKhoItemDto
{
    public string KhoBienSoId { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Ví dụ: "29E-794.01", "29E-794.04").
    /// </summary>
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái kiểm kê: "CoMat" hoặc "Thieu".
    /// </summary>
    public string TrangThaiKiemKe { get; set; } = "CoMat";

    /// <summary>
    /// Có mặt thực tế hay không (true = Có mặt, false = Báo thiếu).
    /// </summary>
    public bool IsCoMat { get; set; } = true;

    /// <summary>
    /// Ghi chú chi tiết cho biển này.
    /// </summary>
    public string? GhiChu { get; set; }
}

/// <summary>
/// Biển số phát hiện thừa thực tế (chưa có trong phần mềm).
/// </summary>
public class BienThuaItemDto
{
    public string? Id { get; set; }

    /// <summary>
    /// Biển kiểm soát phát hiện thừa (Ví dụ: "29E-794.02").
    /// </summary>
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú (Ví dụ: "Phát hiện thừa thực tế").
    /// </summary>
    public string? GhiChu { get; set; }
}

#endregion

#region 3. DTO Thêm mới và Cập nhật Phiếu kiểm kê

/// <summary>
/// Yêu cầu Thêm mới hoặc Cập nhật phiếu kiểm kê kho (Lưu nháp hoặc Xác nhận hoàn tất).
/// </summary>
public class SetKiemKeKhoRequest
{
    /// <summary>
    /// ID phiếu kiểm kê (null: Thêm mới; có Id: Cập nhật).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Mã phiếu kiểm kê (Ví dụ: "KK-2026-10-10-921").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập mã phiếu kiểm kê")]
    public string MaPhieuKiemKe { get; set; } = string.Empty;

    /// <summary>
    /// Ngày kiểm kê.
    /// </summary>
    public DateTime NgayKiemKe { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian kiểm kê (Ví dụ: "17 giờ 15 phút").
    /// </summary>
    public string? ThoiGianKiemKe { get; set; }

    /// <summary>
    /// ID Danh mục Loại xe.
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Loại phương tiện kiểm kê: "Ô tô", "Xe máy", "Rơ moóc".
    /// </summary>
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    /// <summary>
    /// Địa điểm kiểm kê.
    /// </summary>
    public string? DiaDiemKiemKe { get; set; }

    /// <summary>
    /// Trưởng đoàn kiểm kê.
    /// </summary>
    public string? TruongDoanKiemKe { get; set; }

    /// <summary>
    /// Cán bộ lập biên bản.
    /// </summary>
    public string? CanBoLapBienBan { get; set; }

    /// <summary>
    /// Tổng số lượng tồn hệ thống.
    /// </summary>
    public int TongTonHeThong { get; set; }

    /// <summary>
    /// Tổng số lượng thực tế kiểm đếm.
    /// </summary>
    public int TongThucTe { get; set; }

    /// <summary>
    /// Tổng chênh lệch (Thực tế - Hệ thống).
    /// </summary>
    public int TongChenhLech { get; set; }

    /// <summary>
    /// Trạng thái phiếu: "LuuNhap" (Lưu nháp / Ghi lại thường) hoặc "HoanTat" (Xác nhận hoàn tất).
    /// </summary>
    public string TrangThai { get; set; } = "LuuNhap";

    /// <summary>
    /// Đánh dấu Xác nhận hoàn tất (true: Xác nhận hoàn tất và đồng bộ bảng KhoBienSo; false: Ghi lại thường / Lưu nháp).
    /// Với Xác nhận hoàn tất = true: Biển thừa thực tế sẽ được NHẬP vào KhoBienSo, Biển thiếu sẽ được XUẤT khỏi KhoBienSo.
    /// </summary>
    public bool XacNhanHoanTat { get; set; } = false;

    /// <summary>
    /// Ghi chú phiếu kiểm kê.
    /// </summary>
    public string? GhiChu { get; set; }

    /// <summary>
    /// Danh sách chi tiết các nhóm dải đầu biển trong bảng (Mỗi nhóm chứa DanhSachBienTrongKho và DanhSachBienThua).
    /// </summary>
    public List<SetKiemKeNhomDauBienRequest> DanhSachNhomDauBien { get; set; } = new();
}

/// <summary>
/// Dòng nhóm dải đầu biển khi lưu phiếu kiểm kê.
/// </summary>
public class SetKiemKeNhomDauBienRequest
{
    public int STT { get; set; }
    public string DauBien { get; set; } = string.Empty;
    public string? MauBienSoId { get; set; }
    public string MauBien { get; set; } = "Biển Trắng";
    public string? MauBienHex { get; set; }
    public int TonHeThong { get; set; }
    public int ThucTe { get; set; }
    public int ChenhLech { get; set; }
    public int SoBienCoMat { get; set; }
    public int SoBienThieu { get; set; }
    public int SoBienThua { get; set; }
    public string? GhiChu { get; set; }

    /// <summary>
    /// Danh sách biển trong kho của nhóm dải này (Có trạng thái để chuyển từ Có mặt sang thiếu: TrangThaiKiemKe = "CoMat" / "Thieu", IsCoMat = true / false).
    /// </summary>
    public List<BienTrongKhoItemDto> DanhSachBienTrongKho { get; set; } = new();

    /// <summary>
    /// Danh sách biển phát hiện thừa thực tế của nhóm dải này (Dựa vào biển số để tự xác định đầu biển số nào khi hoàn tất).
    /// </summary>
    public List<BienThuaItemDto> DanhSachBienThua { get; set; } = new();
}

#endregion

#region 4. DTOs Danh sách phân trang và Response Phiếu kiểm kê

/// <summary>
/// Tham số tìm kiếm và phân trang danh sách phiếu kiểm kê kho.
/// </summary>
public class PagedKiemKeKhoRequest
{
    public int CurrentPage { get; set; } = 1;
    public int PageIndex { get => CurrentPage; set => CurrentPage = value; }
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Từ khóa tìm kiếm (Mã phiếu, Địa điểm, Trưởng đoàn, Cán bộ lập).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc theo loại phương tiện: "Ô tô", "Xe máy", "Rơ moóc".
    /// </summary>
    public string? LoaiPhuongTien { get; set; }

    /// <summary>
    /// Lọc theo trạng thái: "LuuNhap", "HoanTat".
    /// </summary>
    public string? TrangThai { get; set; }

    /// <summary>
    /// Khoảng ngày kiểm kê: Từ ngày.
    /// </summary>
    public DateTime? TuNgay { get; set; }

    /// <summary>
    /// Khoảng ngày kiểm kê: Đến ngày.
    /// </summary>
    public DateTime? DenNgay { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    public string? CustomerId { get; set; }
}

/// <summary>
/// Thông tin chi tiết phiếu kiểm kê kho trả về cho Client.
/// </summary>
public class KiemKeKhoResponse
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string MaPhieuKiemKe { get; set; } = string.Empty;
    public DateTime NgayKiemKe { get; set; }
    public string? ThoiGianKiemKe { get; set; }
    public string? LoaiXeId { get; set; }
    public string LoaiPhuongTien { get; set; } = "Ô tô";
    public string? DiaDiemKiemKe { get; set; }
    public string? TruongDoanKiemKe { get; set; }
    public string? CanBoLapBienBan { get; set; }
    public int TongTonHeThong { get; set; }
    public int TongThucTe { get; set; }
    public int TongChenhLech { get; set; }
    public string TrangThai { get; set; } = "LuuNhap";
    public string TrangThaiTen { get; set; } = "Lưu nháp";
    public string? GhiChu { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Danh sách chi tiết các nhóm dải đầu biển.
    /// </summary>
    public List<KiemKeNhomDauBienItemDto> DanhSachNhomDauBien { get; set; } = new();
}

#endregion

#region 5. DTO Tự động xác định dải đầu biển từ biển số kiểm soát

/// <summary>
/// Yêu cầu xác định dải đầu biển từ biển số kiểm soát.
/// </summary>
public class XacDinhDauBienRequest
{
    /// <summary>
    /// Biển kiểm soát cần xác định (Ví dụ: "30B-207.66", "29E-794.02", "29AH-316.05").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng truyền biển kiểm soát")]
    public string BienKiemSoat { get; set; } = string.Empty;

    public string? CustomerId { get; set; }
}

/// <summary>
/// Kết quả tự động xác định dải đầu biển từ biển số kiểm soát.
/// </summary>
public class XacDinhDauBienResponse
{
    /// <summary>
    /// Biển kiểm soát chuẩn hóa (Ví dụ: "30B-207.66").
    /// </summary>
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Dải đầu biển được xác định (Ví dụ: "30B-207", "29E-794").
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// ID Danh mục Màu biển số nếu tìm thấy trong hệ thống.
    /// </summary>
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Tên màu biển (Ví dụ: "Biển Trắng", "Biển Vàng").
    /// </summary>
    public string MauBien { get; set; } = "Biển Trắng";

    /// <summary>
    /// Mã màu hex hiển thị badge màu biển.
    /// </summary>
    public string? MauBienHex { get; set; } = "#FFFFFF";

    /// <summary>
    /// Biển này đã tồn tại trong kho (true) hay là biển phát hiện thừa chưa có trong kho (false).
    /// </summary>
    public bool TonTaiTrongKho { get; set; } = false;
}

#endregion

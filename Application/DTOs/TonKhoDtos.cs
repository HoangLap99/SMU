using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

#region 1. DTOs Bộ lọc tìm kiếm tồn kho

/// <summary>
/// Yêu cầu lọc bảng tổng hợp tồn kho biển số xe.
/// </summary>
public class FilterTonKhoRequest
{
    /// <summary>
    /// Từ khóa tìm kiếm số biển (Ví dụ: "29E-895.05", "30B-532...").
    /// </summary>
    public string? TimSoBien { get; set; }
    public string? Keyword { get; set ; }

    /// <summary>
    /// Đầu biển cần lọc (Ví dụ: "29AH-316", "30B-532", "29E-895" hoặc "30B").
    /// </summary>
    public string? DauBien { get; set; }

    /// <summary>
    /// ID Danh mục Loại phương tiện / Loại xe (LoaiXe).
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// ID Danh mục Màu biển số (MauBienSo).
    /// </summary>
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Trạng thái biển số ("DuocBam", "DaBam", "ChoDauGia", "ThuHoi" hoặc null là tất cả).
    /// </summary>
    public string? TrangThai { get; set; }

    /// <summary>
    /// Loại thời gian ("TatCa", "HomNay", "TuanNay", "ThangNay", "TuyChon").
    /// </summary>
    public string? LoaiThoiGian { get; set; }

    /// <summary>
    /// Từ ngày nhập kho (dd/MM/yyyy).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Đến ngày nhập kho (dd/MM/yyyy).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị (nếu null sẽ lấy tự động từ tài khoản hiện tại).
    /// </summary>
    public string? CustomerId { get; set; }
}

#endregion

#region 2. DTOs Bảng tổng hợp tồn kho theo đầu biển

/// <summary>
/// Dòng tổng hợp tồn kho của một nhóm đầu biển số xe.
/// </summary>
public class NhomDauBienTonKhoItemResponse
{
    /// <summary>
    /// Số thứ tự dòng (1, 2, 3...).
    /// </summary>
    public int STT { get; set; }

    /// <summary>
    /// Đầu biển / Nhóm dải biển (Ví dụ: "29AH-316", "29AH-314", "30B-532", "29E-895").
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu Danh mục Đầu biển số (nếu có).
    /// </summary>
    public string? DauBienSoId { get; set; }

    /// <summary>
    /// ID Danh mục Loại xe.
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Loại phương tiện (Ví dụ: "Xe máy", "Ô tô").
    /// </summary>
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    /// <summary>
    /// ID Danh mục Màu biển số.
    /// </summary>
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Tên Màu biển (Ví dụ: "Trắng", "Vàng", "Xanh", "Đỏ").
    /// </summary>
    public string MauBien { get; set; } = "Trắng";

    /// <summary>
    /// Mã Hex màu biển số hiển thị trên badge giao diện (Ví dụ: "#FFFFFF", "#FFCC00").
    /// </summary>
    public string? MauBienHex { get; set; }

    /// <summary>
    /// Số lượng Tồn kho (Được bấm) chưa xuất:
    /// Điều kiện: !DaXuatKho &amp;&amp; TrangThai == "DuocBam" &amp;&amp; !LaBienDauGia.
    /// </summary>
    public int TonKhoDuocBam { get; set; }

    /// <summary>
    /// Số lượng biển Đã bấm (Đã cấp / Đã xuất kho):
    /// Điều kiện: DaXuatKho == true || TrangThai == "DaBam".
    /// </summary>
    public int DaBam { get; set; }

    /// <summary>
    /// Số lượng biển Chờ đấu giá (nếu có).
    /// </summary>
    public int ChoDauGia { get; set; }

    /// <summary>
    /// Tổng số biển của nhóm dải đầu biển này (= Tồn kho + Đã bấm + Chờ đấu giá).
    /// </summary>
    public int TongBien { get; set; }
}

/// <summary>
/// Kết quả trả về cho Bảng Tổng Hợp Tồn Kho Theo Đầu Biển.
/// Kèm dòng tổng cộng toàn bộ các nhóm đầu biển.
/// </summary>
public class TongHopTonKhoResponse
{
    /// <summary>
    /// Tổng số nhóm dải đầu biển (Ví dụ: 34 nhóm dải biển).
    /// </summary>
    public int TongSoNhomDauBien { get; set; }

    /// <summary>
    /// Tổng cộng số biển Tồn kho (Được bấm) của tất cả các nhóm (Ví dụ: 591 biển).
    /// </summary>
    public int TongTonKhoDuocBam { get; set; }

    /// <summary>
    /// Tổng cộng số biển Đã bấm của tất cả các nhóm (Ví dụ: 120 biển).
    /// </summary>
    public int TongDaBam { get; set; }

    /// <summary>
    /// Tổng cộng số biển Chờ đấu giá của tất cả các nhóm.
    /// </summary>
    public int TongChoDauGia { get; set; }

    /// <summary>
    /// Tổng cộng toàn bộ số lượng biển của tất cả các nhóm (Ví dụ: 711 biển).
    /// </summary>
    public int TongSoBien { get; set; }

    /// <summary>
    /// Danh sách chi tiết các nhóm dải đầu biển.
    /// </summary>
    public List<NhomDauBienTonKhoItemResponse> Items { get; set; } = new();
}

#endregion

#region 3. DTOs Chi tiết danh sách biển số chưa xuất của 1 đầu biển

/// <summary>
/// Yêu cầu lấy danh sách biển số chưa xuất khi click vào nút "Chi tiết" ở 1 nhóm đầu biển.
/// </summary>
public class ChiTietTonKhoRequest
{
    /// <summary>
    /// Đầu biển cần xem chi tiết (Ví dụ: "29AH-316", "30B-532", "29E-895").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn Đầu biển để xem chi tiết")]
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// Lọc thêm theo Loại xe (tùy chọn).
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Lọc thêm theo Màu biển (tùy chọn).
    /// </summary>
    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm cụ thể trong nhóm biển (Ví dụ: "316.05").
    /// </summary>
    public string? KeyWord { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    public string? CustomerId { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageIndex { get => CurrentPage; set => CurrentPage = value; }
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Thông tin từng biển số chưa xuất kho thuộc nhóm đầu biển được chọn.
/// </summary>
public class BienSoChuaXuatItemResponse
{
    public int STT { get; set; }
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Ví dụ: "29AH-316.01", "30B-207.66").
    /// </summary>
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Đầu biển (Ví dụ: "29AH-316").
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    public string? LoaiXeId { get; set; }
    public string LoaiPhuongTien { get; set; } = "Ô tô";

    public string? MauBienSoId { get; set; }
    public string MauBien { get; set; } = "Trắng";
    public string? MauBienHex { get; set; }

    /// <summary>
    /// Trạng thái biển: "DuocBam".
    /// </summary>
    public string TrangThai { get; set; } = "DuocBam";
    public string TrangThaiTen { get; set; } = "Biển được bấm (Tồn kho)";

    /// <summary>
    /// Đánh dấu đã xuất kho (luôn false đối với danh sách này).
    /// </summary>
    public bool DaXuatKho { get; set; } = false;

    public DateTime NgayNhapKho { get; set; }
    public string? DotNhapKhoId { get; set; }
    public string? MaDotNhap { get; set; }
    public string? GhiChu { get; set; }
}

#endregion

#region 4. DTOs Dữ liệu cấu hình bộ lọc dropdown

/// <summary>
/// Tùy chọn đầu biển trong dropdown bộ lọc kèm số lượng còn lại.
/// </summary>
public class DauBienFilterOption
{
    public string DauBien { get; set; } = string.Empty;
    public int SoLuongTon { get; set; }
    public string TieuDeHienThi { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu tổng hợp các danh mục hỗ trợ hiển thị bộ lọc tìm kiếm.
/// </summary>
public class BoLocTonKhoResponse
{
    /// <summary>
    /// Danh sách các đầu biển kèm số lượng biển còn tồn.
    /// </summary>
    public List<DauBienFilterOption> DanhSachDauBien { get; set; } = new();

    /// <summary>
    /// Danh mục Loại phương tiện (LoaiXe).
    /// </summary>
    public List<DanhMucOptionResponse> DanhSachLoaiXe { get; set; } = new();

    /// <summary>
    /// Danh mục Màu biển số (MauBienSo).
    /// </summary>
    public List<DanhMucOptionResponse> DanhSachMauBien { get; set; } = new();

    /// <summary>
    /// Danh mục Trạng thái biển số.
    /// </summary>
    public List<DanhMucOptionResponse> DanhSachTrangThai { get; set; } = new();

    /// <summary>
    /// Tổng số nhóm dải biển (Ví dụ: "Tất cả đầu biển (34)").
    /// </summary>
    public int TongSoNhomDauBien { get; set; }

    /// <summary>
    /// Tổng số biển tồn kho hiện có.
    /// </summary>
    public int TongSoBienTon { get; set; }
}

/// <summary>
/// Option tổng quát cho dropdown danh mục.
/// </summary>
public class DanhMucOptionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Ma { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string? Extra { get; set; }
}

#endregion

#region 5. DTOs Ma Trận 100 Số Dải Biển (00 - 99)

/// <summary>
/// Yêu cầu lấy Ma trận 100 số dải biển (00 đến 99).
/// </summary>
public class MaTranDaiSoRequest
{
    /// <summary>
    /// Đầu biển số (Ví dụ: "29AC-963", "30B-532", "29AH-316").
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập hoặc chọn đầu biển số")]
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// ID Khách hàng / Đơn vị.
    /// </summary>
    public string? CustomerId { get; set; }
}

/// <summary>
/// Chú giải màu sắc cho các trạng thái trong ma trận 100 số.
/// </summary>
public class ChuGiaiMauItem
{
    public string Ma { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string MaMauHex { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin 1 ô số trong ma trận 100 số (từ .00 đến .99).
/// </summary>
public class MaTranSoItemResponse
{
    /// <summary>
    /// Số thứ tự từ 0 đến 99.
    /// </summary>
    public int STT { get; set; }

    /// <summary>
    /// Hai chữ số đuôi (Ví dụ: "00", "01" ... "99").
    /// </summary>
    public string SoDuoi { get; set; } = string.Empty;

    /// <summary>
    /// Ký hiệu hiển thị trên giao diện (Ví dụ: ".00", ".01" ... ".99").
    /// </summary>
    public string KyHieuHienThi { get; set; } = string.Empty;

    /// <summary>
    /// ID bản ghi trong bảng KhoBienSo (null nếu chưa nhập vào kho).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Biển kiểm soát hoàn chỉnh (Ví dụ: "29AC-963.00", "29AC-963.52").
    /// </summary>
    public string BienKiemSoat { get; set; } = string.Empty;

    /// <summary>
    /// Đánh dấu biển số đã có trong bảng KhoBienSo hay chưa.
    /// </summary>
    public bool DaCoTrongKho { get; set; } = false;

    /// <summary>
    /// Mã trạng thái:
    /// - "ChuaNhap": Chưa nhập (Trống)
    /// - "DuocBam": Biển được bấm (Tồn kho)
    /// - "DaBam": Biển đã bấm (Đã xuất)
    /// - "ChoDauGia": Chờ đấu giá
    /// - "DauGiaThanhCong": Đấu giá thành công
    /// - "ThuHoi": Thu hồi
    /// </summary>
    public string TrangThai { get; set; } = "ChuaNhap";

    /// <summary>
    /// Tên hiển thị trạng thái trên ô số (Ví dụ: "TRỐNG", "ĐƯỢC BẤM", "ĐÃ BẤM", "CHỜ ĐẤU GIÁ").
    /// </summary>
    public string TrangThaiTen { get; set; } = "TRỐNG";

    /// <summary>
    /// Mã màu hex hiển thị (Ví dụ: "#10B981" Xanh lá, "#374151" Xám đen, "#8B5CF6" Tím, "#E5E7EB" Xám nhạt).
    /// </summary>
    public string MauSacHienThi { get; set; } = "#E5E7EB";

    /// <summary>
    /// Tên class CSS hiển thị ("success", "dark", "purple", "warning", "empty").
    /// </summary>
    public string MauSacClass { get; set; } = "empty";

    public string? LoaiXeId { get; set; }
    public string? LoaiPhuongTien { get; set; }

    public string? MauBienSoId { get; set; }
    public string? MauBien { get; set; }
    public string? MauBienHex { get; set; }

    public bool LaBienDauGia { get; set; } = false;
    public bool DaXuatKho { get; set; } = false;
    public DateTime? NgayNhapKho { get; set; }
    public DateTime? NgayXuatKho { get; set; }
    public string? GhiChu { get; set; }
}

/// <summary>
/// Kết quả trả về Ma Trận 100 Số Dải Biển (từ .00 đến .99).
/// </summary>
public class MaTranDaiSoResponse
{
    /// <summary>
    /// Đầu biển số đã tra cứu (Ví dụ: "29AC-963").
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    /// <summary>
    /// Tiêu đề giao diện (Ví dụ: "Ma Trận 100 Số Dải Biển: 29AC-963").
    /// </summary>
    public string TieuDe { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số ô số trong ma trận dải số (100).
    /// </summary>
    public int TongSoBienDaiSo { get; set; } = 100;

    /// <summary>
    /// Tổng số biển số đã có trong bảng KhoBienSo của dải biển này.
    /// </summary>
    public int TongSoBienTrongKho { get; set; }

    /// <summary>
    /// Số lượng biển được bấm (Tồn kho).
    /// </summary>
    public int SoBienDuocBam { get; set; }

    /// <summary>
    /// Số lượng biển đã bấm (Đã xuất).
    /// </summary>
    public int SoBienDaBam { get; set; }

    /// <summary>
    /// Số lượng biển chờ đấu giá.
    /// </summary>
    public int SoBienChoDauGia { get; set; }

    /// <summary>
    /// Số lượng biển đấu giá thành công.
    /// </summary>
    public int SoBienDauGiaThanhCong { get; set; }

    /// <summary>
    /// Số lượng số chưa nhập trong dải (Trống).
    /// </summary>
    public int SoBienChuaNhap { get; set; }

    /// <summary>
    /// Danh sách các mục chú giải màu sắc.
    /// </summary>
    public List<ChuGiaiMauItem> DanhSachChuGiai { get; set; } = new();

    /// <summary>
    /// Dãy 100 ô số từ .00 đến .99.
    /// </summary>
    public List<MaTranSoItemResponse> DanhSachSo { get; set; } = new();
}

/// <summary>
/// Yêu cầu cập nhật trạng thái của 1 số trong ma trận ("Bấm vào ô số để sửa trạng thái").
/// </summary>
public class CapNhatTrangThaiSoRequest
{
    [Required(ErrorMessage = "Vui lòng nhập đầu biển")]
    public string DauBien { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số đuôi (00-99)")]
    public string SoDuoi { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới: "DuocBam", "DaBam", "ChoDauGia", "DauGiaThanhCong", "ThuHoi".
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn trạng thái mới")]
    public string TrangThaiMoi { get; set; } = string.Empty;

    public bool? LaBienDauGia { get; set; }
    public string? GhiChu { get; set; }
    public string? CustomerId { get; set; }
}

#endregion


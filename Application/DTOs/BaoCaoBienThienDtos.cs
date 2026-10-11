using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

#region 1. DTO Bộ lọc (Khoanh 1 trên giao diện)

/// <summary>
/// Bộ lọc dùng chung cho trang Biểu đồ phân tích biến thiên Nhập - Xuất - Chi phí lệ phí (Khoanh số 1).
/// </summary>
public class FilterBaoCaoBienThienRequest
{
    /// <summary>
    /// Chế độ gom nhóm trục thời gian của biểu đồ và bảng số liệu (Button toggle góc trên bên phải):
    /// - "Thang" (Mặc định) hoặc "TheoThang": Gom theo Tháng (Trục thời gian hiển thị: Tháng 8/2026, Tháng 9/2026, Tháng 10/2026...).
    /// - "Ngay" hoặc "TheoNgay": Gom theo Ngày (Trục thời gian hiển thị: 01/08/2026, 02/08/2026, 03/08/2026...).
    /// Gợi ý FE: Khi người dùng bấm tab 'Theo tháng' truyền "Thang", bấm tab 'Theo ngày' truyền "Ngay".
    /// </summary>
    public string GomTheo { get; set; } = "Thang";

    /// <summary>
    /// Mốc thời gian chọn nhanh (Các nút bấm pill filter: [Toàn bộ], [Tháng 8/2026], [Tháng 9/2026], [Tháng 10/2026], [Quý 3/2026]):
    /// Các giá trị hợp lệ FE có thể truyền:
    /// - "ToanBo" hoặc "All": Lấy toàn bộ dải thời gian dữ liệu (mặc định từ 01/08/2026 đến 31/10/2026 nếu không truyền từ/đến).
    /// - "Thang8" hoặc "Thang8/2026" hoặc "Thang 8/2026": Lọc đúng Tháng 8/2026 (01/08/2026 -> 31/08/2026).
    /// - "Thang9" hoặc "Thang9/2026" hoặc "Thang 9/2026": Lọc đúng Tháng 9/2026 (01/09/2026 -> 30/09/2026).
    /// - "Thang10" hoặc "Thang10/2026" hoặc "Thang 10/2026": Lọc đúng Tháng 10/2026 (01/10/2026 -> 31/10/2026).
    /// - "Quy3" hoặc "Quy3/2026" hoặc "Quy 3/2026": Lọc Quý 3/2026 (01/07/2026 -> 30/09/2026).
    /// - "ThangX/yyyy" (Ví dụ: "Thang11/2026", "Thang12/2026"): Tự động nhận diện tháng và năm bất kỳ.
    /// 
    /// Lưu ý nghiệp vụ FE:
    /// 1. Nếu người dùng chọn mốc nhanh (ví dụ bấm nút 'Tháng 9/2026'): Chỉ cần truyền 'mocThoiGianNhanh: "Thang9"', backend sẽ tự động tính khoảng ngày từ 01/09/2026 đến 30/09/2026.
    /// 2. Nếu người dùng tự pick ngày từ ô 'Từ - Đến' (date picker): Gửi 'tuNgay' và 'denNgay', đồng thời có thể để 'mocThoiGianNhanh = null' hoặc 'custom'. Backend sẽ ưu tiên khoảng ngày tùy chỉnh đó.
    /// </summary>
    public string? MocThoiGianNhanh { get; set; }

    /// <summary>
    /// Lọc từ ngày (Từ: dd/MM/yyyy).
    /// </summary>
    public DateTime? TuNgay { get; set; }

    /// <summary>
    /// Lọc đến ngày (Đến: dd/MM/yyyy).
    /// </summary>
    public DateTime? DenNgay { get; set; }

    /// <summary>
    /// ID Danh mục Loại xe (chọn từ dropdown "Tất cả loại xe"). Để trống là lấy tất cả.
    /// </summary>
    public string? LoaiXeId { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị sở hữu kho. Tự động lấy theo User đăng nhập nếu không truyền.
    /// </summary>
    public string? CustomerId { get; set; }
}

#endregion

#region 2. DTO Thẻ tóm tắt KPI / Tổng quan (Khoanh đỏ số 2)

/// <summary>
/// Dữ liệu 4 thẻ KPI thống kê tổng quan trên đầu trang (Khoanh đỏ số 2).
/// </summary>
public class BaoCaoKpiTongQuanResponse
{
    /// <summary>
    /// Card 1: Tổng số biển nhập kho (Ví dụ: 813 biển).
    /// </summary>
    public int TongSoBienNhapKho { get; set; }

    /// <summary>
    /// Chú thích Card 1: Số đợt tiếp nhận (Ví dụ: "Qua 5 đợt tiếp nhận dải số").
    /// </summary>
    public string ChuThichNhapKho { get; set; } = string.Empty;

    /// <summary>
    /// Card 2: Tổng số biển xuất kho (Ví dụ: 8 biển).
    /// </summary>
    public int TongSoBienXuatKho { get; set; }

    /// <summary>
    /// Chú thích Card 2: "Đã bấm ngẫu nhiên &amp; cấp phát".
    /// </summary>
    public string ChuThichXuatKho { get; set; } = "Đã bấm ngẫu nhiên & cấp phát";

    /// <summary>
    /// Card 3: Tiền thu lệ phí xuất biển (VNĐ, Ví dụ: 43.700.000 đ).
    /// </summary>
    public decimal TienThuLePhiXuatBien { get; set; }

    /// <summary>
    /// Tiền thu định dạng hiển thị: "43.700.000 đ".
    /// </summary>
    public string TienThuLePhiHienThi => $"{TienThuLePhiXuatBien:N0} đ";

    /// <summary>
    /// Chú thích Card 3: "Thu theo biên lai điện tử".
    /// </summary>
    public string ChuThichTienThu { get; set; } = "Thu theo biên lai điện tử";

    /// <summary>
    /// Card 4: Tỷ lệ xuất / nhập kho (%: TongXuat / TongNhap * 100).
    /// </summary>
    public double TyLeXuatNhapKho { get; set; }

    /// <summary>
    /// Tỷ lệ hiển thị: "1%".
    /// </summary>
    public string TyLeXuatNhapKhoHienThi => $"{TyLeXuatNhapKho:0.#}%";

    /// <summary>
    /// Chú thích Card 4: "Tốc độ luân chuyển biển số".
    /// </summary>
    public string ChuThichTyLe { get; set; } = "Tốc độ luân chuyển biển số";
}

#endregion

#region 3. DTO Biểu đồ 1: Biến thiên Số Lượng Biển Nhập và Biển Xuất (Khoanh đỏ số 3)

/// <summary>
/// Dữ liệu Biểu đồ 1: Biến thiên Số Lượng Biển Nhập và Biển Xuất theo thời gian (Khoanh đỏ số 3).
/// </summary>
public class BieuDoBienThienNhapXuatResponse
{
    public string TieuDe { get; set; } = "Biểu đồ 1: Biến thiên Số Lượng Biển Nhập và Biển Xuất theo thời gian";
    public string MoTa { get; set; } = "So sánh khối lượng biển tiếp nhận vào kho (Nhập) và khối lượng biển cấp phát ngẫu nhiên (Xuất)";
    public string DonViTinh { get; set; } = "Biển kiểm soát";
    public string GomTheo { get; set; } = "Thang";

    public int TongNhap { get; set; }
    public int TongXuat { get; set; }

    /// <summary>
    /// Danh sách các cột mốc thời gian hiển thị trên biểu đồ (cột kép Nhập vs Xuất).
    /// </summary>
    public List<BieuDoNhapXuatItemDto> Items { get; set; } = new();
}

/// <summary>
/// Một mốc thời gian trên Biểu đồ 1 (cột kép Nhập vs Xuất).
/// </summary>
public class BieuDoNhapXuatItemDto
{
    /// <summary>
    /// Nhãn hiển thị mốc thời gian (Ví dụ: "Tháng 8/2026" hoặc "01/08/2026").
    /// </summary>
    public string ThoiGian { get; set; } = string.Empty;

    /// <summary>
    /// Mốc thời gian gốc (Date) dùng để sắp xếp hoặc phân tích.
    /// </summary>
    public DateTime MocThoiGian { get; set; }

    /// <summary>
    /// Khối lượng biển tiếp nhận vào kho (Biển nhập kho - Cột Xanh dương).
    /// </summary>
    public int SoLuongNhap { get; set; }

    /// <summary>
    /// Khối lượng biển cấp phát ngẫu nhiên (Biển xuất kho - Cột Xanh lá).
    /// </summary>
    public int SoLuongXuat { get; set; }
}

#endregion

#region 4. DTO Biểu đồ 2: Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất (Khoanh đỏ số 4)

/// <summary>
/// Dữ liệu Biểu đồ 2: Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất theo thời gian (Khoanh đỏ số 4).
/// </summary>
public class BieuDoBienThienLePhiResponse
{
    public string TieuDe { get; set; } = "Biểu đồ 2: Biến thiên Tiền Lệ Phí Thu Từ Biển Xuất theo thời gian";
    public string MoTa { get; set; } = "Tổng số tiền lệ phí thu ngân sách nhà nước từ các hồ sơ xuất biển và đăng ký xe (VNĐ)";
    public string DonViTinh { get; set; } = "Việt Nam đồng (VNĐ)";
    public string GomTheo { get; set; } = "Thang";

    /// <summary>
    /// Tổng tiền thu toàn kỳ (Ví dụ: 43.700.000).
    /// </summary>
    public decimal TongKy { get; set; }

    /// <summary>
    /// Tổng kỳ hiển thị ở góc trên bên phải biểu đồ: "Tổng kỳ: 43.700.000 đ".
    /// </summary>
    public string TongKyHienThi => $"{TongKy:N0} đ";

    /// <summary>
    /// Danh sách các cột mốc thời gian trên biểu đồ tiền thu lệ phí (Cột màu Tím).
    /// </summary>
    public List<BieuDoLePhiItemDto> Items { get; set; } = new();
}

/// <summary>
/// Một mốc thời gian trên Biểu đồ 2.
/// </summary>
public class BieuDoLePhiItemDto
{
    /// <summary>
    /// Nhãn hiển thị mốc thời gian (Ví dụ: "Tháng 8/2026" hoặc "01/08/2026").
    /// </summary>
    public string ThoiGian { get; set; } = string.Empty;

    public DateTime MocThoiGian { get; set; }

    /// <summary>
    /// Số tiền thu thực tế trong kỳ (VNĐ).
    /// </summary>
    public decimal TienThu { get; set; }

    /// <summary>
    /// Nhãn tiền thu hiển thị trên đỉnh cột (Ví dụ: "2.3 tr", "40.9 tr", "550 k").
    /// </summary>
    public string TienThuHienThi { get; set; } = string.Empty;

    /// <summary>
    /// Số hồ sơ phát sinh thu phí trong kỳ.
    /// </summary>
    public int SoHoSo { get; set; }
}

#endregion

#region 5. DTO Cơ cấu biển xuất theo thủ tục & Loại phương tiện (Khoanh đỏ số 5)

/// <summary>
/// Dữ liệu Cơ cấu biển xuất theo thủ tục và Phân bổ theo loại phương tiện (Khoanh đỏ số 5).
/// </summary>
public class CoCauBienXuatResponse
{
    public string TieuDe { get; set; } = "Cơ cấu biển xuất theo thủ tục";
    public string MoTa { get; set; } = "Phân bổ lượt giải quyết hồ sơ";

    public int TongLuotGiaiQuyet { get; set; }
    public decimal TongTienThu { get; set; }

    /// <summary>
    /// Phân bổ theo từng loại thủ tục (Cấp biển, Sang tên, Cấp lại...).
    /// </summary>
    public List<CoCauThuTucItemDto> TheoThuTuc { get; set; } = new();

    /// <summary>
    /// Phân bổ theo loại phương tiện (Ô tô, Xe máy, Rơ moóc, Khác...).
    /// </summary>
    public List<CoCauLoaiPhuongTienItemDto> TheoLoaiPhuongTien { get; set; } = new();
}

/// <summary>
/// Dòng chi tiết một thủ tục giải quyết hồ sơ.
/// </summary>
public class CoCauThuTucItemDto
{
    /// <summary>
    /// Tên loại thủ tục (Ví dụ: "Cấp biển", "Sang tên", "Cấp lại").
    /// </summary>
    public string TenThuTuc { get; set; } = string.Empty;

    /// <summary>
    /// Số lượt giải quyết hồ sơ (Ví dụ: 8 lượt).
    /// </summary>
    public int SoLuot { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm trên tổng số lượt (Ví dụ: 67%).
    /// </summary>
    public double TyLeLuot { get; set; }

    /// <summary>
    /// Số lượng biển số xuất từ kho (Ví dụ: Cấp biển: 8 biển; Cấp lại: 0 biển).
    /// </summary>
    public int SoBienXuatKho { get; set; }

    /// <summary>
    /// Số tiền thu lệ phí từ thủ tục này (VNĐ).
    /// </summary>
    public decimal TienThu { get; set; }

    public string TienThuHienThi => $"{TienThu:N0} đ";
}

/// <summary>
/// Phân bổ giải quyết hồ sơ theo loại phương tiện.
/// </summary>
public class CoCauLoaiPhuongTienItemDto
{
    /// <summary>
    /// Tên loại phương tiện (Ví dụ: "Ô tô", "Xe máy", "Rơ moóc", "Khác").
    /// </summary>
    public string TenLoaiXe { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng hồ sơ / biển số phát sinh (Ví dụ: 6).
    /// </summary>
    public int SoLuong { get; set; }

    /// <summary>
    /// Tỷ lệ % (Ví dụ: 50%).
    /// </summary>
    public double TyLePhanTram { get; set; }

    public string TyLeHienThi => $"{TyLePhanTram:0.#}%";
}

#endregion

#region 6. DTO Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết (Khoanh đỏ số 6)

/// <summary>
/// Dữ liệu Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết (Khoanh đỏ số 6).
/// </summary>
public class BangSoLieuBienThienResponse
{
    public string TieuDe { get; set; } = "Bảng Số Liệu Biến Thiên Nhập - Xuất - Tiền Thu Chi Tiết";

    /// <summary>
    /// Nhãn hiển thị tổng số mốc thời gian ở góc phải (Ví dụ: "3 mốc thời gian").
    /// </summary>
    public string TongMocThoiGianHienThi { get; set; } = string.Empty;

    public string GomTheo { get; set; } = "Thang";

    /// <summary>
    /// Danh sách các dòng số liệu theo từng mốc thời gian (Tháng hoặc Ngày).
    /// </summary>
    public List<BangSoLieuItemDto> Items { get; set; } = new();

    /// <summary>
    /// Dòng TỔNG CỘNG ở chân bảng.
    /// </summary>
    public BangSoLieuTongCongDto TongCong { get; set; } = new();
}

/// <summary>
/// Một dòng dữ liệu trong Bảng biến thiên chi tiết.
/// </summary>
public class BangSoLieuItemDto
{
    public int Stt { get; set; }

    /// <summary>
    /// Thời gian (Ví dụ: "Tháng 8/2026", "Tháng 9/2026", "Tháng 10/2026" hoặc theo ngày).
    /// </summary>
    public string ThoiGian { get; set; } = string.Empty;

    public DateTime MocThoiGian { get; set; }

    /// <summary>
    /// Khối lượng Biển nhập kho (Ví dụ: 613).
    /// </summary>
    public int BienNhapKho { get; set; }

    /// <summary>
    /// Khối lượng Biển xuất kho (Ví dụ: 3).
    /// </summary>
    public int BienXuatKho { get; set; }

    /// <summary>
    /// Số lượng hồ sơ giải quyết (Ví dụ: 5).
    /// </summary>
    public int SoHoSo { get; set; }

    /// <summary>
    /// Tiền thu lệ phí (VNĐ, Ví dụ: 2.300.000).
    /// </summary>
    public decimal TienThuLePhi { get; set; }

    public string TienThuLePhiHienThi => $"{TienThuLePhi:N0} đ";

    /// <summary>
    /// Chênh lệch (Nhập - Xuất) = BienNhapKho - BienXuatKho.
    /// </summary>
    public int ChenhLech { get; set; }

    /// <summary>
    /// Nhãn hiển thị chênh lệch (Ví dụ: "+610", "+146", "+49").
    /// </summary>
    public string ChenhLechHienThi => ChenhLech >= 0 ? $"+{ChenhLech}" : $"{ChenhLech}";
}

/// <summary>
/// Dòng TỔNG CỘNG của Bảng biến thiên chi tiết.
/// </summary>
public class BangSoLieuTongCongDto
{
    public int TongBienNhapKho { get; set; }
    public int TongBienXuatKho { get; set; }
    public int TongSoHoSo { get; set; }
    public decimal TongTienThuLePhi { get; set; }
    public string TongTienThuLePhiHienThi => $"{TongTienThuLePhi:N0} đ";
    public int TongChenhLech => TongBienNhapKho - TongBienXuatKho;
    public string TongChenhLechHienThi => TongChenhLech >= 0 ? $"+{TongChenhLech}" : $"{TongChenhLech}";
}

#endregion

#region 7. DTO Dashboard Tổng Hợp (Gói chung tất cả khoanh đỏ cho 1 lần gọi)

/// <summary>
/// Kết quả toàn diện của trang Báo cáo Phân tích Biến thiên (Gói cả 5 khối dữ liệu).
/// </summary>
public class BaoCaoBienThienDashboardResponse
{
    /// <summary>
    /// Khoanh 2: Thẻ tóm tắt KPI tổng quan.
    /// </summary>
    public BaoCaoKpiTongQuanResponse KpiTongQuan { get; set; } = new();

    /// <summary>
    /// Khoanh 3: Biểu đồ 1 - Biến thiên số lượng Biển Nhập vs Biển Xuất theo thời gian.
    /// </summary>
    public BieuDoBienThienNhapXuatResponse BieuDoNhapXuat { get; set; } = new();

    /// <summary>
    /// Khoanh 4: Biểu đồ 2 - Biến thiên Tiền Lệ Phí thu từ biển xuất theo thời gian.
    /// </summary>
    public BieuDoBienThienLePhiResponse BieuDoLePhi { get; set; } = new();

    /// <summary>
    /// Khoanh 5: Cơ cấu biển xuất theo thủ tục và loại phương tiện.
    /// </summary>
    public CoCauBienXuatResponse CoCauBienXuat { get; set; } = new();

    /// <summary>
    /// Khoanh 6: Bảng số liệu biến thiên nhập - xuất - tiền thu chi tiết.
    /// </summary>
    public BangSoLieuBienThienResponse BangChiTiet { get; set; } = new();
}

#endregion

#region 8. DTOs Bộ lọc Gom theo & Mốc thời gian

/// <summary>
/// Tùy chọn trong bộ lọc 'Gom theo' (Theo tháng / Theo ngày).
/// </summary>
public class BoLocGomTheoItemDto
{
    /// <summary>
    /// Mã định danh chế độ gom nhóm gửi lên API: "Thang", "Ngay".
    /// </summary>
    public string Ma { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị trên nút bấm giao diện: "Theo tháng", "Theo ngày".
    /// </summary>
    public string Ten { get; set; } = string.Empty;

    /// <summary>
    /// Đánh dấu nút được chọn mặc định ban đầu (true: nút Theo tháng).
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Mô tả chi tiết chức năng của chế độ gom nhóm.
    /// </summary>
    public string MoTa { get; set; } = string.Empty;
}

/// <summary>
/// Tùy chọn trong bộ lọc 'Mốc thời gian' chọn nhanh ([Toàn bộ], [Tháng 8/2026], [Tháng 9/2026], [Tháng 10/2026], [Quý 3/2026]).
/// </summary>
public class BoLocMocThoiGianItemDto
{
    /// <summary>
    /// Mã định danh mốc thời gian gửi lên API: "ToanBo", "Thang8", "Thang9", "Thang10", "Quy3"...
    /// </summary>
    public string Ma { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị trên nút pill filter: "Toàn bộ", "Tháng 8/2026", "Tháng 9/2026", "Tháng 10/2026", "Quý 3/2026".
    /// </summary>
    public string Ten { get; set; } = string.Empty;

    /// <summary>
    /// Đánh dấu nút được active mặc định ban đầu (true: nút Toàn bộ).
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Ngày bắt đầu của mốc thời gian (UTC).
    /// </summary>
    public DateTime TuNgay { get; set; }

    /// <summary>
    /// Ngày kết thúc của mốc thời gian (UTC).
    /// </summary>
    public DateTime DenNgay { get; set; }

    /// <summary>
    /// Chuỗi ngày bắt đầu định dạng dd/MM/yyyy để FE tiện bind vào DatePicker.
    /// </summary>
    public string TuNgayHienThi => TuNgay.ToString("dd/MM/yyyy");

    /// <summary>
    /// Chuỗi ngày kết thúc định dạng dd/MM/yyyy để FE tiện bind vào DatePicker.
    /// </summary>
    public string DenNgayHienThi => DenNgay.ToString("dd/MM/yyyy");

    /// <summary>
    /// Loại mốc thời gian: "All", "Month", "Quarter", "Custom".
    /// </summary>
    public string LoaiMoc { get; set; } = "Month";
}

#endregion

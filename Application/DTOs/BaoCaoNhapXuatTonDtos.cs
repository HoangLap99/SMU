using System.ComponentModel.DataAnnotations;

namespace SMU.Application.DTOs;

#region 0. BỘ LỌC DÙNG CHUNG CHUẨN HÓA THEO API /TonKho/tonghop

/// <summary>
/// Bộ lọc dùng chung cho tất cả các báo cáo Nhập - Xuất - Tồn.
/// Bổ sung đầy đủ 100% các tiêu chí lọc tương đồng với API /TonKho/tonghop (FilterTonKhoRequest).
/// </summary>
public class FilterBaoCaoCommonRequest
{
    /// <summary>
    /// Từ khóa tìm kiếm số biển (Ví dụ: "29E-895.05", "30B-532...").
    /// </summary>
    public string? TimSoBien { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm chung (Keyword).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm (TimKiem tương thích ngược).
    /// </summary>
    public string? TimKiem { get; set; }

    /// <summary>
    /// Đầu biển cần lọc (Ví dụ: "29AH-316", "30B-532", "29E-895" hoặc "30B").
    /// </summary>
    public string? DauBien { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm Đầu biển (TimDauBien tương thích ngược).
    /// </summary>
    public string? TimDauBien { get; set; }

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
    /// Từ ngày nhập kho / ngày báo cáo (dd/MM/yyyy) theo chuẩn TonKho.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Đến ngày nhập kho / ngày báo cáo (dd/MM/yyyy) theo chuẩn TonKho.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Ngày bắt đầu kỳ báo cáo (tương thích ngược cho TuNgay).
    /// </summary>
    public DateTime? TuNgay { get; set; }

    /// <summary>
    /// Ngày kết thúc kỳ báo cáo (tương thích ngược cho DenNgay).
    /// </summary>
    public DateTime? DenNgay { get; set; }

    /// <summary>
    /// ID Khách hàng / Đơn vị sở hữu kho (nếu null sẽ lấy tự động từ tài khoản hiện tại).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Helper: Lấy từ ngày thực tế (ưu tiên TuNgay nếu có, hoặc FromDate).
    /// </summary>
    public DateTime? GetTuNgay() => TuNgay ?? FromDate;

    /// <summary>
    /// Helper: Lấy đến ngày thực tế (ưu tiên DenNgay nếu có, hoặc ToDate).
    /// </summary>
    public DateTime? GetDenNgay() => DenNgay ?? ToDate;

    /// <summary>
    /// Helper: Lấy từ khóa tìm kiếm biển số / từ khóa chung.
    /// </summary>
    public string? GetSearchKeyword()
    {
        if (!string.IsNullOrWhiteSpace(TimSoBien)) return TimSoBien.Trim();
        if (!string.IsNullOrWhiteSpace(Keyword)) return Keyword.Trim();
        if (!string.IsNullOrWhiteSpace(TimKiem)) return TimKiem.Trim();
        return null;
    }

    /// <summary>
    /// Helper: Lấy thông tin lọc đầu biển.
    /// </summary>
    public string? GetDauBienFilter()
    {
        if (!string.IsNullOrWhiteSpace(DauBien)) return DauBien.Trim();
        if (!string.IsNullOrWhiteSpace(TimDauBien)) return TimDauBien.Trim();
        return null;
    }
}

#endregion

/// <summary>
/// Yêu cầu lọc Báo cáo Nhập - Xuất - Tồn Biển số xe.
/// Tương thích hoàn toàn với bộ lọc của API /TonKho/tonghop.
/// </summary>
public class FilterBaoCaoNhapXuatTonRequest : FilterBaoCaoCommonRequest
{
}

/// <summary>
/// Toàn bộ dữ liệu hiển thị Báo cáo Nhập - Xuất - Tồn Biển số xe.
/// Đồng bộ tuyệt đối giữa màn hình hiển thị và bản in A4.
/// </summary>
public class BaoCaoNhapXuatTonResponse
{
    /// <summary>
    /// Tiêu đề báo cáo: "BÁO CÁO NHẬP - XUẤT - TỒN BIỂN SỐ XE".
    /// </summary>
    public string TieuDe { get; set; } = "BÁO CÁO NHẬP - XUẤT - TỒN BIỂN SỐ XE";

    /// <summary>
    /// Tiêu đề kỳ báo cáo: "Kỳ báo cáo từ ngày 01/08/2026 đến ngày 31/10/2026".
    /// </summary>
    public string KyBaoCaoHienThi { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú đồng bộ in ấn: "Dữ liệu màn hình và bản in A4 được đồng bộ tuyệt đối theo các bộ lọc hiện tại."
    /// </summary>
    public string GhiChuInA4 { get; set; } = "Dữ liệu màn hình và bản in A4 được đồng bộ tuyệt đối theo các bộ lọc hiện tại.";

    /// <summary>
    /// Ngày bắt đầu (UTC).
    /// </summary>
    public DateTime TuNgay { get; set; }

    /// <summary>
    /// Ngày kết thúc (UTC).
    /// </summary>
    public DateTime DenNgay { get; set; }

    /// <summary>
    /// Chuỗi ngày bắt đầu hiển thị (01/08/2026).
    /// </summary>
    public string TuNgayHienThi => TuNgay.ToString("dd/MM/yyyy");

    /// <summary>
    /// Chuỗi ngày kết thúc hiển thị (31/10/2026).
    /// </summary>
    public string DenNgayHienThi => DenNgay.ToString("dd/MM/yyyy");

    /// <summary>
    /// Tổng số dòng dữ liệu (Ví dụ: 34).
    /// </summary>
    public int TongSoDong { get; set; }

    /// <summary>
    /// Nhãn hiển thị số dòng dữ liệu ở góc phải: "34 dòng dữ liệu".
    /// </summary>
    public string TongSoDongHienThi => $"{TongSoDong} dòng dữ liệu";

    /// <summary>
    /// Danh sách chi tiết các dòng Nhập - Xuất - Tồn theo từng Đầu biển.
    /// </summary>
    public List<BaoCaoNhapXuatTonItemDto> Items { get; set; } = new();

    /// <summary>
    /// Dòng TỔNG CỘNG ở chân bảng báo cáo.
    /// </summary>
    public BaoCaoNhapXuatTonTongCongDto TongCong { get; set; } = new();
}

/// <summary>
/// Một dòng dữ liệu trong Báo cáo Nhập - Xuất - Tồn (theo từng Đầu biển + Loại xe + Màu biển).
/// </summary>
public class BaoCaoNhapXuatTonItemDto
{
    /// <summary>
    /// Số thứ tự dòng (1, 2, 3...).
    /// </summary>
    public int Stt { get; set; }

    /// <summary>
    /// Nhóm dải Đầu biển (Ví dụ: "29A-036", "29AA-000", "29AC-963"...).
    /// </summary>
    public string DauBien { get; set; } = string.Empty;

    public string? LoaiXeId { get; set; }

    /// <summary>
    /// Loại xe hiển thị (Ví dụ: "Ô tô", "Xe máy"...).
    /// </summary>
    public string LoaiXeTen { get; set; } = "Ô tô";

    public string? MauBienSoId { get; set; }

    /// <summary>
    /// Màu biển số hiển thị (Ví dụ: "Trắng", "Xanh", "Vàng"...).
    /// </summary>
    public string MauBienSoTen { get; set; } = "Trắng";

    /// <summary>
    /// Mã màu hex hiển thị badge trên giao diện (Ví dụ: "#FFFFFF", "#3B82F6", "#EAB308").
    /// </summary>
    public string MauBienHex { get; set; } = "#FFFFFF";

    /// <summary>
    /// Tồn đầu kỳ: Số lượng biển còn tồn trong kho trước thời điểm TuNgay.
    /// </summary>
    public int TonDauKy { get; set; }

    /// <summary>
    /// Nhập trong kỳ: Số lượng biển được nhập kho trong khoảng từ TuNgay đến DenNgay.
    /// </summary>
    public int NhapTrongKy { get; set; }

    /// <summary>
    /// Xuất trong kỳ: Số lượng biển đã xuất kho / đã cấp phát trong khoảng từ TuNgay đến DenNgay.
    /// </summary>
    public int XuatTrongKy { get; set; }

    /// <summary>
    /// Tồn cuối kỳ = Tồn đầu kỳ + Nhập trong kỳ - Xuất trong kỳ.
    /// </summary>
    public int TonCuoiKy => TonDauKy + NhapTrongKy - XuatTrongKy;
}

/// <summary>
/// Dòng TỔNG CỘNG của Báo cáo Nhập - Xuất - Tồn.
/// </summary>
public class BaoCaoNhapXuatTonTongCongDto
{
    /// <summary>
    /// Tổng tồn đầu kỳ của tất cả các đầu biển.
    /// </summary>
    public int TongTonDauKy { get; set; }

    /// <summary>
    /// Tổng nhập trong kỳ của tất cả các đầu biển.
    /// </summary>
    public int TongNhapTrongKy { get; set; }

    /// <summary>
    /// Tổng xuất trong kỳ của tất cả các đầu biển.
    /// </summary>
    public int TongXuatTrongKy { get; set; }

    /// <summary>
    /// Tổng tồn cuối kỳ = Tổng đầu kỳ + Tổng nhập - Tổng xuất (Ví dụ: 591 biển).
    /// </summary>
    public int TongTonCuoiKy => TongTonDauKy + TongNhapTrongKy - TongXuatTrongKy;

    /// <summary>
    /// Nhãn hiển thị tổng tồn cuối kỳ ở góc dưới bên phải: "TỔNG CỘNG TỒN CUỐI KỲ: 591 biển".
    /// </summary>
    public string TongTonCuoiKyHienThi => $"TỔNG CỘNG TỒN CUỐI KỲ: {TongTonCuoiKy} biển";
}

/// <summary>
/// Dữ liệu mẫu phục vụ xuất bản in A4 (Print / Export Preview).
/// </summary>
public class BaoCaoNhapXuatTonPrintDataDto
{
    public string TenDonVi { get; set; } = "CÔNG AN THÀNH PHỐ HÀ NỘI";
    public string TenBoPhan { get; set; } = "ĐỘI CẢNH SÁT GIAO THÔNG - TRẬT TỰ";
    public string TieuDe { get; set; } = "BÁO CÁO NHẬP - XUẤT - TỒN BIỂN SỐ XE";
    public string KyBaoCao { get; set; } = string.Empty;
    public DateTime NgayXuatBaoCao { get; set; } = DateTime.UtcNow;
    public string NguoiLapBieu { get; set; } = string.Empty;
    public BaoCaoNhapXuatTonResponse Data { get; set; } = new();
}

#region 2. BÁO CÁO BIỂN SỐ TỒN KHO TẠI THỜI ĐIỂM (Tab 3)

/// <summary>
/// Yêu cầu lọc Báo cáo Biển số tồn kho tại thời điểm.
/// Kế thừa và đồng bộ 100% bộ lọc của API /TonKho/tonghop.
/// </summary>
public class FilterBaoCaoTonTaiThoiDiemRequest : FilterBaoCaoCommonRequest
{
    /// <summary>
    /// Mốc ngày chốt tồn kho (Ví dụ: 31/08/2026).
    /// Nếu không truyền, hệ thống sẽ tự động dùng DenNgay hoặc ToDate.
    /// </summary>
    public DateTime? ChotTonTaiNgay { get; set; }

    /// <summary>
    /// Helper: Lấy ngày chốt tồn kho thực tế.
    /// </summary>
    public DateTime? GetChotNgay() => ChotTonTaiNgay ?? GetDenNgay();
}

public class BaoCaoTonTaiThoiDiemResponse
{
    public string TieuDe { get; set; } = "BÁO CÁO BIẾN SỐ TỒN KHO TẠI THỜI ĐIỂM";
    public string ChotTonTaiNgayHienThi { get; set; } = string.Empty;
    public DateTime ChotTonTaiNgay { get; set; }
    public int TongSoDong { get; set; }
    public string TongSoDongHienThi => $"{TongSoDong} dòng dữ liệu";
    public int TongSoLuongBienTon { get; set; }
    public string TongSoLuongBienTonHienThi => $"TỔNG SỐ LƯỢNG BIỂN TỒN: {TongSoLuongBienTon} biển";
    public List<BaoCaoTonTaiThoiDiemItemDto> Items { get; set; } = new();
}

public class BaoCaoTonTaiThoiDiemItemDto
{
    public int Stt { get; set; }
    public string BienKiemSoat { get; set; } = string.Empty;
    public string DauBien { get; set; } = string.Empty;
    public string LoaiXeTen { get; set; } = "Ô tô";
    public string MauBienSoTen { get; set; } = "Trắng";
    public DateTime NgayNhapKho { get; set; }
    public string NgayNhapKhoHienThi => NgayNhapKho.ToString("dd/MM/yyyy");
    public string TrangThai { get; set; } = "Biển được bấm (Tồn kho)";
}

#endregion

#region 3. BÁO CÁO TIỀN THU THEO BIÊN LAI VÀ THỦ TỤC (Tab 4)

/// <summary>
/// Yêu cầu lọc Báo cáo Tiền thu theo biên lai và thủ tục.
/// Kế thừa và đồng bộ 100% bộ lọc của API /TonKho/tonghop.
/// </summary>
public class FilterBaoCaoTienThuBienLaiRequest : FilterBaoCaoCommonRequest
{
    /// <summary>
    /// ID Danh mục Loại thủ tục (LoaiThuTuc). Để trống lấy tất cả.
    /// </summary>
    public string? LoaiThuTucId { get; set; }
}

public class BaoCaoTienThuBienLaiResponse
{
    public string TieuDe { get; set; } = "BÁO CÁO TIỀN THU THEO BIÊN LAI VÀ THỦ TỤC";
    public string KyBaoCaoHienThi { get; set; } = string.Empty;
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public int TongSoDong { get; set; }
    public string TongSoDongHienThi => $"{TongSoDong} dòng dữ liệu";
    public decimal TongTienThuLePhi { get; set; }
    public string TongTienThuLePhiHienThi => $"TỔNG TIỀN THU LỆ PHÍ: {TongTienThuLePhi:N0} đ";
    public List<BaoCaoTienThuBienLaiItemDto> Items { get; set; } = new();
}

public class BaoCaoTienThuBienLaiItemDto
{
    public int Stt { get; set; }
    public string KyHieuSoBienLai { get; set; } = string.Empty;
    public DateTime NgayBienLai { get; set; }
    public string NgayBienLaiHienThi => NgayBienLai.ToString("dd/MM/yyyy");
    public string ThuTuc { get; set; } = string.Empty;
    public string BienSo { get; set; } = string.Empty;
    public string NguoiNopTien { get; set; } = string.Empty;
    public decimal SoTienThu { get; set; }
    public string SoTienThuHienThi => $"{SoTienThu:N0} đ";
}

#endregion

#region 4. BẢNG THỐNG KÊ THỦ TỤC VÀ TIỀN THU (Tab 5)

/// <summary>
/// Yêu cầu lọc Bảng thống kê thủ tục và tiền thu.
/// Kế thừa và đồng bộ 100% bộ lọc của API /TonKho/tonghop.
/// </summary>
public class FilterBangThongKeThuTucRequest : FilterBaoCaoCommonRequest
{
    /// <summary>
    /// ID Danh mục Loại thủ tục (LoaiThuTuc). Để trống lấy tất cả.
    /// </summary>
    public string? LoaiThuTucId { get; set; }
}

public class BangThongKeThuTucResponse
{
    public string TieuDe { get; set; } = "BẢNG THỐNG KÊ THỦ TỤC VÀ TIỀN THU";
    public string MoTa { get; set; } = "Tách số lượt hồ sơ, số biển xuất kho và tiền thu";
    public string KyBaoCaoHienThi { get; set; } = string.Empty;
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public int TongSoDong { get; set; }
    public string TongSoDongHienThi => $"{TongSoDong} dòng dữ liệu";
    public decimal TongSoLuongTienThu { get; set; }
    public string TongSoLuongTienThuHienThi => $"TỔNG SỐ LƯỢNG TIỀN THU: {TongSoLuongTienThu:N0} đ";
    public List<BangThongKeThuTucItemDto> Items { get; set; } = new();
}

public class BangThongKeThuTucItemDto
{
    public int Stt { get; set; }
    public string LoaiThuTuc { get; set; } = string.Empty;
    public int SoLuotThucHien { get; set; }
    public string SoLuotThucHienHienThi => $"{SoLuotThucHien} lượt";
    public int SoBienXuatTuKho { get; set; }
    public string SoBienXuatTuKhoHienThi => $"{SoBienXuatTuKho} biển";
    public decimal TongTienThu { get; set; }
    public string TongTienThuHienThi => $"{TongTienThu:N0} đ";
}

#endregion

#region 5. BẢNG KÊ CHI TIẾT ĐỢT NHẬP BIẾN SỐ XE (Tab 6)

/// <summary>
/// Yêu cầu lọc Bảng kê chi tiết đợt nhập biển số xe.
/// Kế thừa và đồng bộ 100% bộ lọc của API /TonKho/tonghop.
/// </summary>
public class FilterBangKeChiTietDotNhapRequest : FilterBaoCaoCommonRequest
{
}

public class BangKeChiTietDotNhapResponse
{
    public string TieuDe { get; set; } = "BẢNG KÊ CHI TIẾT ĐỢT NHẬP BIẾN SỐ XE";
    public string KyBaoCaoHienThi { get; set; } = string.Empty;
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public int TongSoDong { get; set; }
    public string TongSoDongHienThi => $"{TongSoDong} dòng dữ liệu";
    public int TongBienDaTiepNhan { get; set; }
    public string TongBienDaTiepNhanHienThi => $"TỔNG BIẾN ĐÃ TIẾP NHẬN: {TongBienDaTiepNhan} biển";
    public List<BangKeChiTietDotNhapItemDto> Items { get; set; } = new();
}

public class BangKeChiTietDotNhapItemDto
{
    public int Stt { get; set; }
    public string MaDotNhap { get; set; } = string.Empty;
    public DateTime NgayNhap { get; set; }
    public string NgayNhapHienThi => NgayNhap.ToString("dd/MM/yyyy");
    public string DauBien { get; set; } = string.Empty;
    public string LoaiXeTen { get; set; } = "Ô tô";
    public int SoLuongNhap { get; set; }
    public int SoDuocBam { get; set; }
    public string CanBoTiepNhan { get; set; } = string.Empty;
}

#endregion

#region 6. BẢNG KÊ CHI TIẾT BIẾN SỐ ĐÃ XUẤT KHO (Tab 7)

/// <summary>
/// Yêu cầu lọc Bảng kê chi tiết biển số đã xuất kho.
/// Kế thừa và đồng bộ 100% bộ lọc của API /TonKho/tonghop.
/// </summary>
public class FilterBangKeChiTietXuatKhoRequest : FilterBaoCaoCommonRequest
{
}

public class BangKeChiTietXuatKhoResponse
{
    public string TieuDe { get; set; } = "BẢNG KÊ CHI TIẾT BIẾN SỐ ĐÃ XUẤT KHO";
    public string MoTa { get; set; } = "Danh sách biển đã cấp phát từ kho ngẫu nhiên";
    public string KyBaoCaoHienThi { get; set; } = string.Empty;
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public int TongSoDong { get; set; }
    public string TongSoDongHienThi => $"{TongSoDong} dòng dữ liệu";
    public int TongBienDaXuatKho { get; set; }
    public string TongBienDaXuatKhoHienThi => $"TỔNG BIẾN ĐÃ XUẤT KHO: {TongBienDaXuatKho} biển";
    public List<BangKeChiTietBienSoXuatKhoItemDto> Items { get; set; } = new();
}

public class BangKeChiTietBienSoXuatKhoItemDto
{
    public int Stt { get; set; }
    public DateTime NgayXuat { get; set; }
    public string NgayXuatHienThi => NgayXuat.ToString("dd/MM/yyyy");
    public string BienSoDaCap { get; set; } = string.Empty;
    public string DauBien { get; set; } = string.Empty;
    public string SoBienLai { get; set; } = string.Empty;
    public string NguoiNhanBien { get; set; } = string.Empty;
    public decimal LePhiThu { get; set; }
    public string LePhiThuHienThi => $"{LePhiThu:N0} đ";
    public string CanBoCap { get; set; } = string.Empty;
}

#endregion

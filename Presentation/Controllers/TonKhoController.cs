using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller Quản lý Tồn kho biển số xe:
/// 1. POST /api/TonKho/tonghop : Bảng Tổng Hợp Tồn Kho Theo Đầu Biển (áp dụng theo bộ lọc).
/// 2. POST /api/TonKho/chitiet : Danh sách các biển số CHƯA XUẤT KHO khi click vào nút "Chi tiết" ở 1 nhóm đầu biển.
/// 3. GET  /api/TonKho/boloc   : Dữ liệu cấu hình cho các dropdown bộ lọc tìm kiếm.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Quản lý Tồn Kho Biển Số")]
public class TonKhoController : ControllerBase
{
    private readonly ITonKhoService _tonKhoService;

    public TonKhoController(ITonKhoService tonKhoService)
    {
        _tonKhoService = tonKhoService;
    }

    /// <summary>
    /// API 1: (POST) Lấy Bảng Tổng Hợp Tồn Kho Theo Đầu Biển.
    /// Áp dụng theo bộ lọc (Tìm số biển, Đầu biển, Loại phương tiện, Màu biển, Trạng thái, Khoảng ngày).
    /// Trả về các cột: STT, Đầu biển, Loại phương tiện, Màu biển (badge), Tồn kho (Được bấm), Đã bấm, Tổng biển và dòng TỔNG CỘNG.
    /// </summary>
    [HttpPost("tonghop")]
    [ProducesResponseType(typeof(TongHopTonKhoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayTongHopTonKho([FromBody] FilterTonKhoRequest? request, CancellationToken cancellationToken)
    {
        var result = await _tonKhoService.LayTongHopTonKhoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: (POST) Lấy danh sách các biển số CHƯA XUẤT KHO khi click vào nút "Chi tiết" của 1 nhóm đầu biển.
    /// Chỉ lấy các biển số thỏa mãn: !DaXuatKho &amp;&amp; TrangThai == 'DuocBam' &amp;&amp; !LaBienDauGia. Có phân trang và bộ lọc từ khóa.
    /// </summary>
    [HttpPost("chitiet")]
    [ProducesResponseType(typeof(PagedResult<BienSoChuaXuatItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LayChiTietBienSoChuaXuat([FromBody] ChiTietTonKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _tonKhoService.LayDanhSachBienSoChuaXuatAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 3: (GET) Lấy dữ liệu cấu hình cho các dropdown bộ lọc tìm kiếm.
    /// Trả về danh sách đầu biển kèm số lượng tồn kho (Ví dụ: "Tất cả đầu biển (34)"), Loại phương tiện, Màu biển, Trạng thái.
    /// </summary>
    [HttpGet("boloc")]
    [ProducesResponseType(typeof(BoLocTonKhoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayThongTinBoLoc([FromQuery] string? customerId, CancellationToken cancellationToken)
    {
        var result = await _tonKhoService.LayThongTinBoLocAsync(customerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4: (POST) Lấy Ma Trận 100 Số Dải Biển từ .00 đến .99 trong bảng KhoBienSo theo Đầu biển.
    /// </summary>
    /// <remarks>
    /// ### 1. Mục đích API:
    /// Trả về toàn bộ ma trận dải 100 số (từ .00 đến .99) của một đầu biển được chọn (Ví dụ: "29AC-963").
    ///
    /// ### 2. Điều kiện hiển thị dữ liệu:
    /// - **Tự động sinh đủ 100 ô số**: Hệ thống luôn sinh đủ 100 phần tử tương ứng từ index `00` đến `99` (ký hiệu hiển thị từ `.00` đến `.99`).
    /// - **Tra cứu trong bảng KhoBienSo**:
    ///   - Tìm các biển số có `BienKiemSoat` bắt đầu bằng `DauBien` (Ví dụ: `29AC-963.xx`).
    ///   - Khớp 2 chữ số đuôi với số thứ tự ô từ 00 đến 99.
    ///   - Lọc theo đơn vị người dùng (`CustomerId == currentCustomerId` hoặc `CustomerId == null`).
    /// - **Biển ĐÃ CÓ trong kho (`DaCoTrongKho = true`)**:
    ///   - Hiển thị đầy đủ thông tin: ID biển, biển kiểm soát, loại phương tiện, màu biển, ngày nhập, trạng thái.
    /// - **Biển CHƯA CÓ trong kho (`DaCoTrongKho = false`)**:
    ///   - Trạng thái là `ChuaNhap` ("TRỐNG"), ô số mờ màu xám nhạt để nhận biết số còn trống trong dải.
    ///
    /// ### 3. Quy tắc xác định trạng thái và màu sắc (Màu sắc lấy từ đâu):
    /// Hệ thống tự động xác định trạng thái và mã màu hiển thị dựa trên các trường nghiệp vụ trong bảng `KhoBienSo`:
    ///
    /// 1. **Biển được bấm (Tồn kho) - Xanh lá (`#10B981` / class `success`)**:
    ///    - **Điều kiện**: Biển có trong kho, `!LaBienDauGia`, `!DaXuatKho` và `TrangThai == "DuocBam"`.
    ///    - **Text hiển thị**: `ĐƯỢC BẤM`.
    ///    - **Ý nghĩa**: Biển số tồn kho sẵn sàng bấm cấp phát cho người dân.
    ///
    /// 2. **Biển đã bấm - Xám đen (`#374151` / class `dark`)**:
    ///    - **Điều kiện**: `DaXuatKho == true` HOẶC `TrangThai == "DaBam"`.
    ///    - **Text hiển thị**: `ĐÃ BẤM`.
    ///    - **Ý nghĩa**: Biển số đã được bấm / đã cấp phát xuất kho.
    ///
    /// 3. **Chờ đấu giá - Tím (`#8B5CF6` / class `purple`)**:
    ///    - **Điều kiện**: `LaBienDauGia == true` và chưa đấu giá thành công (`TrangThai == "ChoDauGia"`).
    ///    - **Text hiển thị**: `CHỜ ĐẤU GIÁ`.
    ///    - **Ý nghĩa**: Biển số đưa vào diện kho biển đấu giá theo quy định.
    ///
    /// 4. **Đấu giá thành công - Cam/Vàng (`#F59E0B` / class `warning`)**:
    ///    - **Điều kiện**: `LaBienDauGia == true` và (`TrangThai == "DauGiaThanhCong"` hoặc đã làm thủ tục xuất kho).
    ///    - **Text hiển thị**: `ĐẤU GIÁ THÀNH CÔNG`.
    ///    - **Ý nghĩa**: Biển số đấu giá đã có người trúng và hoàn tất thủ tục.
    ///
    /// 5. **Chưa nhập (Trống) - Xám nhạt (`#E5E7EB` / class `empty`)**:
    ///    - **Điều kiện**: Số thứ tự trong dải chưa có bản ghi tương ứng trong bảng `KhoBienSo` (`DaCoTrongKho == false`).
    ///    - **Text hiển thị**: `TRỐNG`.
    ///    - **Ý nghĩa**: Số còn trống trong dải 100 số, chưa khai báo nhập kho.
    ///
    /// 6. **Thu hồi - Đỏ (`#EF4444` / class `danger`)**:
    ///    - **Điều kiện**: `TrangThai == "ThuHoi"`.
    ///    - **Text hiển thị**: `THU HỒI`.
    /// </remarks>
    [HttpPost("matran")]
    [ProducesResponseType(typeof(MaTranDaiSoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LayMaTranDaiSoPost([FromBody] MaTranDaiSoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _tonKhoService.LayMaTranDaiSoAsync(request.DauBien, request.CustomerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 5: (POST) Cập nhật trạng thái của 1 ô số trong ma trận 100 số ("Bấm vào ô số để sửa trạng thái").
    /// </summary>
    [HttpPost("suatrangthai")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CapNhatTrangThaiSo([FromBody] CapNhatTrangThaiSoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _tonKhoService.CapNhatTrangThaiSoAsync(request, cancellationToken);
        return Ok(new { success, message = $"Đã cập nhật trạng thái ô số {request.DauBien}.{request.SoDuoi} sang {request.TrangThaiMoi} thành công." });
    }

    /// <summary>
    /// API 6: (POST) Xuất tệp tin Excel Biên bản kiểm kê danh sách biển số tồn trong KhoBienSo.
    /// Định dạng mẫu chuẩn 100% theo MauBaoCao/Tồn tháng 8.25.xlsx (Biên bản kiểm kê Công an phường, bảng 6 cột STT|BKS).
    /// </summary>
    /// <param name="request">Thông tin bộ lọc, đơn vị và thành phần tham gia kiểm kê (tùy chọn).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tệp tin Excel .xlsx</returns>
    [HttpPost("export-bien-ton")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBienTonPost([FromBody] ExportBienTonExcelRequest? request, CancellationToken cancellationToken)
    {
        var (fileBytes, contentType, fileName) = await _tonKhoService.ExportBienTonExcelAsync(request, cancellationToken);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// API 7: (GET) Tải trực tiếp tệp tin Excel Biên bản kiểm kê danh sách biển số tồn trong KhoBienSo.
    /// Hỗ trợ tải trực tiếp qua đường dẫn trình duyệt (Query string).
    /// </summary>
    /// <param name="request">Các tham số truy vấn bộ lọc (tùy chọn).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tệp tin Excel .xlsx</returns>
    [HttpGet("export-bien-ton")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBienTonGet([FromQuery] ExportBienTonExcelRequest? request, CancellationToken cancellationToken)
    {
        var (fileBytes, contentType, fileName) = await _tonKhoService.ExportBienTonExcelAsync(request, cancellationToken);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// API 8: (GET) Lấy danh sách cán bộ thuộc đơn vị của user đang đăng nhập (kèm Cấp bậc CAND và Chức vụ).
    /// Hỗ trợ nạp dropdown chọn Lãnh đạo ký duyệt và Cán bộ tham gia kiểm kê trên giao diện trước khi xuất file.
    /// </summary>
    /// <param name="customerId">Mã đơn vị (tùy chọn, mặc định lấy theo đơn vị của người dùng đang đăng nhập).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Danh sách cán bộ trong đơn vị.</returns>
    [HttpGet("can-bo")]
    [ProducesResponseType(typeof(IReadOnlyList<CanBoDonViResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSachCanBoDonVi([FromQuery] string? customerId, CancellationToken cancellationToken)
    {
        var result = await _tonKhoService.LayDanhSachCanBoDonViAsync(customerId, cancellationToken);
        return Ok(result);
    }
}

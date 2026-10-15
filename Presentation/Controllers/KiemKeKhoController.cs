using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller Quản lý Kiểm kê kho biển số xe:
/// 1. POST /api/KiemKeKho/khoitao : Khởi tạo dữ liệu phiếu kiểm kê từ tồn kho hệ thống (các biển chưa xuất trong bảng KhoBienSo).
/// 2. POST /api/KiemKeKho/chitietbien : Dữ liệu cho popup "Xác định biển thiếu/thừa: Dải {DauBien}" khi click vào nút "Danh sách biển".
/// 3. POST /api/KiemKeKho/set : Thêm mới và cập nhật phiếu kiểm kê (Lưu nháp / Xác nhận hoàn tất).
/// 4. POST /api/KiemKeKho/danhsach : Lấy danh sách phiếu kiểm kê có phân trang và bộ lọc.
/// 5. GET  /api/KiemKeKho/{id} : Lấy thông tin chi tiết 1 phiếu kiểm kê theo Id.
/// 6. GET  /api/KiemKeKho/delete/{id} : Xóa phiếu kiểm kê theo Id.
/// 7. POST /api/KiemKeKho/xacdinhdaubien : Tự động xác định dải đầu biển số từ biển kiểm soát.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Quản lý Kiểm Kê Kho Biển Số")]
public class KiemKeKhoController : ControllerBase
{
    private readonly IKiemKeKhoService _kiemKeKhoService;

    public KiemKeKhoController(IKiemKeKhoService kiemKeKhoService)
    {
        _kiemKeKhoService = kiemKeKhoService;
    }

    /// <summary>
    /// API 1: (POST) Khởi tạo dữ liệu phiếu kiểm kê từ bảng KhoBienSo đối với các biển chưa xuất.
    /// </summary>
    /// <remarks>
    /// ### Mục đích:
    /// Tự động lấy toàn bộ biển số trong bảng `KhoBienSo` thỏa mãn điều kiện CHƯA XUẤT KHO:
    /// `!DaXuatKho &amp;&amp; TrangThai == 'DuocBam' &amp;&amp; !LaBienDauGia`.
    ///
    /// Gom nhóm theo dải đầu biển (DauBien) và Màu biển (MauBien):
    /// - Tính số lượng tồn hệ thống (TonHeThong).
    /// - Gán mặc định số lượng thực tế kiểm đếm = Tồn hệ thống (ThucTe = TonHeThong).
    /// - Chênh lệch ban đầu = 0 (Khớp sổ sách).
    /// - Tự động gợi ý mã phiếu kiểm kê định dạng: `KK-YYYY-MM-DD-xxx`.
    /// </remarks>
    [HttpPost("khoitao")]
    [ProducesResponseType(typeof(KhoiTaoKiemKeKhoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> KhoiTaoPhieuKiemKe([FromBody] KhoiTaoKiemKeKhoRequest? request, CancellationToken cancellationToken)
    {
        var result = await _kiemKeKhoService.KhoiTaoPhieuKiemKeAsync(request ?? new KhoiTaoKiemKeKhoRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: (POST) Lấy dữ liệu cho Popup "Xác định biển thiếu/thừa: Dải {DauBien} ({MauBien})" khi click vào nút "Danh sách biển".
    /// </summary>
    /// <remarks>
    /// ### Mục đích:
    /// Khi người dùng nhấn nút "Danh sách biển" ở 1 dòng dải đầu biển trên bảng kiểm kê:
    /// - Lấy danh sách biển số cụ thể trong kho chưa xuất thuộc dải đó (`KhoBienSo`).
    /// - Trả về trạng thái kiểm kê ("CoMat" / "Thieu").
    /// - Trả về danh sách biển phát hiện thừa thực tế (chưa có trong phần mềm).
    /// - Trả về 4 chỉ số thống kê: Tồn sổ sách, Biển có mặt, Biển báo thiếu, Biển phát hiện thừa.
    /// </remarks>
    [HttpPost("chitietbien")]
    [ProducesResponseType(typeof(ChiTietBienKiemKeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LayChiTietBienPopup([FromBody] ChiTietBienKiemKeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _kiemKeKhoService.LayChiTietBienPopupAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 3: (POST) Thêm mới và cập nhật phiếu kiểm kê kho (Ghi lại thường / Xác nhận hoàn tất).
    /// </summary>
    /// <remarks>
    /// ### 1. Mục đích:
    /// Lưu thông tin phiếu kiểm kê (tương tự như luồng của Nhập kho và Xuất kho):
    /// - Thêm mới (khi `Id == null`): Lưu phiếu kiểm kê, bảng tổng hợp nhóm dải đầu biển và danh sách chi tiết biển thừa/thiếu.
    /// - Cập nhật (khi có `Id`): Chỉnh sửa phiếu kiểm kê.
    ///
    /// ### 2. Cơ chế xử lý trạng thái &amp; Đồng bộ bảng KhoBienSo:
    /// - **Ghi lại thường / Lưu nháp (`XacNhanHoanTat = false` hoặc `TrangThai = "LuuNhap"`)**:
    ///   - Chỉ lưu trữ dữ liệu vào các bảng kiểm kê (`KiemKeKho`, `KiemKeKhoNhomDauBien`, `KiemKeKhoChiTietBien`).
    ///   - **KHÔNG** làm thay đổi dữ liệu trong bảng `KhoBienSo`.
    ///
    /// - **Xác nhận hoàn tất (`XacNhanHoanTat = true` hoặc `TrangThai = "HoanTat"`)**:
    ///   - Chốt số liệu phiếu kiểm kê sang trạng thái `HoanTat`.
    ///   - **Đồng bộ tự động vào bảng `KhoBienSo` để cân bằng tồn kho theo thực tế**:
    ///     + **Biển BÁO THIẾU (`TrangThaiKiemKe == "Thieu"`)**: Hệ thống thực hiện **XUẤT KHO** đối với những biển số này (`DaXuatKho = true`, `TrangThai = "DaBam"`, `TrangThaiTen = "Đã xuất (Kiểm kê báo thiếu)"`).
    ///     + **Biển PHÁT HIỆN THỪA (`LoaiBien == "BienThua"` hoặc `TrangThaiKiemKe == "Thua"`)**: Hệ thống thực hiện **NHẬP KHO** thêm mới vào bảng `KhoBienSo` (`DaXuatKho = false`, `TrangThai = "DuocBam"`, `TrangThaiTen = "Biển được bấm (Tồn kho)"`).
    /// </remarks>
    [HttpPost("set")]
    [ProducesResponseType(typeof(KiemKeKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Set([FromBody] SetKiemKeKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _kiemKeKhoService.SetKiemKeKhoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4: (POST) Lấy danh sách phiếu kiểm kê kho có phân trang và bộ lọc.
    /// </summary>
    [HttpPost("danhsach")]
    [ProducesResponseType(typeof(PagedResult<KiemKeKhoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSach([FromBody] PagedKiemKeKhoRequest? request, CancellationToken cancellationToken)
    {
        var result = await _kiemKeKhoService.LayDanhSachKiemKeAsync(request ?? new PagedKiemKeKhoRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 5: (GET) Lấy thông tin chi tiết 1 phiếu kiểm kê kho theo Id.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KiemKeKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _kiemKeKhoService.LayChiTietKiemKeAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu kiểm kê kho với Id '{id}'." });
        }

        return Ok(result);
    }

    /// <summary>
    /// API 6: (GET) Xóa phiếu kiểm kê kho theo Id.
    /// </summary>
    [HttpGet("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Xoa([FromRoute] string id, CancellationToken cancellationToken)
    {
        var success = await _kiemKeKhoService.XoaKiemKeKhoAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu kiểm kê kho với Id '{id}' để xóa." });
        }

        return Ok(new { success = true, message = $"Đã xóa phiếu kiểm kê kho với Id '{id}' thành công." });
    }

    /// <summary>
    /// API 7: (POST) Tự động xác định dải đầu biển số từ biển số kiểm soát.
    /// </summary>
    /// <remarks>
    /// ### Mục đích:
    /// Khi phát hiện hoặc quét biển số thừa (Ví dụ: "30B-207.66", "29E-794.02", "29AH-316.05"):
    /// - Tự động bóc tách đầu biển số (Ví dụ: "30B-207", "29E-794", "29AH-316").
    /// - Kiểm tra xem biển đã có trong kho hay chưa.
    /// - Tự động tra cứu màu biển tương ứng.
    /// - Dùng để tự động phân bổ biển thừa vào đúng nhóm dải đầu biển trên giao diện hoặc kiểm tra trước khi lưu.
    /// </remarks>
    [HttpPost("xacdinhdaubien")]
    [ProducesResponseType(typeof(XacDinhDauBienResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> XacDinhDauBien([FromBody] XacDinhDauBienRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.BienKiemSoat))
        {
            return BadRequest(new { message = "Vui lòng truyền biển kiểm soát cần xác định." });
        }

        var result = await _kiemKeKhoService.XacDinhDauBienTuBienSoAsync(request.BienKiemSoat, request.CustomerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 7b: (GET) Tự động xác định dải đầu biển số từ biển số kiểm soát qua query string.
    /// </summary>
    [HttpGet("xacdinhdaubien")]
    [ProducesResponseType(typeof(XacDinhDauBienResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> XacDinhDauBienGet([FromQuery] string bienKiemSoat, [FromQuery] string? customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bienKiemSoat))
        {
            return BadRequest(new { message = "Vui lòng truyền biển kiểm soát cần xác định." });
        }

        var result = await _kiemKeKhoService.XacDinhDauBienTuBienSoAsync(bienKiemSoat, customerId, cancellationToken);
        return Ok(result);
    }
}

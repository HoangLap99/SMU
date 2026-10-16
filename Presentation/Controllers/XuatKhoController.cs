using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller quản lý Lập thủ tục &amp; Biên lai thu tiền (Xuất kho biển số xe):
/// 1. GET /api/XuatKho/biensokhokhadung : Lấy danh sách biển số chưa xuất kho khả dụng trong bảng KhoBienSo.
/// 2. GET /api/XuatKho/mucthukhuvuc : Lấy số tiền và căn cứ pháp lý dựa vào nội dung lệ phí và khu vực áp dụng.
/// 3. GET /api/XuatKho/sobienlai/{kyHieuQuyenId} : Lấy thông tin quyển và gợi ý số biên lai tiếp theo.
/// 4. POST /api/XuatKho/set : Thêm mới và cập nhật phiếu Lập thủ tục &amp; Biên lai xuất kho.
/// 5. POST /api/XuatKho/danhsach : Lấy danh sách phiếu xuất kho có phân trang và bộ lọc.
/// 6. GET /api/XuatKho/{id} : Lấy chi tiết phiếu xuất kho theo Id.
/// 7. GET /api/XuatKho/delete/{id} : Xóa phiếu xuất kho theo Id và khôi phục biển số về kho.
/// 8. GET /api/XuatKho/huy/{id} : Hủy phiếu xuất kho và cập nhật lại trạng thái biển số trong KhoBienSo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Quản lý Xuất Kho Biển Số")]
public class XuatKhoController : ControllerBase
{
    private readonly IXuatKhoService _xuatKhoService;
    private readonly ILogHoatDongService _logHoatDongService;

    public XuatKhoController(
        IXuatKhoService xuatKhoService,
        ILogHoatDongService logHoatDongService)
    {
        _xuatKhoService = xuatKhoService;
        _logHoatDongService = logHoatDongService;
    }

    /// <summary>
    /// API 1: (GET) Lấy danh sách biển số chưa xuất kho khả dụng trong bảng KhoBienSo.
    /// Trả về danh sách biển số (format hiển thị "30B-207.66 • Ô tô (Biển Trắng - Đầu 30B-207)") và tổng số biển khả dụng trong kho.
    /// </summary>
    [HttpGet("biensokhokhadung")]
    [ProducesResponseType(typeof(DanhSachBienSoKhaDungResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSachBienSoKhaDung([FromQuery] FilterBienSoKhaDungRequest? request, CancellationToken cancellationToken)
    {
        var result = await _xuatKhoService.LayDanhSachBienSoKhaDungAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: (GET) Dựa vào nội dung lệ phí (MucThuLePhiId) và khu vực áp dụng (KhuVucApDungId) để lấy số tiền từ bảng Mức thu lệ phí khu vực (TT 155/2025/TT-BTC).
    /// </summary>
    [HttpGet("mucthukhuvuc")]
    [ProducesResponseType(typeof(ThongTinMucThuTheoKhuVucResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LayMucThuTheoKhuVuc(
        [FromQuery] string mucThuLePhiId,
        [FromQuery] string khuVucApDungId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mucThuLePhiId) || string.IsNullOrWhiteSpace(khuVucApDungId))
        {
            return BadRequest(new { message = "Vui lòng truyền đầy đủ mucThuLePhiId và khuVucApDungId." });
        }

        var result = await _xuatKhoService.LayMucThuTheoKhuVucAsync(mucThuLePhiId, khuVucApDungId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy thông tin mức thu lệ phí tương ứng với nội dung và khu vực này." });
        }

        return Ok(result);
    }

    /// <summary>
    /// API 3: (GET) Lấy thông tin quyển biên lai và gợi ý số biên lai tiếp theo theo KyHieuQuyenBienLaiId.
    /// </summary>
    [HttpGet("sobienlai/{kyHieuQuyenId}")]
    [ProducesResponseType(typeof(SoBienLaiTiepTheoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LaySoBienLaiTiepTheo([FromRoute] string kyHieuQuyenId, CancellationToken cancellationToken)
    {
        var result = await _xuatKhoService.LaySoBienLaiTiepTheoAsync(kyHieuQuyenId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy Ký hiệu quyển biên lai với Id '{kyHieuQuyenId}'." });
        }

        return Ok(result);
    }

    /// <summary>
    /// API 4: (POST) Thêm mới và cập nhật phiếu Lập thủ tục &amp; Biên lai thu tiền (Xuất kho).
    /// - Nếu DaXuatKho = true: Cập nhật biển số trong bảng KhoBienSo sang DaXuatKho = true, TrangThai = 'DaBam' và tăng số hiện tại của quyển biên lai.
    /// - Nếu DaXuatKho = false: Lưu phiếu dạng lưu tạm / dự thảo, KHÔNG thay đổi dữ liệu trong KhoBienSo.
    /// </summary>
    [HttpPost("set")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Set([FromBody] SetXuatKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _xuatKhoService.SetXuatKhoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4.1: (POST) Cập nhật phiếu Lập thủ tục &amp; Biên lai xuất kho và đồng bộ toàn bộ bảng KhoBienSo theo dữ liệu mới.
    /// Đối với trường hợp đã xuất kho: Toàn bộ dữ liệu mới (thông tin người nộp, lệ phí, số biên lai, và thông tin phương tiện/biển số nếu có) sẽ được cập nhật đồng bộ vào KhoBienSo.
    /// Nếu đổi biển số khác: Biển số cũ sẽ tự động được hoàn tác về kho (DaXuatKho = false, TrangThai = 'DuocBam') và biển số mới chuyển sang đã xuất kho.
    /// </summary>
    [HttpPost("update")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update([FromBody] SetXuatKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _xuatKhoService.UpdateXuatKhoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4.2: (PUT) Cập nhật phiếu Lập thủ tục &amp; Biên lai xuất kho theo Id và đồng bộ toàn bộ bảng KhoBienSo theo dữ liệu mới.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateById([FromRoute] string id, [FromBody] SetXuatKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        request.Id = id;
        var result = await _xuatKhoService.UpdateXuatKhoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 4.3: (POST) Xác nhận xuất kho theo Id phiếu xuất kho.
    /// Khóa phiếu (DaXuatKho = true, TrangThai = 'HoanTat') và chuyển trạng thái biển số trong KhoBienSo sang 'DaBam' (Đã bấm / Đã cấp).
    /// </summary>
    [HttpPost("{id}/xac-nhan")]
    [HttpPost("xacnhan/{id}")]
    [HttpPost("xac-nhan/{id}")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> XacNhanXuatKho([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _xuatKhoService.XacNhanXuatKhoAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 5: (POST) Lấy danh sách phiếu xuất kho có phân trang và bộ lọc.
    /// </summary>
    [HttpPost("danhsach")]
    [ProducesResponseType(typeof(PagedResult<XuatKhoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSach([FromBody] PagedXuatKhoRequest? request, CancellationToken cancellationToken)
    {
        var result = await _xuatKhoService.LayDanhSachXuatKhoAsync(request ?? new PagedXuatKhoRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 6: (GET) Lấy thông tin chi tiết 1 phiếu xuất kho theo Id.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _xuatKhoService.LayChiTietXuatKhoAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu xuất kho với Id '{id}'." });
        }

        return Ok(result);
    }

    /// <summary>
    /// API 7: (GET) Xóa phiếu xuất kho theo Id. Hoàn tác trạng thái biển số trong KhoBienSo về DaXuatKho = false, TrangThai = 'DuocBam'.
    /// </summary>
    [HttpGet("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Xoa([FromRoute] string id, CancellationToken cancellationToken)
    {
        var success = await _xuatKhoService.XoaXuatKhoAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu xuất kho với Id '{id}' để xóa." });
        }

        return Ok(new { success = true, message = $"Đã xóa phiếu xuất kho với Id '{id}' và khôi phục biển số thành công." });
    }

    /// <summary>
    /// API 8: (GET) Hủy phiếu xuất kho theo Id.
    /// Cập nhật trạng thái phiếu xuất kho sang 'DaHuy', DaXuatKho = false và khôi phục trạng thái biển số trong KhoBienSo về khả dụng (DaXuatKho = false, TrangThai = 'DuocBam').
    /// </summary>
    /// <param name="id">Mã GUID của phiếu xuất kho cần hủy.</param>
    /// <param name="lyDoHuy">Lý do hủy phiếu (tùy chọn query param: ?lyDoHuy=...).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("huy/{id}")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HuyPhieuXuatKho(
        [FromRoute] string id,
        [FromQuery] string? lyDoHuy,
        CancellationToken cancellationToken)
    {
        var request = !string.IsNullOrWhiteSpace(lyDoHuy) ? new HuyPhieuXuatKhoRequest { LyDoHuy = lyDoHuy } : null;
        var result = await _xuatKhoService.HuyPhieuXuatKhoAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 9: (GET/POST) Lấy nhật ký hoạt động của phân hệ Xuất kho &amp; Biên lai thu tiền.
    /// </summary>
    [HttpGet("log")]
    [HttpPost("log")]
    [ProducesResponseType(typeof(PagedResult<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogXuatKho([FromQuery] FilterLogHoatDongRequest? request, CancellationToken cancellationToken)
    {
        request ??= new FilterLogHoatDongRequest();
        request.LoaiNghiepVu = "XuatKho";
        var result = await _logHoatDongService.LayDanhSachLogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 10: (GET) Lấy lịch sử log của 1 phiếu xuất kho cụ thể theo Id hoặc Số biên lai.
    /// </summary>
    [HttpGet("{id}/log")]
    [ProducesResponseType(typeof(IReadOnlyList<LogHoatDongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayLogTheoPhieuXuat([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _logHoatDongService.LayLogTheoChungTuAsync(id, cancellationToken);
        return Ok(result);
    }
}

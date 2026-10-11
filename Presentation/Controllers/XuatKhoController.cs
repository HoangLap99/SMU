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
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Quản lý Xuất Kho Biển Số")]
public class XuatKhoController : ControllerBase
{
    private readonly IXuatKhoService _xuatKhoService;

    public XuatKhoController(IXuatKhoService xuatKhoService)
    {
        _xuatKhoService = xuatKhoService;
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
    /// Tự động cập nhật biển số trong bảng KhoBienSo sang DaXuatKho = true, TrangThai = 'DaBam' và tăng số hiện tại của quyển biên lai.
    /// </summary>
    [HttpPost("set")]
    [ProducesResponseType(typeof(XuatKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Set([FromBody] SetXuatKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _xuatKhoService.SetXuatKhoAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
}

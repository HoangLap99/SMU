using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller quản lý Đợt nhập kho và Kho biển số xe:
/// 1. POST /api/NhapKho/danhsach : Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
/// 2. POST /api/NhapKho/set : Thêm mới và update đợt nhập kho.
/// 3. GET /api/NhapKho/{id} : Lấy thông tin 1 đợt nhập kho theo Id.
/// 4. GET /api/NhapKho/delete/{id} : Xóa đợt nhập kho theo Id.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Quản lý Đợt Nhập Kho")]
public class NhapKhoController : ControllerBase
{
    private readonly INhapKhoService _nhapKhoService;

    public NhapKhoController(INhapKhoService nhapKhoService)
    {
        _nhapKhoService = nhapKhoService;
    }

    /// <summary>
    /// API 1: (POST) Lấy danh sách đợt nhập kho có phân trang và bộ lọc.
    /// </summary>
    [HttpPost("danhsach")]
    [ProducesResponseType(typeof(PagedResult<DotNhapKhoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayDanhSach([FromBody] PagedDotNhapKhoRequest? request, CancellationToken cancellationToken)
    {
        var result = await _nhapKhoService.LayDanhSachDotNhapAsync(request ?? new PagedDotNhapKhoRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 2: (POST) Thêm mới và update đợt nhập kho.
    /// Nếu update và DaNhapKho == true thì sẽ lưu danh sách biển số vào bảng KhoBienSo (bỏ qua biển số đấu giá LaBienDauGia == true).
    /// </summary>
    [HttpPost("set")]
    [ProducesResponseType(typeof(DotNhapKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Set([FromBody] SetDotNhapKhoRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _nhapKhoService.SetDotNhapKhoAsync(request, cancellationToken);
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
    /// API 3: (GET) Lấy thông tin 1 đợt nhập kho theo Id.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DotNhapKhoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _nhapKhoService.LayChiTietDotNhapAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy đợt nhập kho với Id '{id}'." });
        }

        return Ok(result);
    }

    /// <summary>
    /// API 4: (GET) Xóa đợt nhập kho theo Id.
    /// </summary>
    [HttpGet("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Xoa([FromRoute] string id, CancellationToken cancellationToken)
    {
        var success = await _nhapKhoService.XoaDotNhapKhoAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy đợt nhập kho với Id '{id}' để xóa." });
        }

        return Ok(new { success = true, message = $"Đã xóa đợt nhập kho với Id '{id}' thành công." });
    }

    /// <summary>
    /// API 5: (GET) Lấy thông tin tệp tin Excel mẫu nhập kho biển số xe (MauBaoCao/NhapKhoBienSo.xlsx).
    /// Output trả về object chứa filename và path (mã hóa Base64 theo cấu hình StorageFolder của appsettings)
    /// đóng vai trò là input cho API tải file /api/FileUpload/download?filename=...&amp;path=...
    /// </summary>
    [HttpGet("export-mau")]
    [HttpGet("mau-excel")]
    [ProducesResponseType(typeof(ExportMauNhapKhoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportMauNhapKhoResponse>> ExportFileMau(CancellationToken cancellationToken)
    {
        var result = await _nhapKhoService.ExportFileMauNhapKhoAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// API 6: (POST) Đọc dữ liệu danh sách biển số xe từ file Excel theo file mẫu với input là FileName (đường dẫn lấy file theo StorageFolder của appsettings).
    /// Output trả về List&lt;BienSoChiTietItemRequest&gt;.
    /// </summary>
    /// <param name="request">Yêu cầu chứa FileName đã upload lên thư mục StorageFolder.</param>
    /// <param name="fileName">Tùy chọn truyền tên file qua query string.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("import-excel")]
    [ProducesResponseType(typeof(List<BienSoChiTietItemRequest>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<BienSoChiTietItemRequest>>> ImportExcel(
        [FromBody] ImportExcelBienSoRequest? request,
        [FromQuery] string? fileName,
        CancellationToken cancellationToken)
    {
        var targetFile = !string.IsNullOrWhiteSpace(request?.FileName) ? request.FileName : fileName;
        if (string.IsNullOrWhiteSpace(targetFile))
        {
            return BadRequest(new { message = "Vui lòng truyền tên file (FileName) cần import." });
        }

        try
        {
            var result = await _nhapKhoService.ImportExcelBienSoAsync(targetFile, cancellationToken);
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi xử lý file Excel: {ex.Message}" });
        }
    }
}

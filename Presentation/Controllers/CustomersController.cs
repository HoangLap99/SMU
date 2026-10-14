using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// Controller quản lý danh sách Khách hàng / Đơn vị (Customer) và cán bộ (User và Account) trực thuộc từng đơn vị.
/// Quản lý hạn mức tài khoản (Quota) và phân quyền vai trò, cấp bậc theo UI.
/// Yêu cầu xác thực JWT Authorization cho mọi thao tác.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Quản lý Đơn vị & Cán bộ")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICurrentUserService _currentUserService;

    public CustomersController(
        ICustomerService customerService,
        ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ khách hàng / đơn vị (kèm thông tin quota và số lượng cán bộ).
    /// </summary>
    /// <param name="search">Từ khóa tìm kiếm theo tên hoặc mã khách hàng (tùy chọn).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAllCustomers(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllCustomersAsync(search, cancellationToken);
        return Ok(customers);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một khách hàng / đơn vị theo Id kèm danh sách cán bộ.
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng / đơn vị.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(string id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = $"Không tìm thấy khách hàng có Id: '{id}'."
            });
        }

        return Ok(customer);
    }

    /// <summary>
    /// Tạo mới một khách hàng / đơn vị (có thể truyền kèm danh sách cán bộ ban đầu).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var createdCustomer = await _customerService.CreateCustomerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCustomerById), new { id = createdCustomer.Id }, createdCustomer);
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng / đơn vị.
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng / đơn vị.</param>
    /// <param name="request">Thông tin cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("{id}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(
        string id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedCustomer = await _customerService.UpdateCustomerAsync(id, request, cancellationToken);
            return Ok(updatedCustomer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Xóa một khách hàng khỏi hệ thống.
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCustomer(string id, CancellationToken cancellationToken)
    {
        var deleted = await _customerService.DeleteCustomerAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = $"Không tìm thấy khách hàng có Id: '{id}' để xóa."
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Lấy thông tin định mức tài khoản đơn vị (Quota: đang hoạt động / giới hạn).
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng / đơn vị.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("{id}/quota")]
    [ProducesResponseType(typeof(CustomerQuotaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerQuotaResponse>> GetCustomerQuota(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var quota = await _customerService.GetCustomerQuotaAsync(id, cancellationToken);
            return Ok(quota);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Lấy danh sách tài khoản cán bộ trong đơn vị (hỗ trợ tìm kiếm theo tên, tài khoản, số hiệu CAND, chức vụ và lọc trạng thái).
    /// </summary>
    /// <param name="id">Mã GUID của đơn vị / khách hàng.</param>
    /// <param name="search">Từ khóa tìm kiếm.</param>
    /// <param name="isActive">Lọc theo trạng thái hoạt động (true/false, bỏ trống = tất cả).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("{id}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CustomerMemberResponse>>> GetCustomerMembers(
        string id,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var members = await _customerService.GetCustomerMembersAsync(id, search, isActive, cancellationToken);
            return Ok(members);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Thêm tài khoản cán bộ mới vào đơn vị (kiểm tra hạn mức Quota, tạo User và Account theo đơn vị như trong Modal UI).
    /// </summary>
    /// <param name="id">Mã GUID của đơn vị.</param>
    /// <param name="request">Thông tin cán bộ mới (Username, Password, FullName, CapBacId, SoHieuCAND, ChucVu, VaiTroPhanQuyenId).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(CustomerMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerMemberResponse>> AddUserToCustomer(
        string id,
        [FromBody] AddUserToCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var member = await _customerService.AddUserToCustomerAsync(id, request, cancellationToken);
            return CreatedAtAction(nameof(GetCustomerMembers), new { id }, member);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy đối tượng",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Vượt quá định mức tài khoản",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Cập nhật thông tin cán bộ trong đơn vị.
    /// </summary>
    /// <param name="id">Mã GUID của đơn vị.</param>
    /// <param name="userId">Mã GUID của cán bộ.</param>
    /// <param name="request">Thông tin cập nhật.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPut("{id}/members/{userId}")]
    [ProducesResponseType(typeof(CustomerMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerMemberResponse>> UpdateUserInCustomer(
        string id,
        string userId,
        [FromBody] UpdateUserInCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var member = await _customerService.UpdateUserInCustomerAsync(id, userId, request, cancellationToken);
            return Ok(member);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy cán bộ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Thay đổi trạng thái cán bộ: Kích hoạt / Ngừng kích hoạt (Tài khoản ngừng kích hoạt không chiếm quota).
    /// </summary>
    /// <param name="id">Mã GUID của đơn vị.</param>
    /// <param name="userId">Mã GUID của cán bộ.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPut("{id}/members/{userId}/toggle-status")]
    [ProducesResponseType(typeof(CustomerMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerMemberResponse>> ToggleUserStatus(
        string id,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var member = await _customerService.ToggleUserStatusAsync(id, userId, cancellationToken);
            return Ok(member);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy cán bộ",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Vượt quá định mức tài khoản",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Xóa tài khoản cán bộ khỏi đơn vị.
    /// </summary>
    /// <param name="id">Mã GUID của đơn vị.</param>
    /// <param name="userId">Mã GUID của cán bộ cần xóa.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpDelete("{id}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveUserFromCustomer(
        string id,
        string userId,
        CancellationToken cancellationToken)
    {
        var removed = await _customerService.RemoveUserFromCustomerAsync(id, userId, cancellationToken);
        if (!removed)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy cán bộ",
                Detail = $"Không tìm thấy cán bộ '{userId}' trong đơn vị '{id}'."
            });
        }

        return NoContent();
    }

    #region Gia hạn và Lịch sử Gói thuê bao (Subscription Renewal & History)

    /// <summary>
    /// API Gia hạn hoặc nâng cấp/đổi gói thuê bao cho Khách hàng / Đơn vị theo ID.
    /// Tự động cập nhật ngày hết hạn, hạn mức User (Quota), trạng thái và ghi vết lịch sử giao dịch.
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng.</param>
    /// <param name="request">Thông tin gia hạn: số tháng, ngày bắt đầu/kết thúc mới, gói thuê bao mới, số tiền, ghi chú...</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("{id}/gia-han")]
    [ProducesResponseType(typeof(LichSuGoiThueBaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LichSuGoiThueBaoResponse>> GiaHanGoiThueBaoTheoId(
        string id,
        [FromBody] GiaHanGoiThueBaoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.GiaHanGoiThueBaoAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy đối tượng",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu gia hạn không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API Gia hạn hoặc nâng cấp/đổi gói thuê bao cho chính Khách hàng / Đơn vị của user đang đăng nhập (hoặc truyền CustomerId trong body).
    /// </summary>
    /// <param name="request">Thông tin gia hạn: CustomerId (tùy chọn), số tháng, ngày bắt đầu/kết thúc mới, gói mới, số tiền...</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpPost("gia-han")]
    [ProducesResponseType(typeof(LichSuGoiThueBaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LichSuGoiThueBaoResponse>> GiaHanGoiThueBao(
        [FromBody] GiaHanGoiThueBaoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.GiaHanGoiThueBaoAsync(request.CustomerId, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy đối tượng",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu gia hạn không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API Xem thông tin trạng thái gói thuê bao hiện tại và toàn bộ lịch sử các lần đăng ký, gia hạn của một Khách hàng / Đơn vị theo Id.
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng / đơn vị.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("{id}/lich-su-thue-bao")]
    [ProducesResponseType(typeof(ThongTinGoiThueBaoCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ThongTinGoiThueBaoCustomerResponse>> LayThongTinVaLichSuGoiThueBaoTheoId(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.LayThongTinVaLichSuGoiThueBaoAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API Xem thông tin trạng thái gói thuê bao hiện tại và toàn bộ lịch sử các lần đăng ký, gia hạn của chính đơn vị user đang đăng nhập (hoặc theo CustomerId query).
    /// </summary>
    /// <param name="customerId">Mã GUID của khách hàng (tùy chọn, mặc định lấy theo tài khoản đăng nhập).</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("lich-su-thue-bao")]
    [ProducesResponseType(typeof(ThongTinGoiThueBaoCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ThongTinGoiThueBaoCustomerResponse>> LayThongTinVaLichSuGoiThueBao(
        [FromQuery] string? customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.LayThongTinVaLichSuGoiThueBaoAsync(customerId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Yêu cầu không hợp lệ",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// API Lấy danh sách lịch sử đăng ký và gia hạn gói thuê bao của Khách hàng / Đơn vị (dạng mảng danh sách).
    /// </summary>
    /// <param name="id">Mã GUID của khách hàng / đơn vị.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    [HttpGet("{id}/lich-su-thue-bao/danh-sach")]
    [ProducesResponseType(typeof(IReadOnlyList<LichSuGoiThueBaoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<LichSuGoiThueBaoResponse>>> LayDanhSachLichSuGoiThueBao(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.LayLichSuGoiThueBaoAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy khách hàng",
                Detail = ex.Message
            });
        }
    }

    #endregion
}

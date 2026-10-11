using System.ComponentModel.DataAnnotations;
using SMU.Core.Enums;

namespace SMU.Application.DTOs;

#region Request DTOs

/// <summary>
/// Yêu cầu tạo mới Khách hàng / Đơn vị (Customer).
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>
    /// Mã định danh khách hàng / đơn vị (Ví dụ: CUS-001, CAP_TAYMO).
    /// </summary>
    [Required(ErrorMessage = "Mã khách hàng không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã khách hàng không được vượt quá 50 ký tự.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên khách hàng / đơn vị (Ví dụ: Công an phường Tây Mỗ).
    /// </summary>
    [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
    [MaxLength(250, ErrorMessage = "Tên khách hàng không được vượt quá 250 ký tự.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết khách hàng / đơn vị.
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
    public string? Description { get; set; }

    /// <summary>
    /// Mã định danh Gói thuê bao áp dụng.
    /// </summary>
    public string? GoiThueBaoId { get; set; }

    /// <summary>
    /// Định mức tài khoản cán bộ tối đa được phép hoạt động (mặc định 10 hoặc theo Gói thuê bao).
    /// </summary>
    public int? GioiHanUser { get; set; }

    /// <summary>
    /// Ngày bắt đầu hợp đồng / hoạt động.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc hợp đồng / hoạt động.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Trạng thái ban đầu của khách hàng.
    /// </summary>
    public CustomerStatus Status { get; set; } = CustomerStatus.Planning;

    /// <summary>
    /// Danh sách cán bộ được khởi tạo cùng đơn vị (tùy chọn).
    /// </summary>
    public List<AddUserToCustomerRequest>? InitialMembers { get; set; }
}

/// <summary>
/// Yêu cầu cập nhật thông tin Khách hàng / Đơn vị.
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>
    /// Tên khách hàng / đơn vị.
    /// </summary>
    [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
    [MaxLength(250, ErrorMessage = "Tên khách hàng không được vượt quá 250 ký tự.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết khách hàng / đơn vị.
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
    public string? Description { get; set; }

    /// <summary>
    /// Mã định danh Gói thuê bao áp dụng.
    /// </summary>
    public string? GoiThueBaoId { get; set; }

    /// <summary>
    /// Định mức tài khoản cán bộ tối đa.
    /// </summary>
    public int? GioiHanUser { get; set; }

    /// <summary>
    /// Ngày bắt đầu hợp đồng / hoạt động.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc hợp đồng / hoạt động.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Trạng thái khách hàng.
    /// </summary>
    public CustomerStatus Status { get; set; } = CustomerStatus.Planning;

    /// <summary>
    /// Trạng thái kích hoạt.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Yêu cầu thêm mới tài khoản cán bộ vào Đơn vị / Khách hàng (tạo cả User và Account theo Customer).
/// </summary>
public class AddUserToCustomerRequest
{
    /// <summary>
    /// Tên đăng nhập tài khoản cán bộ (VD: nguyen.van.a, admin_nghiado).
    /// </summary>
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 100 ký tự.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu khởi tạo (mặc định nếu để trống: "MatKhau123@").
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Họ và tên cán bộ (VD: Bùi Văn Kiên, Nguyễn Văn A).
    /// </summary>
    [Required(ErrorMessage = "Họ và tên cán bộ không được để trống.")]
    [MaxLength(250, ErrorMessage = "Họ và tên không được vượt quá 250 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh Cấp bậc (chọn từ danh mục Cấp bậc).
    /// </summary>
    public string? CapBacId { get; set; }

    /// <summary>
    /// Số hiệu Công an nhân dân (CAND) (VD: 142-890).
    /// </summary>
    [MaxLength(50, ErrorMessage = "Số hiệu CAND không được vượt quá 50 ký tự.")]
    public string? SoHieuCAND { get; set; }

    /// <summary>
    /// Chức vụ / Vị trí công tác (VD: Cán bộ tổ CSTT, Phó Trưởng Công an phường).
    /// </summary>
    [MaxLength(200, ErrorMessage = "Chức vụ không được vượt quá 200 ký tự.")]
    public string? ChucVu { get; set; }

    /// <summary>
    /// Mã định danh Vai trò phân quyền (chọn từ danh mục Vai trò phân quyền).
    /// </summary>
    public string? VaiTroPhanQuyenId { get; set; }

    /// <summary>
    /// Email liên hệ (tùy chọn).
    /// </summary>
    [MaxLength(250, ErrorMessage = "Email không được vượt quá 250 ký tự.")]
    public string? Email { get; set; }

    /// <summary>
    /// Số điện thoại liên hệ (tùy chọn).
    /// </summary>
    [MaxLength(50, ErrorMessage = "Số điện thoại không được vượt quá 50 ký tự.")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Yêu cầu cập nhật thông tin cán bộ trong Đơn vị / Khách hàng.
/// </summary>
public class UpdateUserInCustomerRequest
{
    /// <summary>
    /// Họ và tên cán bộ.
    /// </summary>
    [Required(ErrorMessage = "Họ và tên cán bộ không được để trống.")]
    [MaxLength(250, ErrorMessage = "Họ và tên không được vượt quá 250 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh Cấp bậc.
    /// </summary>
    public string? CapBacId { get; set; }

    /// <summary>
    /// Số hiệu Công an nhân dân.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Số hiệu CAND không được vượt quá 50 ký tự.")]
    public string? SoHieuCAND { get; set; }

    /// <summary>
    /// Chức vụ / Vị trí công tác.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Chức vụ không được vượt quá 200 ký tự.")]
    public string? ChucVu { get; set; }

    /// <summary>
    /// Mã định danh Vai trò phân quyền.
    /// </summary>
    public string? VaiTroPhanQuyenId { get; set; }

    /// <summary>
    /// Email liên hệ.
    /// </summary>
    [MaxLength(250, ErrorMessage = "Email không được vượt quá 250 ký tự.")]
    public string? Email { get; set; }

    /// <summary>
    /// Số điện thoại liên hệ.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Số điện thoại không được vượt quá 50 ký tự.")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Đặt lại mật khẩu mới (nếu cần thay đổi).
    /// </summary>
    public string? NewPassword { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Thông tin định mức tài khoản cán bộ của đơn vị (Quota).
/// </summary>
public class CustomerQuotaResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int GioiHanUser { get; set; }
    public int DangHoatDong { get; set; }
    public int NgungKichHoat { get; set; }
    public int TongSo { get; set; }
    public int ConLai => Math.Max(0, GioiHanUser - DangHoatDong);
    public string QuotaDisplay => $"Đang hoạt động: {DangHoatDong} / {GioiHanUser} tài khoản (Tài khoản ngừng kích hoạt không chiếm quota)";
}

/// <summary>
/// Thông tin tài khoản cán bộ (User + Account) trong một Đơn vị / Khách hàng.
/// </summary>
public class CustomerMemberResponse
{
    public string UserId { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CapBacId { get; set; }
    public string? CapBacTen { get; set; }
    public string? SoHieuCAND { get; set; }
    public string? ChucVu { get; set; }
    public string? VaiTroPhanQuyenId { get; set; }
    public string? VaiTroPhanQuyenTen { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime Modified { get; set; }
    public string? ModifiedByName { get; set; }
}

/// <summary>
/// Thông tin chi tiết Khách hàng / Đơn vị kèm thông tin quota và danh sách cán bộ.
/// </summary>
public class CustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoiThueBaoId { get; set; }
    public string? GoiThueBaoTen { get; set; }
    public int GioiHanUser { get; set; } = 10;
    public int ActiveMemberCount { get; set; }
    public int TotalMemberCount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CustomerStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime Modified { get; set; }
    public string? ModifiedByName { get; set; }

    /// <summary>
    /// Danh sách cán bộ trực thuộc đơn vị / khách hàng.
    /// </summary>
    public IReadOnlyList<CustomerMemberResponse> Members { get; set; } = [];
}

#endregion

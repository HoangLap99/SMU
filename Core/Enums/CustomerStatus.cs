namespace SMU.Core.Enums;

/// <summary>
/// Trạng thái của Khách hàng / Đơn vị.
/// </summary>
public enum CustomerStatus
{
    /// <summary>
    /// Đang lập kế hoạch / Chuẩn bị
    /// </summary>
    Planning = 0,

    /// <summary>
    /// Đang thực hiện / Đang triển khai
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Đã hoàn thành
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Tạm dừng
    /// </summary>
    OnHold = 3,

    /// <summary>
    /// Đã hủy bỏ
    /// </summary>
    Cancelled = 4
}

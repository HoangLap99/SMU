namespace SMU.Application.DTOs;

/// <summary>
/// Yêu cầu tạo bản sao lưu dữ liệu.
/// </summary>
public class BackupRequest
{
    /// <summary>
    /// Ghi chú hoặc mô tả lý do sao lưu (ví dụ: "Sao lưu định kỳ cuối tuần", "Trước khi nâng cấp hệ thống").
    /// </summary>
    public string? GhiChu { get; set; }

    /// <summary>
    /// Chỉ định có nén thành tệp .zip hay không (nếu null sẽ lấy theo cấu hình appsettings.json).
    /// </summary>
    public bool? CompressToZip { get; set; }
}

/// <summary>
/// Kết quả sau khi thực hiện sao lưu toàn bộ dữ liệu cơ sở dữ liệu.
/// </summary>
public class BackupResultDto
{
    /// <summary>
    /// Tên tệp tin sao lưu (Ví dụ: "SMU_Backup_20261018_142500.sql" hoặc ".zip").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn tương đối của tệp sao lưu tính từ thư mục cấu hình.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn tuyệt đối của tệp sao lưu trên ổ đĩa máy chủ.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dung lượng tệp tính bằng byte.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Dung lượng tệp định dạng thân thiện (Ví dụ: "1.25 MB", "540 KB").
    /// </summary>
    public string FileSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số lượng bảng dữ liệu đã được sao lưu.
    /// </summary>
    public int TotalTables { get; set; }

    /// <summary>
    /// Tổng số lượng dòng / bản ghi dữ liệu đã được sao lưu.
    /// </summary>
    public long TotalRows { get; set; }

    /// <summary>
    /// Thời gian thực thi sao lưu (tính bằng giây).
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Thời điểm hoàn tất sao lưu (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Người thực hiện sao lưu (Username / UserId).
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Danh sách chi tiết số dòng sao lưu theo từng bảng.
    /// </summary>
    public List<BackupTableSummaryDto> TableSummaries { get; set; } = [];
}

/// <summary>
/// Tóm tắt kết quả sao lưu của từng bảng dữ liệu.
/// </summary>
public class BackupTableSummaryDto
{
    /// <summary>
    /// Tên bảng dữ liệu.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng bản ghi đã sao lưu trong bảng.
    /// </summary>
    public long RowCount { get; set; }
}

/// <summary>
/// Thông tin tệp sao lưu lưu trữ trong thư mục cấu hình.
/// </summary>
public class BackupFileInfo
{
    /// <summary>
    /// Tên tệp tin sao lưu.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn tương đối của tệp sao lưu.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Dung lượng tệp tính bằng byte.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Dung lượng tệp định dạng thân thiện (Ví dụ: "1.25 MB", "540 KB").
    /// </summary>
    public string FileSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm tạo tệp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

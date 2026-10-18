using System.ComponentModel.DataAnnotations;

namespace SMU.Infrastructure.Data.Configuration;

/// <summary>
/// Cấu hình thư mục lưu trữ và chính sách sao lưu dữ liệu đọc từ mục 'Backup' trong appsettings.json.
/// </summary>
public class BackupSettings
{
    public const string SectionName = "Backup";

    /// <summary>
    /// Thư mục lưu trữ các bản sao lưu (đường dẫn tương đối theo ContentRoot hoặc đường dẫn tuyệt đối).
    /// Mặc định: "Backups".
    /// </summary>
    [Required(ErrorMessage = "Cấu hình 'BackupFolder' không được để trống.")]
    public string BackupFolder { get; set; } = "Backups";

    /// <summary>
    /// Tự động nén tệp sao lưu thành định dạng .zip để tiết kiệm dung lượng lưu trữ máy chủ.
    /// Mặc định: false (lưu tệp .sql trực tiếp).
    /// </summary>
    public bool CompressToZip { get; set; } = false;

    /// <summary>
    /// Số lượng bản sao lưu tối đa được giữ lại trong thư mục (0: không giới hạn).
    /// Khi vượt quá số lượng này, hệ thống sẽ tự động dọn dẹp các bản sao lưu cũ nhất.
    /// Mặc định: 30 bản.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Số lượng bản backup lưu trữ tối đa phải từ 0 đến 1000.")]
    public int MaxBackupFilesToKeep { get; set; } = 30;
}

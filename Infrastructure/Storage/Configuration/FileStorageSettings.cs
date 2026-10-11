using System.ComponentModel.DataAnnotations;

namespace SMU.Infrastructure.Storage.Configuration;

/// <summary>
/// Cấu hình lưu trữ tệp tin đọc từ mục 'FileStorage' trong appsettings.json.
/// </summary>
public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Đường dẫn thư mục lưu trữ file (tương đối theo ContentRoot hoặc đường dẫn tuyệt đối).
    /// Mặc định: "Uploads".
    /// </summary>
    [Required(ErrorMessage = "Cấu hình 'StorageFolder' không được để trống.")]
    public string StorageFolder { get; set; } = "Uploads";

    /// <summary>
    /// Giữ lại phần mở rộng (.pdf, .docx, .png,...) của tệp gốc khi lưu file GUID trên ổ đĩa.
    /// Mặc định là true để các hệ thống/phần mềm có thể nhận diện được định dạng file khi mở.
    /// </summary>
    public bool PreserveExtension { get; set; } = true;
}

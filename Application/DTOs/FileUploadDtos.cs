namespace SMU.Application.DTOs;

/// <summary>
/// Kết quả trả về sau khi tải tệp tin lên máy chủ thành công.
/// </summary>
public class FileUploadResponse
{
    /// <summary>
    /// Tên gốc của tệp khi người dùng tải lên từ phía FE.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Mã GUID ngẫu nhiên gồm đúng 36 ký tự đại diện cho tệp (dạng: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
    /// </summary>
    public string FileGuid { get; set; } = string.Empty;

    /// <summary>
    /// Tên file đã lưu thực tế trên đĩa (ví dụ: {fileGuid}.ext hoặc {fileGuid}).
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Thư mục lưu trữ được chỉ định trong cấu hình appsettings.
    /// </summary>
    public string StorageFolder { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn tương đối của file trên hệ thống lưu trữ.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước của tệp tính theo byte.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Kiểu nội dung MIME của tệp (vd: image/png, application/pdf).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn API hỗ trợ tải tệp kèm query string filename và path đã mã hóa Base64.
    /// </summary>
    public string? DownloadUrl { get; set; }
}

/// <summary>
/// Kết quả phân giải thông tin tệp tin phục vụ API tải file.
/// </summary>
public class ResolvedDownloadFileResult
{
    /// <summary>
    /// Đường dẫn tệp tin vật lý tuyệt đối trên máy chủ.
    /// </summary>
    public string PhysicalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Kiểu MIME của tệp tin.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Tên tệp tin hiển thị khi tải về trình duyệt.
    /// </summary>
    public string DownloadFileName { get; set; } = string.Empty;
}

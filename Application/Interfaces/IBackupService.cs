using SMU.Application.DTOs;

namespace SMU.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ sao lưu toàn bộ dữ liệu cơ sở dữ liệu PostgreSQL vào thư mục cấu hình trong appsettings.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Thực hiện sao lưu toàn bộ dữ liệu của tất cả các bảng trong database và lưu vào thư mục được cấu hình.
    /// </summary>
    /// <param name="request">Tùy chọn yêu cầu sao lưu (ghi chú, nén zip,...).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin chi tiết kết quả sao lưu.</returns>
    Task<BackupResultDto> CreateBackupAsync(BackupRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách toàn bộ các tệp sao lưu hiện có trong thư mục cấu hình.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách thông tin các tệp sao lưu.</returns>
    Task<IReadOnlyList<BackupFileInfo>> GetBackupFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Đọc luồng dữ liệu của một tệp sao lưu để phục vụ tải về.
    /// </summary>
    /// <param name="fileName">Tên tệp tin cần tải về.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Luồng tệp (Stream), ContentType và tên tệp tin.</returns>
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadBackupFileAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một tệp sao lưu khỏi thư mục cấu hình.
    /// </summary>
    /// <param name="fileName">Tên tệp tin cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>True nếu xóa thành công, False nếu không tìm thấy tệp.</returns>
    Task<bool> DeleteBackupFileAsync(string fileName, CancellationToken cancellationToken = default);
}

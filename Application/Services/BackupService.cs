using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Infrastructure.Data;
using SMU.Infrastructure.Data.Configuration;

namespace SMU.Application.Services;

/// <summary>
/// Dịch vụ sao lưu toàn bộ dữ liệu cơ sở dữ liệu PostgreSQL vào thư mục cấu hình trong appsettings.json.
/// Xuất toàn bộ cấu trúc và dữ liệu thành tệp SQL dump chuẩn hoặc tệp nén ZIP.
/// </summary>
public class BackupService : IBackupService
{
    private readonly PostgresDbContext _dbContext;
    private readonly BackupSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogHoatDongService _logHoatDongService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        PostgresDbContext dbContext,
        IOptions<BackupSettings> options,
        IWebHostEnvironment environment,
        ICurrentUserService currentUserService,
        ILogHoatDongService logHoatDongService,
        ILogger<BackupService> logger)
    {
        _dbContext = dbContext;
        _settings = options.Value;
        _environment = environment;
        _currentUserService = currentUserService;
        _logHoatDongService = logHoatDongService;
        _logger = logger;
    }

    /// <summary>
    /// Thực hiện sao lưu toàn bộ dữ liệu của tất cả các bảng trong database và lưu vào thư mục được cấu hình.
    /// </summary>
    public async Task<BackupResultDto> CreateBackupAsync(BackupRequest? request = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var backupDir = GetBackupDirectory();
        var timestampStr = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var baseFileName = $"SMU_Backup_{timestampStr}";
        var sqlFileName = $"{baseFileName}.sql";
        var sqlFilePath = Path.Combine(backupDir, sqlFileName);

        _logger.LogInformation("Bắt đầu tiến trình sao lưu toàn bộ cơ sở dữ liệu SMU vào thư mục: {BackupDir}", backupDir);

        var tableSummaries = new List<BackupTableSummaryDto>();
        long totalRows = 0;

        // 1. Kết nối cơ sở dữ liệu
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            shouldCloseConnection = true;
        }

        try
        {
            // 2. Lấy danh sách toàn bộ các bảng vật lý trong schema public
            var tables = await GetPublicTablesAsync(connection, cancellationToken);
            _logger.LogInformation("Tìm thấy {TableCount} bảng trong cơ sở dữ liệu cần sao lưu.", tables.Count);

            // 3. Mở luồng ghi tệp SQL với bộ đệm lớn (64KB) và mã hóa UTF-8
            await using (var fileStream = new FileStream(sqlFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
            await using (var writer = new StreamWriter(fileStream, new UTF8Encoding(false), 65536))
            {
                // Ghi thông tin phần đầu tệp tin sao lưu
                await writer.WriteLineAsync($"-- ============================================================================");
                await writer.WriteLineAsync($"-- SMU SYSTEM - POSTGRESQL FULL DATABASE BACKUP");
                await writer.WriteLineAsync($"-- Ngày tạo (Local): {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                await writer.WriteLineAsync($"-- Ngày tạo (UTC)  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                await writer.WriteLineAsync($"-- Người thực hiện : {_currentUserService.Username ?? "System"} (ID: {_currentUserService.UserId ?? "N/A"})");
                await writer.WriteLineAsync($"-- Tổng số bảng    : {tables.Count}");
                if (!string.IsNullOrWhiteSpace(request?.GhiChu))
                {
                    await writer.WriteLineAsync($"-- Ghi chú         : {request.GhiChu}");
                }
                await writer.WriteLineAsync($"-- ============================================================================");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("SET statement_timeout = 0;");
                await writer.WriteLineAsync("SET lock_timeout = 0;");
                await writer.WriteLineAsync("SET client_encoding = 'UTF8';");
                await writer.WriteLineAsync("SET standard_conforming_strings = on;");
                await writer.WriteLineAsync("SET check_function_bodies = false;");
                await writer.WriteLineAsync("SET client_min_messages = warning;");
                await writer.WriteLineAsync("SET row_security = off;");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("BEGIN;");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("-- Tắt tạm thời kiểm tra foreign key và triggers để đảm bảo khôi phục dữ liệu an toàn");
                await writer.WriteLineAsync("SET session_replication_role = 'replica';");
                await writer.WriteLineAsync();

                // 4. Lần lượt sao lưu dữ liệu của từng bảng
                foreach (var tableName in tables)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowCount = await ExportTableDataAsync(connection, writer, tableName, cancellationToken);
                    totalRows += rowCount;

                    tableSummaries.Add(new BackupTableSummaryDto
                    {
                        TableName = tableName,
                        RowCount = rowCount
                    });
                }

                await writer.WriteLineAsync();
                await writer.WriteLineAsync("-- Bật lại kiểm tra foreign key và triggers sau khi hoàn tất chèn dữ liệu");
                await writer.WriteLineAsync("SET session_replication_role = 'origin';");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("COMMIT;");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"-- ============================================================================");
                await writer.WriteLineAsync($"-- SAO LƯU HOÀN TẤT: {totalRows:N0} bản ghi thuộc {tables.Count} bảng dữ liệu.");
                await writer.WriteLineAsync($"-- ============================================================================");
                await writer.FlushAsync(cancellationToken);
            }

            // 5. Kiểm tra tùy chọn nén ZIP
            var shouldCompress = request?.CompressToZip ?? _settings.CompressToZip;
            string finalFileName = sqlFileName;
            string finalFilePath = sqlFilePath;

            if (shouldCompress)
            {
                var zipFileName = $"{baseFileName}.zip";
                var zipFilePath = Path.Combine(backupDir, zipFileName);

                _logger.LogInformation("Tiến hành nén file sao lưu '{SqlFileName}' thành '{ZipFileName}'...", sqlFileName, zipFileName);

                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Create))
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(sqlFilePath, sqlFileName, CompressionLevel.Optimal);
                }

                // Xóa tệp .sql tạm thời sau khi nén thành công
                try
                {
                    File.Delete(sqlFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa file SQL tạm '{SqlFilePath}' sau khi nén ZIP.", sqlFilePath);
                }

                finalFileName = zipFileName;
                finalFilePath = zipFilePath;
            }

            // 6. Tính toán kích thước tệp và thời gian thực thi
            var fileInfo = new FileInfo(finalFilePath);
            var fileSizeBytes = fileInfo.Length;
            var fileSizeFormatted = FormatFileSize(fileSizeBytes);
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;

            var configuredFolder = string.IsNullOrWhiteSpace(_settings.BackupFolder) ? "Backups" : _settings.BackupFolder;
            var relativePath = Path.Combine(configuredFolder, finalFileName).Replace('\\', '/');

            _logger.LogInformation("Sao lưu thành công tệp '{FileName}' ({FileSize}) trong {Duration:F2} giây. Tổng {TotalRows:N0} dòng.",
                finalFileName, fileSizeFormatted, duration, totalRows);

            // 7. Tự động dọn dẹp các bản backup cũ nếu vượt quá giới hạn cấu hình
            await CleanOldBackupsAsync(backupDir, cancellationToken);

            // 8. Ghi vết vào Nhật ký hoạt động (Audit Log)
            try
            {
                await _logHoatDongService.GhiLogAsync(
                    loaiNghiepVu: "HeThong",
                    hanhDong: "Backup",
                    maChungTu: finalFileName,
                    noiDung: $"Sao lưu toàn bộ cơ sở dữ liệu hệ thống thành công: {finalFileName} ({fileSizeFormatted}, {totalRows:N0} bản ghi, {tables.Count} bảng). Ghi chú: {request?.GhiChu ?? "Sao lưu hệ thống"}",
                    chiTietThayDoi: $"Thời gian thực thi: {duration:F2}s, Dung lượng: {fileSizeFormatted}, Số bảng: {tables.Count}, Số dòng: {totalRows}",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi nhật ký hoạt động cho tác vụ sao lưu: {Message}", ex.Message);
            }

            return new BackupResultDto
            {
                FileName = finalFileName,
                FilePath = finalFilePath,
                RelativePath = relativePath,
                FileSizeBytes = fileSizeBytes,
                FileSizeFormatted = fileSizeFormatted,
                TotalTables = tables.Count,
                TotalRows = totalRows,
                DurationSeconds = Math.Round(duration, 2),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username ?? "System",
                TableSummaries = tableSummaries
            };
        }
        finally
        {
            if (shouldCloseConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các tệp sao lưu hiện có trong thư mục cấu hình.
    /// </summary>
    public Task<IReadOnlyList<BackupFileInfo>> GetBackupFilesAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = GetBackupDirectory();
        var directoryInfo = new DirectoryInfo(backupDir);

        if (!directoryInfo.Exists)
        {
            return Task.FromResult<IReadOnlyList<BackupFileInfo>>([]);
        }

        var files = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.Extension.Equals(".sql", StringComparison.OrdinalIgnoreCase) ||
                        f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f =>
            {
                var configuredFolder = string.IsNullOrWhiteSpace(_settings.BackupFolder) ? "Backups" : _settings.BackupFolder;
                var relPath = Path.Combine(configuredFolder, f.Name).Replace('\\', '/');

                return new BackupFileInfo
                {
                    FileName = f.Name,
                    RelativePath = relPath,
                    FileSizeBytes = f.Length,
                    FileSizeFormatted = FormatFileSize(f.Length),
                    CreatedAt = f.CreationTimeUtc
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupFileInfo>>(files);
    }

    /// <summary>
    /// Đọc luồng dữ liệu của một tệp sao lưu để phục vụ tải về.
    /// </summary>
    public Task<(Stream FileStream, string ContentType, string FileName)> DownloadBackupFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Tên tệp tin không được để trống.", nameof(fileName));
        }

        // Đảm bảo an toàn không bị tấn công Path Traversal
        var safeFileName = Path.GetFileName(fileName);
        var backupDir = GetBackupDirectory();
        var targetFilePath = Path.Combine(backupDir, safeFileName);

        if (!File.Exists(targetFilePath))
        {
            throw new FileNotFoundException($"Không tìm thấy tệp sao lưu: '{safeFileName}'.", safeFileName);
        }

        var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".zip" => "application/zip",
            ".sql" => "application/sql",
            _ => "application/octet-stream"
        };

        var fileStream = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<(Stream FileStream, string ContentType, string FileName)>((fileStream, contentType, safeFileName));
    }

    /// <summary>
    /// Xóa một tệp sao lưu khỏi thư mục cấu hình.
    /// </summary>
    public Task<bool> DeleteBackupFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult(false);
        }

        var safeFileName = Path.GetFileName(fileName);
        var backupDir = GetBackupDirectory();
        var targetFilePath = Path.Combine(backupDir, safeFileName);

        if (!File.Exists(targetFilePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(targetFilePath);
            _logger.LogInformation("Đã xóa tệp sao lưu: {FilePath}", targetFilePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa tệp sao lưu: {FilePath}", targetFilePath);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Lấy đường dẫn thư mục lưu trữ backup (hỗ trợ cả đường dẫn tương đối và tuyệt đối).
    /// </summary>
    private string GetBackupDirectory()
    {
        var configuredFolder = string.IsNullOrWhiteSpace(_settings.BackupFolder) ? "Backups" : _settings.BackupFolder;
        var destinationFolder = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(_environment.ContentRootPath, configuredFolder);

        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
            _logger.LogInformation("Khởi tạo thư mục sao lưu mới: {Directory}", destinationFolder);
        }

        return destinationFolder;
    }

    /// <summary>
    /// Lấy danh sách các bảng dữ liệu trong schema public.
    /// </summary>
    private static async Task<List<string>> GetPublicTablesAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var tables = new List<string>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
              AND table_type = 'BASE TABLE'
            ORDER BY table_name;";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <summary>
    /// Đọc dữ liệu từ bảng và ghi ra tệp tin SQL dưới dạng các lệnh INSERT hàng loạt.
    /// </summary>
    private static async Task<long> ExportTableDataAsync(
        DbConnection connection,
        StreamWriter writer,
        string tableName,
        CancellationToken cancellationToken)
    {
        // 1. Lấy danh sách các cột của bảng
        var columns = new List<string>();
        await using (var cmdCol = connection.CreateCommand())
        {
            cmdCol.CommandText = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_schema = 'public' 
                  AND table_name = @tableName 
                ORDER BY ordinal_position;";

            var param = cmdCol.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = tableName;
            cmdCol.Parameters.Add(param);

            await using var readerCol = await cmdCol.ExecuteReaderAsync(cancellationToken);
            while (await readerCol.ReadAsync(cancellationToken))
            {
                columns.Add(readerCol.GetString(0));
            }
        }

        if (columns.Count == 0)
        {
            return 0;
        }

        // Định dạng danh sách cột trong SQL: "Col1", "Col2", ...
        var quotedColumns = string.Join(", ", columns.Select(c => $"\"{c}\""));

        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"-- ----------------------------------------------------------------------------");
        await writer.WriteLineAsync($"-- Dữ liệu bảng: \"public\".\"{tableName}\"");
        await writer.WriteLineAsync($"-- ----------------------------------------------------------------------------");

        // 2. Đọc toàn bộ dữ liệu từ bảng qua DataReader và ghi theo lô (100 dòng mỗi lệnh INSERT)
        long rowCount = 0;
        const int batchSize = 100;
        var batchValues = new List<string>(batchSize);

        await using (var cmdData = connection.CreateCommand())
        {
            cmdData.CommandText = $"SELECT * FROM \"public\".\"{tableName}\";";

            await using var readerData = await cmdData.ExecuteReaderAsync(cancellationToken);
            while (await readerData.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var values = new string[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    if (readerData.IsDBNull(i))
                    {
                        values[i] = "NULL";
                    }
                    else
                    {
                        var val = readerData.GetValue(i);
                        values[i] = FormatSqlLiteral(val);
                    }
                }

                batchValues.Add($"({string.Join(", ", values)})");
                rowCount++;

                if (batchValues.Count >= batchSize)
                {
                    await WriteBatchInsertAsync(writer, tableName, quotedColumns, batchValues);
                    batchValues.Clear();
                }
            }
        }

        if (batchValues.Count > 0)
        {
            await WriteBatchInsertAsync(writer, tableName, quotedColumns, batchValues);
            batchValues.Clear();
        }

        if (rowCount == 0)
        {
            await writer.WriteLineAsync($"-- Bảng \"public\".\"{tableName}\": Không có dữ liệu.");
        }
        else
        {
            await writer.WriteLineAsync($"-- Đã sao lưu {rowCount:N0} bản ghi của bảng \"public\".\"{tableName}\".");
        }

        return rowCount;
    }

    /// <summary>
    /// Ghi 1 khối INSERT hàng loạt (multi-row VALUES) vào tệp SQL.
    /// </summary>
    private static async Task WriteBatchInsertAsync(
        StreamWriter writer,
        string tableName,
        string quotedColumns,
        List<string> batchValues)
    {
        await writer.WriteLineAsync($"INSERT INTO \"public\".\"{tableName}\" ({quotedColumns}) VALUES");
        for (int i = 0; i < batchValues.Count; i++)
        {
            var isLast = (i == batchValues.Count - 1);
            await writer.WriteLineAsync($"  {batchValues[i]}{(isLast ? ";" : ",")}");
        }
    }

    /// <summary>
    /// Định dạng giá trị C# thành chuỗi SQL Literal tương thích chuẩn PostgreSQL.
    /// </summary>
    private static string FormatSqlLiteral(object val)
    {
        if (val == null || val is DBNull)
        {
            return "NULL";
        }

        return val switch
        {
            bool b => b ? "TRUE" : "FALSE",
            short s => s.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            byte by => by.ToString(CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.ffffff}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss.ffffffzzz}'",
            DateOnly dOnly => $"'{dOnly:yyyy-MM-dd}'",
            TimeOnly tOnly => $"'{tOnly:HH:mm:ss.ffffff}'",
            Guid g => $"'{g}'",
            byte[] bytes => $"E'\\\\x{Convert.ToHexString(bytes)}'",
            string str => $"'{str.Replace("'", "''")}'",
            _ => $"'{val.ToString()?.Replace("'", "''") ?? string.Empty}'"
        };
    }

    /// <summary>
    /// Định dạng kích thước dung lượng file (Bytes -> KB / MB / GB).
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }
        if (bytes < 1024 * 1024)
        {
            return $"{(bytes / 1024.0):F2} KB";
        }
        if (bytes < 1024 * 1024 * 1024)
        {
            return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
        }
        return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
    }

    /// <summary>
    /// Tự động dọn dẹp các tệp sao lưu cũ nhất khi vượt quá số lượng tối đa cấu hình.
    /// </summary>
    private async Task CleanOldBackupsAsync(string backupDir, CancellationToken cancellationToken)
    {
        if (_settings.MaxBackupFilesToKeep <= 0)
        {
            return;
        }

        try
        {
            var directoryInfo = new DirectoryInfo(backupDir);
            if (!directoryInfo.Exists)
            {
                return;
            }

            var backupFiles = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.Extension.Equals(".sql", StringComparison.OrdinalIgnoreCase) ||
                            f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (backupFiles.Count > _settings.MaxBackupFilesToKeep)
            {
                var filesToDelete = backupFiles.Skip(_settings.MaxBackupFilesToKeep).ToList();
                _logger.LogInformation("Phát hiện {Count} tệp sao lưu cũ vượt quá giới hạn lưu trữ ({Limit}). Bắt đầu dọn dẹp...",
                    filesToDelete.Count, _settings.MaxBackupFilesToKeep);

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        _logger.LogInformation("Đã dọn dẹp tệp sao lưu cũ: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể dọn dẹp tệp sao lưu cũ '{FileName}': {Message}", file.Name, ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi xảy ra trong quá trình dọn dẹp các tệp sao lưu cũ: {Message}", ex.Message);
        }

        await Task.CompletedTask;
    }

    #endregion
}

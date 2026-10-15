using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Core.Exceptions;

namespace SMU.Presentation.Middlewares;

/// <summary>
/// Trình xử lý ngoại lệ toàn cục sử dụng giao diện chuẩn IExceptionHandler của .NET 8.
/// Tự động bắt tất cả các Exception và trả về ProblemDetails theo chuẩn RFC 7807.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Đã phát sinh ngoại lệ: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            AppException appEx => (StatusCodes.Status500InternalServerError, "Lỗi xử lý nghiệp vụ", appEx.Message),
            ArgumentException argEx => (StatusCodes.Status500InternalServerError, "Lỗi trong quá trình xử lý", argEx.Message),
            InvalidOperationException invEx => (StatusCodes.Status500InternalServerError, "Lỗi trong quá trình xử lý", invEx.Message),
            KeyNotFoundException keyEx => (StatusCodes.Status404NotFound, "Không tìm thấy dữ liệu", keyEx.Message),
            UnauthorizedAccessException unAuthEx => (StatusCodes.Status403Forbidden, "Không có quyền truy cập", unAuthEx.Message),
            DbUpdateException dbEx => (StatusCodes.Status500InternalServerError, "Lỗi cơ sở dữ liệu", _environment.IsDevelopment() ? (dbEx.InnerException?.Message ?? dbEx.Message) : "Đã xảy ra lỗi khi lưu dữ liệu vào hệ thống. Vui lòng thử lại sau."),
            OperationCanceledException => (499, "Yêu cầu bị hủy bỏ", "Thao tác đã bị hủy hoặc vượt quá thời gian chờ (timeout)."),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi trong quá trình xử lý", _environment.IsDevelopment() ? (exception.InnerException?.Message ?? exception.Message) : (!string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "Đã xảy ra sự cố không mong muốn trên hệ thống. Vui lòng thử lại sau."))
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

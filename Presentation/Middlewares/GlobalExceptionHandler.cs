using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            AppException appEx => ((int)appEx.StatusCode, "Lỗi xử lý nghiệp vụ", appEx.Message),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, "Dữ liệu đầu vào không hợp lệ", argEx.Message),
            InvalidOperationException invEx => (StatusCodes.Status400BadRequest, "Lỗi cấu hình hoặc trạng thái", invEx.Message),
            OperationCanceledException => (499, "Yêu cầu bị hủy bỏ", "Thao tác đã bị hủy hoặc vượt quá thời gian chờ (timeout)."),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi máy chủ nội bộ", _environment.IsDevelopment() ? exception.Message : "Đã xảy ra sự cố không mong muốn trên hệ thống. Vui lòng thử lại sau.")
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

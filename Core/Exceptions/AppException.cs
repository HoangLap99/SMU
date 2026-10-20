using System.Net;

namespace SMU.Core.Exceptions;

/// <summary>
/// Lớp ngoại lệ cơ sở cho toàn bộ ứng dụng, hỗ trợ chỉ định mã trạng thái HTTP.
/// </summary>
public abstract class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }

    protected AppException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) 
        : base(message)
    {
        StatusCode = statusCode;
    }

    protected AppException(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

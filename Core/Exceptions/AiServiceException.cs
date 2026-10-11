using System.Net;

namespace SMU.Core.Exceptions;

/// <summary>
/// Ngoại lệ phát sinh khi xảy ra lỗi trong quá trình giao tiếp hoặc phân tích từ AI Provider.
/// </summary>
public class AiServiceException : AppException
{
    public AiServiceException(string message, HttpStatusCode statusCode = HttpStatusCode.BadGateway) 
        : base(message, statusCode)
    {
    }

    public AiServiceException(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.BadGateway) 
        : base(message, innerException, statusCode)
    {
    }
}

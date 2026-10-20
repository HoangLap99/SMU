using System.Net;

namespace SMU.Core.Exceptions;

/// <summary>
/// Ngoại lệ phát sinh khi xảy ra sự cố trong quá trình đọc hoặc bóc tách nội dung tài liệu.
/// </summary>
public class DocumentExtractionException : AppException
{
    public DocumentExtractionException(string message, Exception? innerException = null) 
        : base(message, innerException!, HttpStatusCode.InternalServerError)
    {
    }
}

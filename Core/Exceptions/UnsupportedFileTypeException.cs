using System.Net;

namespace SMU.Core.Exceptions;

/// <summary>
/// Ngoại lệ phát sinh khi định dạng tệp không nằm trong danh sách hỗ trợ.
/// </summary>
public class UnsupportedFileTypeException : AppException
{
    public string Extension { get; }

    public UnsupportedFileTypeException(string extension, IEnumerable<string> supportedExtensions)
        : base($"Định dạng file '{extension}' không được hỗ trợ. Các định dạng hỗ trợ: {string.Join(", ", supportedExtensions)}", HttpStatusCode.BadRequest)
    {
        Extension = extension;
    }
}

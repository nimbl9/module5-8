namespace module1_4.Common;

public class ApiResponsePacket<T>
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public T? Result { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponsePacket<T> Success(T result, int statusCode = 200, string? traceId = null)
    {
        return new ApiResponsePacket<T>
        {
            StatusCode = statusCode,
            IsSuccess = true,
            Result = result,
            ErrorCode = null,
            ErrorMessage = null,
            TraceId = traceId
        };
    }

    public static ApiResponsePacket<T> Failure(int statusCode, string errorCode, string message, string? traceId = null)
    {
        return new ApiResponsePacket<T>
        {
            StatusCode = statusCode,
            IsSuccess = false,
            Result = default,
            ErrorCode = errorCode,
            ErrorMessage = message,
            TraceId = traceId
        };
    }
}
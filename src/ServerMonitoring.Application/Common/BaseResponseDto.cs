namespace ServerMonitoring.Application.Common;

public sealed class BaseResponseDto<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    public static BaseResponseDto<T> Ok(T data, string message = "عملیات موفق بود")
        => new()
        {
            Success = true,
            Message = message,
            Data = data
        };
}

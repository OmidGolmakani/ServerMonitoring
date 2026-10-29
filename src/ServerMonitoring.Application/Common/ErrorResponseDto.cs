namespace ServerMonitoring.Application.Common;

public sealed class ErrorResponseDto
{
    public bool Success { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int StatusCode { get; init; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}

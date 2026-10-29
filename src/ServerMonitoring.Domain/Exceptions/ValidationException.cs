namespace ServerMonitoring.Domain.Exceptions;

public sealed class ValidationException : AppException
{
    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message, 400, "اعتبارسنجی")
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

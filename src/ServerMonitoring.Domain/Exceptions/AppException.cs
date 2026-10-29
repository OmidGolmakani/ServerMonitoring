namespace ServerMonitoring.Domain.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode, string title)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }

    public string Title { get; }
}

namespace ServerMonitoring.Domain.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404, "یافت نشد")
    {
    }
}

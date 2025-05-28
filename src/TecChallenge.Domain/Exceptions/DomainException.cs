namespace TecChallenge.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public static void ThrowIfNull(object? argument, string message)
    {
        if (argument is null)
        {
            throw new DomainException(message);
        }
    }
}
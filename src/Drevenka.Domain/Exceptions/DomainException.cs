namespace Drevenka.Domain.Exceptions;

/// <summary>
/// Výjimka indikující porušení doménového pravidla nebo neplatný stav entity.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

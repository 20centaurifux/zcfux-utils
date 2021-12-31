namespace zcfux.Logging;

public sealed class FactoryException : Exception
{
    public FactoryException(string message) : base(message)
    {
    }

    public FactoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
namespace zcfux.Logging.Console;

[Logger("console")]
internal sealed class Writer : IWriter
{
    public void WriteMessage(ESeverity severity, string message)
        => System.Console.WriteLine($"{DateTime.Now} {severity} {message}");

    public void WriteException(ESeverity severity, Exception exception)
        => WriteMessage(severity, exception?.ToString()!);
}
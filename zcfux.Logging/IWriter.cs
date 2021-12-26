namespace zcfux.Logging;

public interface IWriter
{
    void WriteMessage(ESeverity severity, string message);

    void WriteException(ESeverity severity, Exception exception);
}
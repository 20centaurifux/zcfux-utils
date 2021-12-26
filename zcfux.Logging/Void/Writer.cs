namespace zcfux.Logging.Void;

[Logger("void")]
public class Writer : IWriter
{
    public void WriteMessage(ESeverity severity, string message)
    {
    }

    public void WriteException(ESeverity severity, Exception exception)
    {
    }
}
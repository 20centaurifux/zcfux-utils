namespace zcfux.Logging.Test.Writer;

[Logger("collect")]
public sealed class Collect : IWriter
{
    public static readonly IDictionary<ESeverity, IList<string>> Messages = new Dictionary<ESeverity, IList<string>>();

    public void WriteMessage(ESeverity severity, string message)
        => Write(severity, message);

    public void WriteException(ESeverity severity, Exception exception)
        => Write(severity, exception.ToString());

    static void Write(ESeverity severity, string message)
    {
        if (!Messages.TryGetValue(severity, out var messages))
        {
            messages = new List<string>();

            Messages[severity] = messages;
        }

        messages.Add(message);
    }
}
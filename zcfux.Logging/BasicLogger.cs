namespace zcfux.Logging;

internal sealed class BasicLogger : ILogger
{
    readonly IWriter _writer;

    public BasicLogger(IWriter writer)
        => _writer = writer;

    public void Trace(string message)
        => Log(ESeverity.Trace, message);

    public void Trace(string format, params object[] args)
        => Log(ESeverity.Trace, format, args);

    public void Trace(Exception ex)
        => Log(ESeverity.Trace, ex);

    public void Debug(string message)
        => Log(ESeverity.Debug, message);

    public void Debug(string format, params object[] args)
        => Log(ESeverity.Debug, format, args);

    public void Debug(Exception ex)
        => Log(ESeverity.Debug, ex);

    public void Info(string message)
        => Log(ESeverity.Info, message);

    public void Info(string format, params object[] args)
        => Log(ESeverity.Info, format, args);

    public void Info(Exception ex)
        => Log(ESeverity.Info, ex);

    public void Warn(string message)
        => Log(ESeverity.Warn, message);

    public void Warn(string format, params object[] args)
        => Log(ESeverity.Warn, format, args);

    public void Warn(Exception ex)
        => Log(ESeverity.Warn, ex);

    public void Error(string message)
        => Log(ESeverity.Error, message);

    public void Error(string format, params object[] args)
        => Log(ESeverity.Error, format, args);

    public void Error(Exception ex)
        => Log(ESeverity.Error, ex);

    public void Fatal(string message)
        => Log(ESeverity.Fatal, message);

    public void Fatal(string format, params object[] args)
        => Log(ESeverity.Fatal, format, args);

    public void Fatal(Exception ex)
        => Log(ESeverity.Fatal, ex);

    void Log(ESeverity severity, string message)
    {
        Swallow(() =>
        {
            _writer.WriteMessage(severity, message);
        });
    }

    void Log(ESeverity severity, string format, params object[] args)
    {
        Swallow(() =>
        {
            var msg = string.Format(format, args);

            _writer.WriteMessage(severity, msg);
        });
    }

    void Log(ESeverity severity, Exception exception)
    {
        Swallow(() =>
        {
            _writer.WriteException(severity, exception);
        });
    }

    void Swallow(Action fn)
    {
        try
        {
            fn();
        }
        catch { }
    }
}
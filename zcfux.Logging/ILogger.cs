namespace zcfux.Logging;

public interface ILogger
{
    void Trace(string message);

    void Trace(string format, params object[] args);

    void Trace(Exception ex);

    void Debug(string message);

    void Debug(string format, params object[] args);

    void Debug(Exception ex);

    void Info(string message);

    void Info(string format, params object[] args);

    void Info(Exception ex);

    void Warn(string message);

    void Warn(string format, params object[] args);

    void Warn(Exception ex);

    void Error(string message);

    void Error(string format, params object[] args);

    void Error(Exception ex);

    void Fatal(string message);

    void Fatal(string format, params object[] args);

    void Fatal(Exception ex);
}
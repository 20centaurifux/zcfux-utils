using NLog;

namespace zcfux.Logging.NLog;

internal static class Mapper
{
    public static LogLevel Map(ESeverity severity)
    {
        LogLevel level;

        switch (severity)
        {
            case ESeverity.Trace:
                level = LogLevel.Trace;
                break;

            case ESeverity.Debug:
                level = LogLevel.Debug;
                break;

            case ESeverity.Info:
                level = LogLevel.Info;
                break;

            case ESeverity.Warn:
                level = LogLevel.Warn;
                break;

            case ESeverity.Error:
                level = LogLevel.Error;
                break;

            case ESeverity.Fatal:
                level = LogLevel.Fatal;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
        }

        return level;
    }
}
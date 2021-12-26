using System.Diagnostics;
using System.Reflection;

namespace zcfux.Logging.NLog;

[Logger("nlog")]
public sealed class Writer : IWriter
{
    readonly global::NLog.ILogger _logger;

    public Writer()
    {
        var callingAssembly = GetCallingAssembly();

        _logger = global::NLog.LogManager.GetLogger(callingAssembly.GetName().Name);
    }

    static Assembly GetCallingAssembly()
    {
        var method = new StackFrame(skipFrames: 4, needFileInfo: false).GetMethod();

        return method!.DeclaringType!.Assembly;
    }

    public void WriteMessage(ESeverity severity, string message)
        => _logger.Log(Mapper.Map(severity), message);

    public void WriteException(ESeverity severity, Exception exception)
        => _logger.Log(Mapper.Map(severity), exception);
}
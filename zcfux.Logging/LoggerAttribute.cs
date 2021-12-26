namespace zcfux.Logging;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LoggerAttribute : Attribute
{
    public LoggerAttribute(string name)
        => Name = name;

    public string Name { get; }
}
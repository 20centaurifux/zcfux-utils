namespace zcfux.Logging;

public sealed class Factory
{
    static readonly Lazy<IDictionary<string, Type>> Types = new(TypeLoader.GetTypes());

    public static ILogger ByName(string name)
    {
        var writer = CreateWriter(name);

        return new BasicLogger(writer);
    }

    static IWriter CreateWriter(string name)
    {
        IWriter? writer = null;

        if (Types.Value.TryGetValue(name, out var t))
        {
            writer = Activator.CreateInstance(t) as IWriter;
        }

        return writer ?? throw new FactoryException("Writer not found.");
    }
}
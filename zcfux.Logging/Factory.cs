namespace zcfux.Logging;

public static class Factory
{
    static readonly Lazy<IDictionary<string, Type>> Types = new(TypeLoader.GetTypes());

    public static ILogger ByName(string name)
    {
        var writer = CreateWriter(name);

        return new BasicLogger(writer);
    }

    public static ILogger FromAssembly(string assemblyName, string typeName)
    {
        try
        {
            var writer = Activator.CreateInstance(assemblyName, typeName)!.Unwrap();

            return new BasicLogger((writer as IWriter)!);
        }
        catch (Exception ex)
        {
            throw new FactoryException("Couldn't create writer.", ex);
        }
    }

    static IWriter CreateWriter(string name)
    {
        IWriter? writer = null;

        if (Types.Value.TryGetValue(name, out var t))
        {
            writer = Activator.CreateInstance(t) as IWriter;
        }

        return writer ?? throw new FactoryException($"Writer `{name}' not found.");
    }
}
namespace zcfux.SqlMapper;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute : Attribute
{
    public ColumnAttribute(string columnName)
        => ColumnName = columnName;

    public string ColumnName { get; }
}
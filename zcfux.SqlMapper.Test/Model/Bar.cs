namespace zcfux.SqlMapper.Test.Model;

[Model]
public sealed class Bar
{
    [Column("ID")]
    public long Identifier { get; set; }

    [Column("A")]
    public string First { get; set; }

    [Column("B")]
    public string Second { get; set; }
}
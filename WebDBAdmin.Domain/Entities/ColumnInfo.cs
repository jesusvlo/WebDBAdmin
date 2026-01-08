namespace UniversalDBStudio.Domain.Entities;

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; } = false;
    public bool IsIdentity { get; set; } = false;
    public int? Length { get; set; }
}

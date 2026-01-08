namespace WebDBAdmin.Application.DTOs;

public class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; } = false;
    // Length/Precision could be added here later depending on the type
    public int? Length { get; set; }
}

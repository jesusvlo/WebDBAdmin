using System.Collections.Generic;

namespace WebDBAdmin.Application.DTOs;

public class TableDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
}

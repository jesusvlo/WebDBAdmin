using WebDBAdmin.Domain.Entities;

using WebDBAdmin.Application.DTOs;

namespace WebDBAdmin.Application.Interfaces;

public interface ISchemaService
{
    Task<List<string>> GetDatabasesAsync(ConnectionInfo connectionInfo);
    Task<List<string>> GetTablesAsync(ConnectionInfo connectionInfo);
    Task<List<ColumnDefinition>> GetColumnsAsync(ConnectionInfo connectionInfo, string tableName);
}

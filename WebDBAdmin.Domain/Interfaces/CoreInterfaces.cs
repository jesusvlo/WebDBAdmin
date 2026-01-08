using System.Data;
using UniversalDBStudio.Domain.Entities;

namespace UniversalDBStudio.Domain.Interfaces;

public interface IConnectionFactory
{
    IDbConnection CreateConnection(ConnectionInfo connectionInfo);
    string GetConnectionString(ConnectionInfo connectionInfo);
}

public interface IDatabaseMetadataService
{
    Task<List<string>> GetTablesAsync(ConnectionInfo connectionInfo);
    Task<List<string>> GetDatabasesAsync(ConnectionInfo connectionInfo);
    Task<List<ColumnInfo>> GetColumnsAsync(ConnectionInfo connectionInfo, string tableName);
}

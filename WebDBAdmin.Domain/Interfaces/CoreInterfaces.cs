using System.Data;
using WebDBAdmin.Domain.Entities;

namespace WebDBAdmin.Domain.Interfaces;

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

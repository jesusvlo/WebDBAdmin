using DatabaseSchemaReader;
using WebDBAdmin.Domain.Entities;
using WebDBAdmin.Domain.Interfaces;
using System.Data.Common;
using DatabaseSchemaReader.DataSchema;

namespace WebDBAdmin.Infrastructure.Services;

public class DatabaseMetadataService : IDatabaseMetadataService
{
    private readonly IConnectionFactory _connectionFactory;

    public DatabaseMetadataService(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<string>> GetDatabasesAsync(ConnectionInfo connectionInfo)
    {
        return await Task.Run(async () =>
        {
            try
            {
                using var connection = (DbConnection)_connectionFactory.CreateConnection(connectionInfo);
                await connection.OpenAsync();

                var databases = new List<string>();
                System.Data.DataTable table;

                switch (connectionInfo.Engine)
                {
                    case Domain.Enums.DatabaseEngine.SqlServer:
                        table = connection.GetSchema("Databases");
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            databases.Add(row["database_name"].ToString()!);
                        }
                        break;
                    case Domain.Enums.DatabaseEngine.MySql:
                        table = connection.GetSchema("Databases");
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            databases.Add(row["DATABASE_NAME"].ToString()!);
                        }
                        break;
                    case Domain.Enums.DatabaseEngine.PostgreSql:
                        table = connection.GetSchema("Databases");
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            databases.Add(row["database_name"].ToString()!);
                        }
                        break;
                    default:
                        return new List<string>();
                }

                return databases.OrderBy(d => d).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting databases via schema: {ex.Message}");
                return new List<string>();
            }
        });
    }

    public async Task<List<string>> GetTablesAsync(ConnectionInfo connectionInfo)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var connection = (DbConnection)_connectionFactory.CreateConnection(connectionInfo);
                var reader = new DatabaseReader(connection);

                // Use AllTables to get schema info for filtering
                var tables = reader.AllTables();

                // Filter out system tables/schemas based on engine
                var filtered = tables.Where(t =>
                {
                    var schema = t.SchemaOwner?.ToLowerInvariant();
                    if (schema == null) return true;

                    // MySQL specific: SchemaOwner is the database name. 
                    // We must filter to ensure we only return tables belonging to the requested database.
                    if (connectionInfo.Engine == Domain.Enums.DatabaseEngine.MySql)
                    {
                        if (!string.Equals(t.SchemaOwner, connectionInfo.Database, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    // Common system schemas to exclude
                    if (schema == "sys" ||
                        schema == "information_schema" ||
                        schema == "mysql" ||
                        schema == "performance_schema" ||
                        schema.StartsWith("pg_"))
                    {
                        return false;
                    }

                    return true;
                });

                return filtered.Select(t => t.Name).OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tables: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(ConnectionInfo connectionInfo, string tableName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var connection = (DbConnection)_connectionFactory.CreateConnection(connectionInfo);
                var reader = new DatabaseReader(connection);

                // Optimized: User AllTables but filter? Or use table name to load exact?
                // DSR doesn't have a direct "GetTable(name)" that is efficient without loading schemas?
                // Actually AllTables() loads everything. For a single table it might be heavy if DB is huge.
                // But keeping coherence with previous implementation:
                var table = reader.AllTables().FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));

                if (table != null)
                {
                    return table.Columns.Select(c => new ColumnInfo
                    {
                        Name = c.Name,
                        Type = c.NetDataType(),
                        IsNullable = c.Nullable,
                        IsPrimaryKey = c.IsPrimaryKey,
                        IsIdentity = c.IsAutoNumber,
                        Length = c.Length
                    }).ToList();
                }

                return new List<ColumnInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting columns: {ex.Message}");
                throw;
            }
        });
    }
}

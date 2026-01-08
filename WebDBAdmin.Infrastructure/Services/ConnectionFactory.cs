using System.Data;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using WebDBAdmin.Domain.Entities;
using WebDBAdmin.Domain.Enums;
using WebDBAdmin.Domain.Interfaces;

namespace WebDBAdmin.Infrastructure.Services;

public class ConnectionFactory : IConnectionFactory
{
    public IDbConnection CreateConnection(ConnectionInfo connectionInfo)
    {
        string connectionString = GetConnectionString(connectionInfo);

        return connectionInfo.Engine switch
        {
            DatabaseEngine.SqlServer => new SqlConnection(connectionString),
            DatabaseEngine.MySql => new MySqlConnection(connectionString),
            DatabaseEngine.PostgreSql => new NpgsqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database engine {connectionInfo.Engine} is not supported.")
        };
    }

    public string GetConnectionString(ConnectionInfo connectionInfo)
    {
        switch (connectionInfo.Engine)
        {
            case DatabaseEngine.SqlServer:
                var sqlPort = connectionInfo.Port > 0 ? $",{connectionInfo.Port}" : "";
                return $"Server={connectionInfo.Server}{sqlPort};Database={connectionInfo.Database};User Id={connectionInfo.Username};Password={connectionInfo.Password};TrustServerCertificate=True;";
            case DatabaseEngine.MySql:
                var mySqlPort = connectionInfo.Port > 0 ? connectionInfo.Port : 3306;
                return $"Server={connectionInfo.Server};Port={mySqlPort};Database={connectionInfo.Database};Uid={connectionInfo.Username};Pwd={connectionInfo.Password};";
            case DatabaseEngine.PostgreSql:
                var pgPort = connectionInfo.Port > 0 ? connectionInfo.Port : 5432;
                return $"Host={connectionInfo.Server};Port={pgPort};Database={connectionInfo.Database};Username={connectionInfo.Username};Password={connectionInfo.Password};";
            default:
                throw new NotSupportedException($"Database engine {connectionInfo.Engine} is not supported.");
        }
    }
}

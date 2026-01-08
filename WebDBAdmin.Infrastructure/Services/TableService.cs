using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Runner.Processors.Postgres;
using FluentMigrator.Runner.Generators.MySql;
using FluentMigrator.Runner.Generators.Postgres;
using FluentMigrator.Runner.Generators.SqlServer;
using System.Data;
using WebDBAdmin.Application.DTOs;
using WebDBAdmin.Application.Interfaces;
using WebDBAdmin.Domain.Entities;
using WebDBAdmin.Domain.Enums;
using WebDBAdmin.Domain.Interfaces;
using ColumnDefinition = WebDBAdmin.Application.DTOs.ColumnDefinition;
using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;
using WebDBAdmin.Infrastructure.Migrations;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner.Conventions;

namespace WebDBAdmin.Infrastructure.Services;

public class TableService : ITableService
{
    private readonly IConnectionFactory _connectionFactory;

    private static readonly Dictionary<Type, DbType> TypeToDbTypeMap = new()
    {
        { typeof(byte), DbType.Byte },
        { typeof(sbyte), DbType.SByte },
        { typeof(short), DbType.Int16 },
        { typeof(ushort), DbType.UInt16 },
        { typeof(int), DbType.Int32 },
        { typeof(uint), DbType.UInt32 },
        { typeof(long), DbType.Int64 },
        { typeof(ulong), DbType.UInt64 },
        { typeof(float), DbType.Single },
        { typeof(double), DbType.Double },
        { typeof(decimal), DbType.Decimal },
        { typeof(bool), DbType.Boolean },
        { typeof(string), DbType.String },
        { typeof(char), DbType.StringFixedLength },
        { typeof(Guid), DbType.Guid },
        { typeof(DateTime), DbType.DateTime },
        { typeof(DateTimeOffset), DbType.DateTimeOffset },
        { typeof(DateOnly), DbType.Date },
        { typeof(TimeOnly), DbType.Time },
        { typeof(TimeSpan), DbType.Time },
        { typeof(byte[]), DbType.Binary },
        { typeof(object), DbType.Object },
    };

    public TableService(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static DbType GetDbType(Type type)
    {
        // Manejar tipos nullable
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (TypeToDbTypeMap.TryGetValue(underlyingType, out var dbType))
        {
            return dbType;
        }

        // Manejar enums como su tipo subyacente
        if (underlyingType.IsEnum)
        {
            return GetDbType(Enum.GetUnderlyingType(underlyingType));
        }

        // Valor por defecto para tipos no mapeados
        return DbType.Object;
    }

    public async Task CreateTableAsync(ConnectionInfo connectionInfo, TableDefinition tableDefinition)
    {
        await ExecuteMigration(connectionInfo, m =>
        {
            var table = m.Create.Table(tableDefinition.Name);
            foreach (var col in tableDefinition.Columns)
            {
                m.DefineColumn(table.WithColumn(col.Name), col);
            }
        });
    }

    public async Task AddColumnAsync(ConnectionInfo connectionInfo, string tableName, ColumnDefinition column)
    {
        await ExecuteMigration(connectionInfo, m =>
        {
            m.DefineColumn(m.Alter.Table(tableName).AddColumn(column.Name), column);
        });
    }

    public async Task DropTableAsync(ConnectionInfo connectionInfo, string tableName)
    {
        await ExecuteMigration(connectionInfo, m => m.Delete.Table(tableName));
    }

    public async Task DropColumnAsync(ConnectionInfo connectionInfo, string tableName, string columnName)
    {
        await ExecuteMigration(connectionInfo, m => m.Delete.Column(columnName).FromTable(tableName));
    }

    public async Task RenameTableAsync(ConnectionInfo connectionInfo, string oldName, string newName)
    {
        await ExecuteMigration(connectionInfo, m => m.Rename.Table(oldName).To(newName));
    }

    public async Task AlterColumnAsync(ConnectionInfo connectionInfo, string tableName, ColumnDefinition column)
    {
        await ExecuteMigration(connectionInfo, m =>
        {
            m.DefineColumn(m.Alter.Table(tableName).AlterColumn(column.Name), column);
        });
    }

    private async Task ExecuteMigration(ConnectionInfo connectionInfo, Action<AdHocMigration> action)
    {
        var serviceProvider = CreateServices(connectionInfo);
        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var migration = new AdHocMigration(action);

        // Execute migration using dynamic dispatch to access Up(IMigration)
        ((dynamic)runner).Up(migration);

        await Task.CompletedTask;
    }

    private IServiceProvider CreateServices(ConnectionInfo info)
    {
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                rb.WithGlobalConnectionString(_connectionFactory.GetConnectionString(info));
                switch (info.Engine)
                {
                    case DatabaseEngine.SqlServer: rb.AddSqlServer(); break;
                    case DatabaseEngine.MySql: rb.AddMySql5(); break;
                    case DatabaseEngine.PostgreSql: rb.AddPostgres(); break;
                    default: throw new NotImplementedException("Engine not supported");
                }
            })
            .BuildServiceProvider();
    }
}

using System.Threading.Tasks;
using WebDBAdmin.Application.DTOs;
using WebDBAdmin.Domain.Entities;

namespace WebDBAdmin.Application.Interfaces;

public interface ITableService
{
    Task CreateTableAsync(ConnectionInfo connectionInfo, TableDefinition tableDefinition);
    Task DropTableAsync(ConnectionInfo connectionInfo, string tableName);
    Task AddColumnAsync(ConnectionInfo connectionInfo, string tableName, ColumnDefinition column);
    Task DropColumnAsync(ConnectionInfo connectionInfo, string tableName, string columnName);
    Task RenameTableAsync(ConnectionInfo connectionInfo, string oldName, string newName);
    Task AlterColumnAsync(ConnectionInfo connectionInfo, string tableName, ColumnDefinition column);
}

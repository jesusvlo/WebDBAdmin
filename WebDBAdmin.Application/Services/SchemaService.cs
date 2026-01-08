using WebDBAdmin.Application.Interfaces;
using WebDBAdmin.Application.DTOs;
using WebDBAdmin.Domain.Entities;
using WebDBAdmin.Domain.Interfaces;

namespace WebDBAdmin.Application.Services;

public class SchemaService : ISchemaService
{
    private readonly IDatabaseMetadataService _metadataService;

    public SchemaService(IDatabaseMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    public async Task<List<string>> GetDatabasesAsync(ConnectionInfo connectionInfo)
    {
        try
        {
            return await _metadataService.GetDatabasesAsync(connectionInfo);
        }
        catch (Exception)
        {
            // Log error?
            // For now, rethrow or return empty to let caller handle notification
            throw;
        }
    }

    public async Task<List<string>> GetTablesAsync(ConnectionInfo connectionInfo)
    {
        try
        {
            return await _metadataService.GetTablesAsync(connectionInfo);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<ColumnDefinition>> GetColumnsAsync(ConnectionInfo connectionInfo, string tableName)
    {
        try
        {
            var columns = await _metadataService.GetColumnsAsync(connectionInfo, tableName);
            return columns.Select(c => new ColumnDefinition
            {
                Name = c.Name,
                Type = c.Type,
                IsNullable = c.IsNullable,
                IsPrimaryKey = c.IsPrimaryKey,
                Length = c.Length
            }).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }
}

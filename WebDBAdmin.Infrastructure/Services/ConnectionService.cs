using System.Text.Json;
using WebDBAdmin.Application.Interfaces;
using WebDBAdmin.Domain.Entities;

namespace WebDBAdmin.Infrastructure.Services;

public class ConnectionService : IConnectionService
{
    private readonly string _filePath;

    public ConnectionService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "UniversalDBStudio");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        _filePath = Path.Combine(appFolder, "connections.json");
    }

    public async Task<List<ConnectionInfo>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<ConnectionInfo>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<ConnectionInfo>>(json) ?? new List<ConnectionInfo>();
        }
        catch
        {
            return new List<ConnectionInfo>();
        }
    }

    public async Task<ConnectionInfo?> GetAsync(Guid id)
    {
        var connections = await GetAllAsync();
        return connections.FirstOrDefault(c => c.Id == id);
    }

    public async Task SaveAsync(ConnectionInfo connection)
    {
        var connections = await GetAllAsync();
        var existing = connections.FirstOrDefault(c => c.Id == connection.Id);

        if (existing != null)
        {
            // Update existing
            // Simple replace
            var index = connections.IndexOf(existing);
            connections[index] = connection;
        }
        else
        {
            connections.Add(connection);
        }

        await SaveToFileAsync(connections);
    }

    public async Task DeleteAsync(Guid id)
    {
        var connections = await GetAllAsync();
        var toRemove = connections.FirstOrDefault(c => c.Id == id);
        if (toRemove != null)
        {
            connections.Remove(toRemove);
            await SaveToFileAsync(connections);
        }
    }

    private async Task SaveToFileAsync(List<ConnectionInfo> connections)
    {
        var json = JsonSerializer.Serialize(connections, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}

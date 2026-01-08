using WebDBAdmin.Domain.Entities;

namespace WebDBAdmin.Application.Interfaces;

public interface IConnectionService
{
    Task<List<ConnectionInfo>> GetAllAsync();
    Task<ConnectionInfo?> GetAsync(Guid id);
    Task SaveAsync(ConnectionInfo connection);
    Task DeleteAsync(Guid id);
}

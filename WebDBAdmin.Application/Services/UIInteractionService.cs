using WebDBAdmin.Domain.Entities;

namespace WebDBAdmin.Application.Services;

public class UIInteractionService
{
    // Event triggered when a query is requested from another component (e.g. Schema Browser)
    public event Action<string>? OnQueryRequested;

    // Event triggered when a table load is requested (XPO dynamic mode)
    // Args: ConnectionInfo (to ensure context), TableName
    public event Action<ConnectionInfo, string>? OnTableLoadRequested;

    public void RequestTableLoad(ConnectionInfo connectionInfo, string tableName)
    {
        OnTableLoadRequested?.Invoke(connectionInfo, tableName);
    }
}

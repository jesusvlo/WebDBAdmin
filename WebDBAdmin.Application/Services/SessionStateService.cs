using WebDBAdmin.Domain.Entities;


namespace WebDBAdmin.Application.Services;

public class SessionStateService
{
    public ConnectionInfo? CurrentConnection { get; private set; }

    // Event to notify UI when connection changes
    public event Action? OnChange;

    public void SetConnection(ConnectionInfo connectionInfo)
    {
        CurrentConnection = connectionInfo;
        NotifyStateChanged();
    }

    public bool IsConnected => CurrentConnection != null;

    private void NotifyStateChanged() => OnChange?.Invoke();
}

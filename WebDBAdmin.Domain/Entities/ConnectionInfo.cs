using UniversalDBStudio.Domain.Enums;

namespace UniversalDBStudio.Domain.Entities;

public class ConnectionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DatabaseEngine Engine { get; set; }
    public string Server { get; set; } = "localhost";
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool TrustedConnection { get; set; }

    public string GetConnectionString()
    {
        // This might be better placed in Infrastructure, but keeping a basic format here is useful 
        // or just identifying details. 
        // We will leave the actual connection string generation to the Infrastructure layer 
        // to avoid dependency on specific drivers here.
        return $"{Engine}:{Server}:{Database}";
    }
}

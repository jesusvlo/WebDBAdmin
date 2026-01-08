using Microsoft.Extensions.DependencyInjection;

namespace WebDBAdmin.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Interfaces.ISchemaService, Services.SchemaService>();
        services.AddScoped<Services.SessionStateService>();
        services.AddScoped<Services.UIInteractionService>();
        return services;
    }
}

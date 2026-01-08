using Microsoft.Extensions.DependencyInjection;
using WebDBAdmin.Domain.Interfaces;
using WebDBAdmin.Application.Interfaces;
using WebDBAdmin.Infrastructure.Services;

using RepoDb;

namespace WebDBAdmin.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Initialize RepoDB
        GlobalConfiguration.Setup()
            .UseSqlServer()
            .UseMySql()
            .UsePostgreSql();

        services.AddScoped<IConnectionFactory, ConnectionFactory>();
        services.AddScoped<IDatabaseMetadataService, DatabaseMetadataService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<ITableService, TableService>();

        GlobalConfiguration.Setup().UseMySql();
        GlobalConfiguration.Setup().UseSqlServer();
        GlobalConfiguration.Setup().UsePostgreSql();

        return services;
    }
}

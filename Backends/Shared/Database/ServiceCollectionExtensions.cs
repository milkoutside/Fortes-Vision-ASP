using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Statuses;
using Shared.Database.Repositories;
using Shared.Domain.Statuses;


namespace Shared.Database;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedDatabase(this IServiceCollection services, IConfiguration configuration, string connectionName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Не найдена строка подключения '{connectionName}'. Задайте ConnectionStrings:{connectionName} или переменную окружения ConnectionStrings__{connectionName}.");
        }

        services.AddDbContext<SharedDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySql => mySql.MigrationsAssembly(typeof(SharedDbContext).Assembly.FullName));
        });

        // Repositories
        services.AddScoped<IStatusRepository, StatusRepository>();
        // Application services
        services.AddScoped<IStatusService, StatusService>();

        return services;
    }
}



using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Shared.Database;

public class DesignTimeSharedDbContextFactory : IDesignTimeDbContextFactory<SharedDbContext>
{
    public SharedDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SharedDbContext>();

        // Берём строку подключения из ENV, иначе дефолт для локалки (Docker MySQL на localhost)
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? "server=localhost;port=3306;database=fortesdb;user=fortes;password=fortes_pass;TreatTinyAsBoolean=false";

        // Важно: фиксируем версию, НЕ используем AutoDetect — так EF CLI не будет коннектиться при scaffolding
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 34));

        optionsBuilder.UseMySql(
            cs,
            serverVersion,
            mySql => mySql.MigrationsAssembly(typeof(SharedDbContext).Assembly.FullName));

        return new SharedDbContext(optionsBuilder.Options);
    }
}



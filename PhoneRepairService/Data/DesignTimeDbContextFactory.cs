using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySql.EntityFrameworkCore.Extensions;

namespace PhoneRepairService.Data;

/// <summary>Создание контекста для команд миграций (Add-Migration, Update-Database, dotnet ef).</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RepairDbContext>
{
    public RepairDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        string connectionString = configuration.GetConnectionString("RepairDb")
            ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:RepairDb.");
        return new RepairDbContext(new DbContextOptionsBuilder<RepairDbContext>().UseMySQL(connectionString).Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMysticForgeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<MysticForgeDbContext>(options => options
            .UseNpgsql(connectionString, npg => npg.UseVector())
            .UseSnakeCaseNamingConvention());

        return services;
    }
}

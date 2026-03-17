using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kaaiman_reizen.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMainContext(this IServiceCollection services, string connectionString)
    {
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        services.AddDbContext<MainContext>(options =>
            options.UseMySql(connectionString, serverVersion));
        return services;
    }

    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddScoped<ITravelLeaderService, TravelLeaderService>();
        return services;
    }
}

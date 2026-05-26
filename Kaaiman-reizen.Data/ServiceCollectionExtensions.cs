using Kaaiman_reizen.Data.Seeders;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddScoped<IJourneyService, JourneyService>();
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddScoped<IRuleService, RuleService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPhoneNumberService, PhoneNumberService>();
        return services;
    }

    /// <summary>
    /// Registreert de DatabaseSeeder uitsluitend wanneer de omgeving Development is.
    /// In productie is deze service niet beschikbaar in de DI-container.
    /// </summary>
    public static IServiceCollection AddDevSeeder(this IServiceCollection services, IHostEnvironment env)
    {
        if (env.IsDevelopment())
            services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

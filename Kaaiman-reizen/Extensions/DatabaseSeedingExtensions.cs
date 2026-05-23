using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kaaiman_reizen.Extensions;

public static class DatabaseSeedingExtensions
{
    /// <summary>
    /// Migreert de database en voert de benodigde seeders uit.
    /// De Bogus-seeder runt uitsluitend in de ontwikkelomgeving.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            var context = services.GetRequiredService<MainContext>();

            // Stap 1: EF Core-migraties uitvoeren.
            // HasData voor Rules wordt hier automatisch meegenomen.
            await context.Database.MigrateAsync();
            logger.LogInformation("Database-migraties succesvol uitgevoerd.");

            // Stap 2: Identity-rollen en standaard gebruikers aanmaken.
            // Dit is productie-veilig en mag altijd draaien.
            await SeedIdentityAsync(services, logger);

            // Stap 3: Bogus-seeder uitsluitend in ontwikkeling uitvoeren.
            // De productiedatabase wordt NOOIT gevuld met testdata.
            if (app.Environment.IsDevelopment())
            {
                var seeder = services.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fout opgetreden tijdens database-migratie of seeding.");
            throw;
        }
    }

    // ─── IDENTITY SEEDING ────────────────────────────────────────────────────────

    private static async Task SeedIdentityAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<MainContext>();

        string[] roles = ["Planner", "Reisleider"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var defaultUsers = new[]
        {
            new { Email = "planner@kaaiman.nl",    Role = "Planner",    TravelLeaderName = (string?)null },
            new { Email = "reisleider@kaaiman.nl", Role = "Reisleider", TravelLeaderName = (string?)"Jan de Vries" },
            new { Email = "maria@kaaiman.nl",      Role = "Reisleider", TravelLeaderName = (string?)"Maria Jansen" }
        };

        foreach (var config in defaultUsers)
        {
            var account = await userManager.FindByEmailAsync(config.Email);

            if (account is null)
            {
                account = new ApplicationUser
                {
                    UserName = config.Email,
                    Email = config.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(account, "Kaaiman26!");
                if (!result.Succeeded)
                {
                    logger.LogWarning("Kon gebruiker {Email} niet aanmaken: {Errors}",
                        config.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            if (!await userManager.IsInRoleAsync(account, config.Role))
                await userManager.AddToRoleAsync(account, config.Role);

            if (config.TravelLeaderName is null)
                continue;

            var travelLeader = await dbContext.TravelLeader
                .FirstOrDefaultAsync(t => t.Name == config.TravelLeaderName);

            if (travelLeader is null)
                continue;

            await UpsertClaimAsync(userManager, account, "TravelLeaderId", travelLeader.Id.ToString());
            await UpsertClaimAsync(userManager, account, ClaimTypes.Name, travelLeader.Name);
        }
    }

    private static async Task UpsertClaimAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser account,
        string claimType,
        string newValue)
    {
        var claims = await userManager.GetClaimsAsync(account);
        var existing = claims.FirstOrDefault(c => c.Type == claimType);

        if (existing is null)
            await userManager.AddClaimAsync(account, new Claim(claimType, newValue));
        else if (existing.Value != newValue)
            await userManager.ReplaceClaimAsync(account, existing, new Claim(claimType, newValue));
    }
}

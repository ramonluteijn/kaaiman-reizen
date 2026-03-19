using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Kaaiman_reizen.Data.Identity;

namespace Kaaiman_reizen.Data;

public class MainContext : IdentityDbContext<ApplicationUser>
{
    public MainContext(DbContextOptions<MainContext> options) : base(options)
    {
    }

    public DbSet<TravelLeader> TravelLeader { get; set; }
    public DbSet<PreferredDestination> PreferredDestinations { get; set; }
    public DbSet<AvailabilityPeriod> AvailabilityPeriods { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        CreateRelations(builder);
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<TravelLeader>().HasData(
            new TravelLeader { Id = 1, Name = "Jan de Vries", PhoneNumber = "06-12345678", AmountOfTrips = 8 },
            new TravelLeader { Id = 2, Name = "Maria Jansen", PhoneNumber = "06-87654321", AmountOfTrips = 12 }
        );
        builder.Entity<PreferredDestination>().HasData(
            new PreferredDestination { Id = 1, TravelLeaderId = 1, Rank = 1, Destination = "Italië" },
            new PreferredDestination { Id = 2, TravelLeaderId = 1, Rank = 2, Destination = "Griekenland" },
            new PreferredDestination { Id = 3, TravelLeaderId = 1, Rank = 3, Destination = "Kroatië" },
            new PreferredDestination { Id = 4, TravelLeaderId = 2, Rank = 1, Destination = "Spanje" },
            new PreferredDestination { Id = 5, TravelLeaderId = 2, Rank = 2, Destination = "Portugal" },
            new PreferredDestination { Id = 6, TravelLeaderId = 2, Rank = 3, Destination = "Marokko" }
        );
        // Seed availability periods for examples
        builder.Entity<AvailabilityPeriod>().HasData(
            new AvailabilityPeriod { Id = 1, TravelLeaderId = 1, Start = new DateOnly(2025, 4, 29), End = new DateOnly(2025, 5, 3) },
            new AvailabilityPeriod { Id = 2, TravelLeaderId = 1, Start = new DateOnly(2025, 5, 30), End = new DateOnly(2025, 6, 14) },
            new AvailabilityPeriod { Id = 3, TravelLeaderId = 2, Start = new DateOnly(2025, 1, 1), End = new DateOnly(2025, 12, 31) }
        );
    }

    private static void CreateRelations(ModelBuilder builder)
    {
        builder.Entity<PreferredDestination>()
            .HasOne(p => p.TravelLeader)
            .WithMany(r => r.PreferredDestinations)
            .HasForeignKey(p => p.TravelLeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AvailabilityPeriod>()
            .HasOne(a => a.TravelLeader)
            .WithMany(t => t.AvailabilityPeriods)
            .HasForeignKey(a => a.TravelLeaderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using Microsoft.EntityFrameworkCore;
using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data;

public class MainContext : DbContext
{
    public MainContext(DbContextOptions<MainContext> options) : base(options)
    {
    }

    public DbSet<Reisleider> Reisleiders { get; set; }
    public DbSet<PreferredDestination> PreferredDestinations { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        CreateRelations(builder);
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<Reisleider>().HasData(
            new Reisleider { Id = 1, Name = "Jan de Vries", PhoneNumber = "06-12345678", AmountOfTrips = 8, WhenAvailable = "April–juni 2025" },
            new Reisleider { Id = 2, Name = "Maria Jansen", PhoneNumber = "06-87654321", AmountOfTrips = 12, WhenAvailable = "Hele jaar" }
        );
        builder.Entity<PreferredDestination>().HasData(
            new PreferredDestination { Id = 1, ReisleiderId = 1, Rank = 1, Destination = "Italië" },
            new PreferredDestination { Id = 2, ReisleiderId = 1, Rank = 2, Destination = "Griekenland" },
            new PreferredDestination { Id = 3, ReisleiderId = 1, Rank = 3, Destination = "Kroatië" },
            new PreferredDestination { Id = 4, ReisleiderId = 2, Rank = 1, Destination = "Spanje" },
            new PreferredDestination { Id = 5, ReisleiderId = 2, Rank = 2, Destination = "Portugal" },
            new PreferredDestination { Id = 6, ReisleiderId = 2, Rank = 3, Destination = "Marokko" }
        );
    }

    private static void CreateRelations(ModelBuilder builder)
    {
        builder.Entity<PreferredDestination>()
            .HasOne(p => p.Reisleider)
            .WithMany(r => r.PreferredDestinations)
            .HasForeignKey(p => p.ReisleiderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
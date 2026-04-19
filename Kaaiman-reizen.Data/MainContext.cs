using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Kaaiman_reizen.Data.Identity;
using System.Collections;

namespace Kaaiman_reizen.Data;

public class MainContext : IdentityDbContext<ApplicationUser>
{
    public MainContext(DbContextOptions<MainContext> options) : base(options)
    {
    }

    public DbSet<TravelLeader> TravelLeader { get; set; }
    public DbSet<PreferredDestination> PreferredDestinations { get; set; }
    public DbSet<AvailabilityPeriod> AvailabilityPeriods { get; set; }
    public DbSet<Journey> Journey { get; set; }
    public DbSet<Rule> Rule { get; set; }
    public DbSet<PlanningVersion> PlanningVersions { get; set; }
    public DbSet<PlanningAssignment> PlanningAssignments { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        CreateRelations(builder);
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<TravelLeader>().HasData(
            new TravelLeader
            {
                Id = 1,
                Name = "Jan de Vries",
                PhoneNumber = "06-12345678",
                AmountOfTrips = 8,
                MinTrips = 2,
                MaxTrips = 10,
                IsActive = true,
                Note = ""
            },
            new TravelLeader
            {
                Id = 2,
                Name = "Maria Jansen",
                PhoneNumber = "06-87654321",
                AmountOfTrips = 12,
                MinTrips = 3,
                MaxTrips = 15,
                IsActive = true,
                Note = ""
            }
        );
        builder.Entity<PreferredDestination>().HasData(
            new PreferredDestination { Id = 1, TravelLeaderId = 1, Rank = 1, Destination = "Italië" },
            new PreferredDestination { Id = 2, TravelLeaderId = 1, Rank = 2, Destination = "Griekenland" },
            new PreferredDestination { Id = 3, TravelLeaderId = 1, Rank = 3, Destination = "Kroatië" },
            new PreferredDestination { Id = 4, TravelLeaderId = 2, Rank = 1, Destination = "Spanje" },
            new PreferredDestination { Id = 5, TravelLeaderId = 2, Rank = 2, Destination = "Oostenrijk" },
            new PreferredDestination { Id = 6, TravelLeaderId = 2, Rank = 3, Destination = "Griekenland" }
        );
        // Jan is available for spring (Griekenland, Kroatië) and summer (Italië)
        // Maria is available for the early spring trips (Spanje, Oostenrijk) and into summer
        builder.Entity<AvailabilityPeriod>().HasData(
            new AvailabilityPeriod { Id = 1, TravelLeaderId = 1, Start = new DateOnly(2026, 4, 1), End = new DateOnly(2026, 7, 31) },
            new AvailabilityPeriod { Id = 2, TravelLeaderId = 2, Start = new DateOnly(2026, 3, 1), End = new DateOnly(2026, 5, 31) }
        );
        builder.Entity<Journey>().HasData(
            new Journey { Id = 1, Name = "Italië",      Start = new DateOnly(2026, 7, 1),  End = new DateOnly(2026, 7, 14), Busses = 1, Travelers = 10, RequiredLeaders = 1, BookingStatus = 0 },
            new Journey { Id = 2, Name = "Spanje",      Start = new DateOnly(2026, 3, 10), End = new DateOnly(2026, 3, 20), Busses = 2, Travelers = 15, RequiredLeaders = 1, BookingStatus = 0 },
            new Journey { Id = 3, Name = "Oostenrijk",  Start = new DateOnly(2026, 3, 25), End = new DateOnly(2026, 4, 3),  Busses = 1, Travelers = 8,  RequiredLeaders = 1, BookingStatus = 1 },
            new Journey { Id = 4, Name = "Griekenland", Start = new DateOnly(2026, 4, 5),  End = new DateOnly(2026, 4, 15), Busses = 3, Travelers = 25, RequiredLeaders = 2, BookingStatus = 2 },
            new Journey { Id = 5, Name = "Kroatië",     Start = new DateOnly(2026, 4, 28), End = new DateOnly(2026, 5, 10), Busses = 2, Travelers = 12, RequiredLeaders = 1, BookingStatus = 2 }
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

        builder.Entity<Journey>()
           .HasMany(a => a.TravelLeaders)
           .WithMany(r => r.Journeys);

        builder.Entity<PlanningAssignment>()
            .HasOne(assignment => assignment.PlanningVersion)
            .WithMany(version => version.Assignments)
            .HasForeignKey(assignment => assignment.PlanningVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningAssignment>()
            .HasOne(assignment => assignment.Journey)
            .WithMany()
            .HasForeignKey(assignment => assignment.JourneyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanningAssignment>()
            .HasOne(assignment => assignment.TravelLeader)
            .WithMany()
            .HasForeignKey(assignment => assignment.TravelLeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Rule>()
            .HasIndex(rule => rule.Key)
            .IsUnique();
    }
}

using Kaaiman_reizen.Data.Configuration;
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
    public DbSet<TravelLeaderAvailabilityHistory> TravelLeaderAvailabilityHistories { get; set; }
    public DbSet<AvailabilityPeriod> AvailabilityPeriods { get; set; }
    public DbSet<Journey> Journey { get; set; }
    public DbSet<JourneyTravelLeader> JourneyTravelLeaders { get; set; }
    public DbSet<Rule> Rule { get; set; }
    public DbSet<PlanningVersion> PlanningVersions { get; set; }
    public DbSet<PlanningAssignment> PlanningAssignments { get; set; }
    public DbSet<PlanningRound> PlanningRounds { get; set; }
    public DbSet<PlanningRoundParticipation> PlanningRoundParticipations { get; set; }
    public DbSet<PlanningRoundPreference> PlanningRoundPreferences { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<JourneyNotificationHistory> JourneyNotificationHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        CreateRelations(builder);

        // Only static data in alle environments.
        builder.ApplyConfiguration(new RuleConfiguration());
    }

    private static void CreateRelations(ModelBuilder builder)
    {
        builder.Entity<PreferredDestination>()
            .HasOne(p => p.TravelLeader)
            .WithMany(r => r.PreferredDestinations)
            .HasForeignKey(p => p.TravelLeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PreferredDestination>()
            .HasOne(p => p.Journey)
            .WithMany()
            .HasForeignKey(p => p.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AvailabilityPeriod>()
            .HasOne(a => a.TravelLeader)
            .WithMany(t => t.AvailabilityPeriods)
            .HasForeignKey(a => a.TravelLeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TravelLeaderAvailabilityHistory>()
            .HasOne(h => h.TravelLeader)
            .WithMany()
            .HasForeignKey(h => h.TravelLeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TravelLeaderAvailabilityHistory>()
            .HasOne(h => h.PlanningVersion)
            .WithMany()
            .HasForeignKey(h => h.PlanningVersionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TravelLeaderAvailabilityHistory>()
            .HasOne(h => h.Journey)
            .WithMany()
            .HasForeignKey(h => h.JourneyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Journey>()
           .HasMany(a => a.TravelLeaders)
           .WithMany(r => r.Journeys)
           .UsingEntity<JourneyTravelLeader>(
                right => right
                    .HasOne(join => join.TravelLeader)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasForeignKey(join => join.TravelLeaderId),
                left => left
                    .HasOne(join => join.Journey)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasForeignKey(join => join.JourneyId),
                join =>
                {
                    join.HasKey(x => new { x.JourneyId, x.TravelLeaderId });
                    join.Property(x => x.JourneyId).HasColumnName("JourneysId");
                    join.Property(x => x.TravelLeaderId).HasColumnName("TravelLeadersId");
                });

        builder.Entity<PlanningAssignment>()
            .HasOne(assignment => assignment.PlanningVersion)
            .WithMany(version => version.Assignments)
            .HasForeignKey(assignment => assignment.PlanningVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningAssignment>()
            .HasOne(assignment => assignment.Journey)
            .WithMany()
            .HasForeignKey(assignment => assignment.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningAssignment>()
            .HasOne(assignment => assignment.TravelLeader)
            .WithMany()
            .HasForeignKey(assignment => assignment.TravelLeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanningVersion>()
            .HasOne(v => v.PlanningRound)
            .WithMany(r => r.Versions)
            .HasForeignKey(v => v.PlanningRoundId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PlanningRoundParticipation>()
            .HasOne(p => p.PlanningRound)
            .WithMany(r => r.Participations)
            .HasForeignKey(p => p.PlanningRoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningRoundParticipation>()
            .HasOne(p => p.TravelLeader)
            .WithMany()
            .HasForeignKey(p => p.TravelLeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningRoundParticipation>()
            .HasIndex(p => new { p.PlanningRoundId, p.TravelLeaderId })
            .IsUnique();

        builder.Entity<PlanningRoundPreference>()
            .HasOne(p => p.Participation)
            .WithMany(par => par.Preferences)
            .HasForeignKey(p => p.PlanningRoundParticipationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningRoundPreference>()
            .HasOne(p => p.Journey)
            .WithMany()
            .HasForeignKey(p => p.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanningRoundPreference>()
            .HasIndex(p => new { p.PlanningRoundParticipationId, p.JourneyId })
            .IsUnique();
    }
}

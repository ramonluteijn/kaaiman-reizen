using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaaiman_reizen.Data.Configuration;

// This is the same in each environment so should be seeded through HasData.
internal sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasIndex(r => r.Key).IsUnique();

        builder.HasData(
            new Rule { Id = 1, Key = "NoOverlap", Description = "Reisleider mag geen overlappende reizen hebben.", IsActive = true },
            new Rule { Id = 2, Key = "MinimumGapDays", Description = "Minimaal aantal dagen tussen twee reizen.", IsActive = true, Value = "3" },
            new Rule { Id = 3, Key = "RequiredExperience", Description = "Minimaal aantal reizen ervaring voor niet-standaard bestemmingen.", IsActive = true, Value = "3" },
            new Rule { Id = 4, Key = "MinMaxJourneys", Description = "Controle op minimum/maximum aantal reizen per reisleider.", IsActive = true }
        );
    }
}
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Rules;
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
            new Rule { Id = 1, Key = RuleKeys.NoOverlap, Description = "Reisleider mag geen overlappende reizen hebben.", IsActive = true, Weight = 1 },
            new Rule { Id = 2, Key = RuleKeys.MinimumGapDays, Description = "Minimaal aantal dagen tussen twee reizen.", IsActive = true, Value = "3", Weight = 1 },
            new Rule { Id = 3, Key = RuleKeys.RequiredExperience, Description = "Minimaal aantal reizen ervaring voor niet-standaard bestemmingen.", IsActive = true, Value = "3", Weight = 1 },
            new Rule { Id = 4, Key = RuleKeys.MinMaxJourneys, Description = "Controle op minimum/maximum aantal reizen per reisleider.", IsActive = true, Weight = 1 },
            new Rule { Id = 5, Key = RuleKeys.PreferencesEnabled, Description = "Reisleider krijgt voorkeur voor reizen naar zijn favoriete bestemmingen.", IsActive = true, Weight = 1 },
            new Rule { Id = 6, Key = RuleKeys.JourneyReminderEnabled, Description = "Versturen van reisnotificaties voor aankomende reizen.", IsActive = true, Value = "true", Weight = 1 },
            new Rule { Id = 7, Key = RuleKeys.JourneyReminderDays, Description = "Aantal dagen voor vertrek waarop reisnotificaties worden verstuurd (komma-gescheiden).", IsActive = true, Value = "7,3", Weight = 1 }
        );
    }
}

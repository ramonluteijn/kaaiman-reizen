using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Rules;

namespace Kaaiman_reizen.Tests.Rules;

public class RuleWeightTests
{
    [Fact]
    public void RuleEntity_HasWeightProperty_WithDefaultOne()
    {
        // Arrange & Act
        var rule = new Rule
        {
            Id = 1,
            Key = "TestRule",
            Description = "Test",
            IsActive = true
        };

        // Assert
        Assert.Equal(1, rule.Weight); // Default weight should be 1
    }

    [Fact]
    public void RuleEntity_CanSetCustomWeight()
    {
        // Arrange
        var rule = new Rule
        {
            Id = 1,
            Key = "TestRule",
            Description = "Test",
            IsActive = true,
            Weight = 10
        };

        // Act & Assert
        Assert.Equal(10, rule.Weight);
    }

    [Fact]
    public void CheckRules_FromRules_ReadsWeights()
    {
        // Arrange: Create test rules with different weights
        var rules = new List<Rule>
        {
            new Rule { Id = 1, Key = "NoOverlap", Description = "No overlap", IsActive = true, Weight = 5 },
            new Rule { Id = 2, Key = "MinimumGapDays", Description = "Gap", IsActive = true, Value = "3", Weight = 3 },
            new Rule { Id = 3, Key = "RequiredExperience", Description = "Experience", IsActive = true, Value = "2", Weight = 2 },
            new Rule { Id = 4, Key = "MinMaxJourneys", Description = "Min/Max", IsActive = true, Weight = 1 },
            new Rule { Id = 5, Key = "PreferencesEnabled", Description = "Preferences", IsActive = true, Weight = 1 }
        };

        // Act
        var settings = CheckRules.FromRules(rules);

        // Assert - Verify weights are read from base rules
        Assert.Equal(5, settings.NoOverlapWeight);
        Assert.Equal(3, settings.MinimumGapWeight);
        Assert.Equal(2, settings.RequiredExperienceWeight);
        // PreferenceWeight falls back to the base PreferencesEnabled rule weight when no explicit "PreferenceWeight" rule is provided
        Assert.Equal(1, settings.PreferenceWeight);
    }

    [Fact]
    public void CheckRules_PreferencesEnabledRuleExists()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule { Id = 5, Key = "PreferencesEnabled", Description = "Preferences", IsActive = true, Weight = 1 }
        };

        // Act
        var settings = CheckRules.FromRules(rules);

        // Assert - Verify preferences are enabled by default
        Assert.True(settings.PreferencesEnabled);
        Assert.Equal(1, settings.PreferenceWeight); // Falls back to the rule weight on PreferencesEnabled
    }

    [Fact]
    public void CheckRules_TogglePreferencesWithExplicitRule()
    {
        // Arrange: Create rule where PreferencesEnabled IsActive = false to disable preferences
        var rules = new List<Rule>
        {
            new Rule { Id = 1, Key = "PreferencesEnabled", Description = "Enable/disable", IsActive = false, Weight = 1 }
        };

        // Act
        var settings = CheckRules.FromRules(rules);

        // Assert - Verify preferences can be disabled by setting IsActive to false
        Assert.False(settings.PreferencesEnabled);
    }
}






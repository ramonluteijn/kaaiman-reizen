using Kaaiman_reizen.Data.Rules;

namespace Kaaiman_reizen.Tests.Rules;

public class HasExperienceTests
{
    [Fact]
    public void Check_ReturnsTrue_ForAccentedCountryName()
    {
        var canTravel = HasExperience.Check(requiredExperience: 3, destination: "Italië", experience: 0);

        Assert.True(canTravel);
    }

    [Fact]
    public void Check_ReturnsTrue_ForHyphenAndUnderscoreVariants()
    {
        var hyphenVariant = HasExperience.Check(requiredExperience: 5, destination: "Wit-Rusland", experience: 0);
        var underscoreVariant = HasExperience.Check(requiredExperience: 5, destination: "Wit_Rusland", experience: 0);

        Assert.True(hyphenVariant);
        Assert.True(underscoreVariant);
    }

    [Fact]
    public void Check_FallsBackToExperience_ForUnknownDestination()
    {
        var insufficientExperience = HasExperience.Check(requiredExperience: 2, destination: "Atlantis", experience: 1);
        var enoughExperience = HasExperience.Check(requiredExperience: 2, destination: "Atlantis", experience: 2);

        Assert.False(insufficientExperience);
        Assert.True(enoughExperience);
    }
}


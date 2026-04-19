using Kaaiman_reizen.Data.Enum;

namespace Kaaiman_reizen.Data.Rules;

public static class HasExperience
{
    public static bool Check(int requiredExperience, string destination, int? experience = 0)
    {
        var normalizedDestination = CountryMappings.NormalizeCountryName(destination);
        // check alternative county names
        if (CountryMappings.AlternativeCountryNames.Keys.Any(key => CountryMappings.NormalizeCountryName(key) == normalizedDestination))
        {
            return true;
        }
        // check regular countries list
        if (System.Enum.GetNames<Countries>().Any(countryName => CountryMappings.NormalizeCountryName(countryName) == normalizedDestination))
        {
            return true;
        }
        return experience >= requiredExperience;
    }
}

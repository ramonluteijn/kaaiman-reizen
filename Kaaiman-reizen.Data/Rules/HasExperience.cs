using Kaaiman_reizen.Data.Enum;

namespace Kaaiman_reizen.Data.Rules;

public static class HasExperience
{
    public static bool Check(int requiredExperience, string destination, int? experience = 0)
    {
        // check alternative county names
        if (CountryMappings.AlternativeCountryNames.TryGetValue(destination, out _))
        {
            return true;
        }
        // check regular countries list
        if (System.Enum.TryParse<Countries>(destination.Replace("-", "_").Replace(" ", ""), true, out _))
        {
            return true;
        }
        return experience >= requiredExperience;
    }
}

using Kaaiman_reizen.Data.Enum;

namespace Kaaiman_reizen.Data.Rules;

public static class HasExperience
{
    public static bool Check(int requiredExperience, string destination, int? experience = 0)
    {
        // If destination is in Europe, always allowed
        if (System.Enum.TryParse<Countries>(destination, true, out _))
        {
            return true;
        }
        return experience >= requiredExperience;
    }
}

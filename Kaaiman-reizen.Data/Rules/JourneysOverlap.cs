namespace Kaaiman_reizen.Data.Rules;

public static class JourneysOverlap
{
    public static bool Check(DateTime firstStart, DateTime firstEnd, DateTime secondStart, DateTime secondEnd)
    {
        return firstStart < secondEnd && secondStart < firstEnd;
    }
}

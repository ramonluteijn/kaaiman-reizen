namespace Kaaiman_reizen.Data.Rules;

public static class JourneysOverlap
{
    public static bool Check(DateOnly firstStart, DateOnly firstEnd, DateOnly secondStart, DateOnly secondEnd)
    {
        return firstStart < secondEnd && secondStart < firstEnd;
    }
}

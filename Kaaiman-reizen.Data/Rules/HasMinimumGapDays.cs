namespace Kaaiman_reizen.Data.Rules;

public static class HasMinimumGapDays
{
    public static bool Check(DateTime existingStart,
        DateTime existingEnd,
        DateTime candidateStart,
        DateTime candidateEnd,
        int minimumGapDays)
    {
        if (JourneysOverlap.Check(existingStart, existingEnd, candidateStart, candidateEnd))
        {
            return false;
        }

        var gap = candidateStart >= existingEnd
            ? (candidateStart.Date - existingEnd.Date).TotalDays
            : (existingStart.Date - candidateEnd.Date).TotalDays;

        return gap >= minimumGapDays;
    }
}

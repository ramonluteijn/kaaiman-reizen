namespace Kaaiman_reizen.Data.Rules;

public static class HasMinimumGapDays
{
    public static bool Check(
        DateOnly existingStart,
        DateOnly existingEnd,
        DateOnly candidateStart,
        DateOnly candidateEnd,
        int minimumGapDays)
    {
        if (JourneysOverlap.Check(existingStart, existingEnd, candidateStart, candidateEnd))
        {
            return false;
        }

        var gap = candidateStart >= existingEnd
            ? (candidateStart.ToDateTime(TimeOnly.MinValue) - existingEnd.ToDateTime(TimeOnly.MinValue)).TotalDays
            : (existingStart.ToDateTime(TimeOnly.MinValue) - candidateEnd.ToDateTime(TimeOnly.MinValue)).TotalDays;

        return gap >= minimumGapDays;
    }
}

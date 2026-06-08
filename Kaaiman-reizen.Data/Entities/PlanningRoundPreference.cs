namespace Kaaiman_reizen.Data.Entities;

/// <summary>Voorkeur van een reisleider voor een specifieke ronde. Rank 1-3 = voorkeur, Rank 0 = beschikbaar maar geen voorkeur.</summary>
public class PlanningRoundPreference
{
    public int Id { get; set; }
    public int PlanningRoundParticipationId { get; set; }
    public int JourneyId { get; set; }
    public int Rank { get; set; }

    public PlanningRoundParticipation Participation { get; set; } = null!;
    public Journey Journey { get; set; } = null!;
}

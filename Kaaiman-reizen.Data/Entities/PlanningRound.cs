namespace Kaaiman_reizen.Data.Entities;

public class PlanningRound
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime PreferenceDeadline { get; set; }
    public DateTime PublicationDeadline { get; set; }

    public List<PlanningRoundParticipation> Participations { get; set; } = [];
    public List<PlanningVersion> Versions { get; set; } = [];
    public List<Journey> Journeys { get; set; } = [];

    public bool IsPreferenceDeadlinePassed(DateTime? utcNow = null) =>
        (utcNow ?? DateTime.UtcNow).Date > PreferenceDeadline.Date;
}

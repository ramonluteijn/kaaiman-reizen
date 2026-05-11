namespace Kaaiman_reizen.Data.Entities;

public class PlanningVersion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }

    public int? PlanningYear { get; set; }

    public List<PlanningAssignment> Assignments { get; set; } = [];
}

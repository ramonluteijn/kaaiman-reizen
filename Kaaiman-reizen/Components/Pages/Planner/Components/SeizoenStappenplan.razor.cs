using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner.Components;

public partial class SeizoenStappenplan : ComponentBase
{
    [Parameter, EditorRequired] public StappenplanModel Model { get; set; } = default!;
}

public class StappenplanModel
{
    public string RondeNaam { get; init; } = string.Empty;
    public int Seizoen { get; init; }
    public int Voortgang { get; init; }
    public int HuidigStap { get; init; }
    public List<PlanStap> Stappen { get; init; } = [];
}

public enum StapStatus { Voltooid, Huidig, Aandacht, Toekomstig }
public enum SublabelStijl { GroenBadge, GrijsBadge, OranjeText, RoodText, GrijsText }
public enum KnopVariant { Primair, Outlined, Gevaar }

public class PlanStap
{
    public int Nummer { get; init; }
    public string Titel { get; init; } = string.Empty;
    public string Sublabel { get; init; } = string.Empty;
    public SublabelStijl SublabelStijl { get; init; }
    public StapStatus Status { get; init; }
    public string? KnopTekst { get; init; }
    public KnopVariant KnopVariant { get; init; }
    public string? KnopHref { get; init; }
}

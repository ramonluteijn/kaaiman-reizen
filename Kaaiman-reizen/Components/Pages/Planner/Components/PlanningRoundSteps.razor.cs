using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner.Components;

public partial class PlanningRoundSteps : ComponentBase
{
    [Parameter, EditorRequired] public StepPlanModel Model { get; set; } = default!;
}

public class StepPlanModel
{
    public string RoundName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Progress { get; init; }
    public int CurrentStep { get; init; }
    public List<PlanStep> Steps { get; init; } = [];
}

public enum StepStatus { Completed, Current, Attention, Future }
public enum SublabelStyle { GreenBadge, GreyBadge, OrangeText, RedText, GreyText }
public enum ButtonVariant { Primary, Outlined, Danger }

public class PlanStep
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Sublabel { get; init; } = string.Empty;
    public SublabelStyle SublabelStyle { get; init; }
    public StepStatus Status { get; init; }
    public string? ButtonText { get; init; }
    public ButtonVariant ButtonVariant { get; init; }
    public string? ButtonHref { get; init; }
}

using Google.OrTools.Sat;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.Planner;

namespace Kaaiman_reizen.Services;

public class PlannerDraftService : IPlannerDraftService
{
    private readonly ITravelLeaderService _leaderService;
    private readonly IJourneyService _journeyService;
    private readonly IRuleService _ruleService;

    public PlannerDraftService(ITravelLeaderService leaderService, IJourneyService journeyService, IRuleService ruleService)
    {
        _leaderService = leaderService;
        _journeyService = journeyService;
        _ruleService = ruleService;
    }

    public async Task<PlannerDraftRequest> BuildRequestAsync(PlanningRound round, CancellationToken ct = default)
    {
        var leaders = await _leaderService.GetTravelLeadersAsync(ct);
        var journeys = await _journeyService.GetJourneysAsync(ct);
        var rules = await _ruleService.GetRulesAsync(ct);
        var settings = Data.Rules.CheckRules.FromRules(rules);

        // Gebruik ronde-specifieke voorkeuren (PlanningRoundPreference) i.p.v. globale PreferredDestinations
        var participationByLeader = round.Participations
            .ToDictionary(p => p.TravelLeaderId, p => p);

        var activeLeaderInputs = leaders
            .Where(l => l.IsActive)
            .Select(l =>
            {
                var participation = participationByLeader.TryGetValue(l.Id, out var p)
                    ? p
                    : null;

                var prefs = participation is not null
                    ? participation.Preferences
                    : (IEnumerable<PlanningRoundPreference>)[];

                return new PlannerLeaderInput
                {
                    Id = l.Id,
                    Name = l.Name,
                    Note = participation?.Note ?? string.Empty,
                    AmountOfTrips = l.AmountOfTrips ?? 0,
                    MinTrips = participation?.MinTrips ?? 0,
                    MaxTrips = participation?.MaxTrips ?? 0,
                    PreferredDestinations = prefs.ToDictionary(x => x.JourneyId, x => x.Rank),
                    PreferredDestinationDetails = prefs
                        .Where(x => x.Rank >= 1 && x.Rank <= 3)
                        .OrderBy(x => x.Rank)
                        .Select(x => new PreferredDestinationDisplayInput
                        {
                            JourneyId = x.JourneyId,
                            JourneyTitle = x.Journey?.Name ?? $"Reis {x.JourneyId}",
                            Rank = x.Rank
                        })
                        .ToList()
                };
            }).ToList();

        return new PlannerDraftRequest
        {
            Leaders = activeLeaderInputs.Where(l => l.PreferredDestinations.Count > 0).ToList(),
            AllActiveLeaders = activeLeaderInputs,
            Journeys = journeys
                .Where(j => j.BookingStatus == BookingStatus.Huidig
                         && j.Start >= round.StartDate
                         && j.Start <= round.EndDate)
                .Select(j => new PlannerJourneyInput
                {
                    Id = j.Id,
                    Name = j.Name,
                    Start = j.Start,
                    End = j.End,
                    RequiredLeaders = j.RequiredLeaders
                })
                .ToList(),
            RuleSettings = settings
        };
    }

    public PlannerDraftResult GenerateDraft(PlannerDraftRequest request)
    {
        var result = new PlannerDraftResult();
        var leaders = request.Leaders;
        var journeys = request.Journeys;
        var settings = request.RuleSettings;

        int L = leaders.Count;
        int J = journeys.Count;

        if (L == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Geen reisleiders of geen actieve reisleiders";
            return result;
        }
        if (J == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Geen reizen gevonden";
            return result;
        }

        // Pre-solve: separate journeys that have enough available leaders from those that don't
        var solvable = new List<PlannerJourneyInput>();
        foreach (var journey in journeys)
        {
            int eligible = leaders.Count(l => l.PreferredDestinations.ContainsKey(journey.Id));

            if (eligible < journey.RequiredLeaders)
                result.JourneyWarnings[journey.Id] =
                    $"Niet genoeg reisleiders beschikbaar ({eligible} van {journey.RequiredLeaders} vereist). " +
                    "Controleer of reisleiders deze reis hebben geselecteerd als beschikbaarheid of voorkeur.";
            else
                solvable.Add(journey);
        }

        result.IsSuccess = true;

        if (solvable.Count == 0)
            return result;

        int Js = solvable.Count;

        var model = new CpModel();

        var x = new BoolVar[L, Js];
        for (int l = 0; l < L; l++)
            for (int j = 0; j < Js; j++)
                x[l, j] = model.NewBoolVar($"x_{l}_{j}");

        // Constraint A: each solvable journey can have at most RequiredLeaders assigned
        for (int j = 0; j < Js; j++)
        {
            var vars = Enumerable.Range(0, L).Select(l => (ILiteral)x[l, j]).Cast<BoolVar>().ToList();
            model.Add(LinearExpr.Sum(vars) <= solvable[j].RequiredLeaders);
        }

        // Constraint B: block leader if they have no availability entry for this journey.
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < Js; j++)
            {
                bool available = leaders[l].PreferredDestinations.ContainsKey(solvable[j].Id);
                if (!available)
                    model.Add(x[l, j] == 0);
            }
        }

        // Pre-calculate journey conflicts to avoid redundant checks in the leader loop
        var overlaps = new List<(int j1, int j2)>();
        var gapConflicts = new List<(int j1, int j2)>();
        for (int j1 = 0; j1 < Js; j1++)
        {
            for (int j2 = j1 + 1; j2 < Js; j2++)
            {
                var a = solvable[j1];
                var b = solvable[j2];
                if (Kaaiman_reizen.Data.Rules.JourneysOverlap.Check(a.Start, a.End, b.Start, b.End))
                    overlaps.Add((j1, j2));
                else if (!Kaaiman_reizen.Data.Rules.HasMinimumGapDays.Check(a.Start, a.End, b.Start, b.End, settings.MinimumGapDays))
                    gapConflicts.Add((j1, j2));
            }
        }

        // Constraint C: each leader can be assigned at most MaxTrips journeys (and at least MinTrips if enabled)
        for (int l = 0; l < L; l++)
        {
            var vars = Enumerable.Range(0, Js).Select(j => x[l, j]).ToList();
            var sum = LinearExpr.Sum(vars);
            if (!settings.MinMaxJourneysActive)
            {
                // If MinMaxJourneys rule is not active, still enforce any MaxTrips limits but ignore MinTrips to avoid infeasibility and allow more flexible assignments.
                if (leaders[l].MaxTrips > 0)
                {
                    model.Add(sum <= leaders[l].MaxTrips);
                }
            }
            else
            {
                if (leaders[l].MaxTrips > 0)
                {
                    model.Add(sum <= leaders[l].MaxTrips);
                }
                if (leaders[l].MinTrips > 0)
                {
                    model.Add(sum >= leaders[l].MinTrips);
                }
            }
        }

        // Constraint D: a leader cannot have two overlapping journeys. If the rule is active, enforce as hard constraint.
        // Otherwise create soft-violation variables that are penalised by the NoOverlapWeight.
        var overlapViolations = new List<IntVar>();
        if (settings.NoOverlapActive || settings.NoOverlapWeight > 0)
        {
            foreach (var (j1, j2) in overlaps)
            {
                for (int l = 0; l < L; l++)
                {
                    if (settings.NoOverlapActive)
                    {
                        model.Add(x[l, j1] + x[l, j2] <= 1);
                    }
                    else
                    {
                        // Soft constraint: penalise via objective function if both are assigned
                        var overlapVar = model.NewIntVar(0, 1, $"overlap_{l}_{j1}_{j2}");
                        model.Add(overlapVar <= x[l, j1]);
                        model.Add(overlapVar <= x[l, j2]);
                        model.Add(overlapVar * 2 >= x[l, j1] + x[l, j2]);
                        overlapViolations.Add(overlapVar);
                    }
                }
            }
        }

        // Objective: minimize total preference cost (rank 1 = cheapest, no preference = 10)
        var obj = LinearExpr.NewBuilder();
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < Js; j++)
            {
                int cost;
                if (settings.PreferencesEnabled)
                {
                    cost = leaders[l].PreferredDestinations.TryGetValue(solvable[j].Id, out int rank)
                        ? (rank == 0 ? 5 : rank)
                        : 10;
                    cost *= settings.PreferenceWeight;
                }
                else
                {
                    // preferences disabled -> neutral cost for any assignment
                    cost = 5;
                }

                obj.AddTerm(x[l, j], cost);

                // Required experience: if the rule is active enforce hard block, otherwise penalise
                if (settings.RequiredExperienceActive)
                {
                    // if leader does not have enough experience for this journey, block
                    if (!Data.Rules.HasExperience.Check(settings.RequiredExperience, solvable[j].Name, leaders[l].AmountOfTrips))
                    {
                        model.Add(x[l, j] == 0);
                    }
                }
                else if (settings.RequiredExperienceWeight > 0)
                {
                    if (!Data.Rules.HasExperience.Check(settings.RequiredExperience, solvable[j].Name, leaders[l].AmountOfTrips))
                    {
                        obj.AddTerm(x[l, j], settings.RequiredExperienceWeight);
                    }
                }
            }
        }

        // Penalise each missing leader slot — high enough to always prefer assigning over not
        const int MissingLeaderPenalty = 1000;
        for (int j = 0; j < Js; j++)
        {
            var colVars = Enumerable.Range(0, L).Select(l => x[l, j]).ToList();
            var shortfall = model.NewIntVar(0, solvable[j].RequiredLeaders, $"shortfall_{j}");
            model.Add(shortfall == solvable[j].RequiredLeaders - LinearExpr.Sum(colVars));
            obj.AddTerm(shortfall, MissingLeaderPenalty);
        }

        // Fairness: penalise leaving an eligible leader unassigned
        const int UnassignedPenalty = 15;
        for (int l = 0; l < L; l++)
        {
            bool eligible = Enumerable.Range(0, Js).Any(j =>
                leaders[l].PreferredDestinations.ContainsKey(solvable[j].Id));
            if (!eligible) continue;

            var assignedVar = model.NewBoolVar($"assigned_{l}");
            var rowVars = Enumerable.Range(0, Js).Select(j => x[l, j]).ToList();
            model.Add(LinearExpr.Sum(rowVars) >= assignedVar);
            obj.AddTerm(assignedVar, -UnassignedPenalty);
        }

        // Add penalty terms for overlap and minimum gap violations if present
        if (overlapViolations.Any())
        {
            foreach (var ov in overlapViolations)
                obj.AddTerm(ov, settings.NoOverlapWeight);
        }

        // Minimum gap: a leader cannot have two journeys with insufficient gap between them.
        // If the rule is active, enforce as hard constraint. Otherwise create soft-violation variables that are penalised by the MinimumGapWeight.
        var minGapViolations = new List<IntVar>();
        if (settings.MinimumGapDaysActive || settings.MinimumGapWeight > 0)
        {
            foreach (var (j1, j2) in gapConflicts)
            {
                for (int l = 0; l < L; l++)
                {
                    if (settings.MinimumGapDaysActive)
                    {
                        // Hard constraint: prevent assignment of both journeys
                        model.Add(x[l, j1] + x[l, j2] <= 1);
                    }
                    else
                    {
                        // Soft constraint: penalise via objective function
                        var violation = model.NewIntVar(0, 1, $"mingap_{l}_{j1}_{j2}");
                        model.Add(violation <= x[l, j1]);
                        model.Add(violation <= x[l, j2]);
                        model.Add(violation * 2 >= x[l, j1] + x[l, j2]);
                        minGapViolations.Add(violation);
                    }
                }
            }
        }

        // Add penalty terms for minimum gap soft violations if present
        if (minGapViolations.Any())
        {
            foreach (var mv in minGapViolations)
                obj.AddTerm(mv, settings.MinimumGapWeight);
        }

        model.Minimize(obj);

        var solver = new CpSolver();
        solver.StringParameters = "max_time_in_seconds:10";
        var status = solver.Solve(model);

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            for (int j = 0; j < Js; j++)
            {
                var assignments = new List<JourneyAssignmentResult>();
                for (int l = 0; l < L; l++)
                {
                    if (solver.Value(x[l, j]) == 1L)
                    {
                        int? rankMatched = leaders[l].PreferredDestinations
                            .TryGetValue(solvable[j].Id, out int r) ? (r == 0 ? null : r) : null;

                        assignments.Add(new JourneyAssignmentResult
                        {
                            LeaderId = leaders[l].Id,
                            LeaderName = leaders[l].Name,
                            RankMatched = rankMatched
                        });
                    }
                }
                if (assignments.Count > 0)
                    result.JourneyAssignments[solvable[j].Id] = assignments;

                if (assignments.Count < solvable[j].RequiredLeaders)
                    result.JourneyWarnings[solvable[j].Id] =
                        $"Niet volledig ingepland ({assignments.Count} van {solvable[j].RequiredLeaders} reisleiders). " +
                        "Wijs de overige handmatig toe.";
            }
        }
        else
        {
            foreach (var journey in solvable)
                result.JourneyWarnings[journey.Id] =
                    "Kon niet worden ingepland door conflicten of capaciteitsproblemen. " +
                    "Controleer MaxTrips-instellingen of overlappende reizen. Wijs handmatig toe.";
        }

        return result;
    }
}

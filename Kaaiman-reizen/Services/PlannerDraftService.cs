using Google.OrTools.Sat;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.Planner;

namespace Kaaiman_reizen.Services;

public class PlannerDraftService : IPlannerDraftService
{
    private readonly ITravelLeaderService _leaderService;
    private readonly IJourneyService _journeyService;

    public PlannerDraftService(ITravelLeaderService leaderService, IJourneyService journeyService)
    {
        _leaderService = leaderService;
        _journeyService = journeyService;
    }

    // 1. Voeg 'int year' toe als eerste parameter
    public async Task<PlannerDraftRequest> BuildRequestAsync(int year, CancellationToken ct = default)
    {
        var leaders = await _leaderService.GetTravelLeadersAsync(ct);
        var journeys = await _journeyService.GetJourneysAsync(ct);

        var activeLeaderInputs = leaders
            .Where(l => l.IsActive)
            .Select(l => new PlannerLeaderInput
            {
                Id = l.Id,
                Name = l.Name,
                Note = l.Note,
                AmountOfTrips = l.AmountOfTrips ?? 0,
                MinTrips = l.MinTrips ?? 0,
                MaxTrips = l.MaxTrips ?? 0,
                PreferredDestinations = l.PreferredDestinations
                    .Where(p => p.JourneyId.HasValue)
                    .ToDictionary(p => p.JourneyId!.Value, p => p.Rank),
                PreferredDestinationDetails = l.PreferredDestinations
                    .Where(p => p.JourneyId.HasValue && p.Rank >= 1 && p.Rank <= 3)
                    .OrderBy(p => p.Rank)
                    .Select(p => new PreferredDestinationDisplayInput
                    {
                        JourneyId = p.JourneyId!.Value,
                        JourneyTitle = p.Journey?.Name ?? $"Reis {p.JourneyId!.Value}",
                        Rank = p.Rank
                    })
                    .ToList()
            }).ToList();

        return new PlannerDraftRequest
        {
            Leaders = activeLeaderInputs.Where(l => l.PreferredDestinations.Count > 0).ToList(),
            AllActiveLeaders = activeLeaderInputs,
            Journeys = journeys
                // 2. Filter hier direct op het meegegeven jaar!
                .Where(j => j.BookingStatus == BookingStatus.Bezig && j.Start.Year == year)
                .Select(j => new PlannerJourneyInput
                {
                    Id = j.Id,
                    Name = j.Name,
                    Start = j.Start,
                    End = j.End,
                    RequiredLeaders = j.RequiredLeaders
                })
                .ToList(),
        };
    }

    public PlannerDraftResult GenerateDraft(PlannerDraftRequest request)
    {
        var result = new PlannerDraftResult();
        var leaders = request.Leaders;
        var journeys = request.Journeys;

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
                    "Controleer of reisleiders deze reis hebben geselecteerd in hun voorkeuren.";
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

        // Constraint B: block leader if they have no preference for this journey
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < Js; j++)
            {
                bool available = leaders[l].PreferredDestinations.ContainsKey(solvable[j].Id);
                if (!available)
                    model.Add(x[l, j] == 0);
            }
        }

        // Constraint C: each leader can be assigned at most MaxTrips journeys
        for (int l = 0; l < L; l++)
        {
            var vars = Enumerable.Range(0, Js).Select(j => x[l, j]).ToList();
            model.Add(LinearExpr.Sum(vars) <= leaders[l].MaxTrips);
        }

        // Constraint D: a leader cannot have two overlapping journeys
        for (int l = 0; l < L; l++)
        {
            for (int j1 = 0; j1 < Js; j1++)
            {
                for (int j2 = j1 + 1; j2 < Js; j2++)
                {
                    var a = solvable[j1];
                    var b = solvable[j2];
                    if (a.Start < b.End && b.Start < a.End)
                        model.Add(x[l, j1] + x[l, j2] <= 1);
                }
            }
        }

        // Objective: minimize total preference cost (rank 1 = cheapest, no preference = 10)
        var obj = LinearExpr.NewBuilder();
        for (int l = 0; l < L; l++)
            for (int j = 0; j < Js; j++)
            {
                int cost = leaders[l].PreferredDestinations.TryGetValue(solvable[j].Id, out int rank)
                    ? (rank == 0 ? 5 : rank)
                    : 10;
                obj.AddTerm(x[l, j], cost);
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

using Google.OrTools.Sat;
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

    public async Task<PlannerDraftRequest> BuildRequestAsync(CancellationToken ct = default)
    {
        var leaders  = await _leaderService.GetTravelLeadersAsync(ct);
        var journeys = await _journeyService.GetJourneysAsync(ct);

        return new PlannerDraftRequest
        {
            Leaders = leaders
                .Where(l => l.IsActive && l.AvailabilityPeriods.Any())
                .Select(l => new PlannerLeaderInput
                {
                    Id = l.Id,
                    Name = l.Name,
                    AmountOfTrips = l.AmountOfTrips ?? 0,
                    MinTrips = l.MinTrips ?? 0,
                    MaxTrips = l.MaxTrips ?? 0,
                    AvailabilityPeriods = l.AvailabilityPeriods
                        .Select(a => (a.Start, a.End))
                        .ToList(),
                    PreferredDestinations = l.PreferredDestinations.ToDictionary(p => p.Destination, p => p.Rank)
                }).ToList(),
            Journeys = journeys
                .Select(j => new PlannerJourneyInput
                {
                    Id              = j.Id,
                    Name            = j.Name,
                    Start           = j.Start,
                    End             = j.End,
                    RequiredLeaders = j.RequiredLeaders   // ← carry through
                })
                .ToList(),
        };
    }

    public PlannerDraftResult GenerateDraft(PlannerDraftRequest request)
    {
        var result  = new PlannerDraftResult();
        var leaders = request.Leaders;
        var journeys = request.Journeys;

        int L = leaders.Count;
        int J = journeys.Count;

        if (L == 0)
        {
            result.IsSuccess  = false;
            result.ErrorMessage = "Geen reisleiders of geen actieve reisleiders";
            return result;
        }
        if (J == 0)
        {
            result.IsSuccess  = false;
            result.ErrorMessage = "Geen reizen gevonden";
            return result;
        }

        var model = new CpModel();

        //:: CREATES A 2D BOOLEAN ARRAY (LEADERS × JOURNEYS)
        var x = new BoolVar[L, J];
        for (int l = 0; l < L; l++)
            for (int j = 0; j < J; j++)
                x[l, j] = model.NewBoolVar($"x_{l}_{j}");

        // Constraint A: each journey must have exactly RequiredLeaders leaders assigned
        for (int j = 0; j < J; j++)
        {
            var vars = Enumerable.Range(0, L).Select(l => (ILiteral)x[l, j]).Cast<BoolVar>().ToList();
            model.Add(LinearExpr.Sum(vars) == journeys[j].RequiredLeaders);
        }

        // Constraint B: block leader if not available for the full journey dates
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < J; j++)
            {
                var leader  = leaders[l];
                var journey = journeys[j];
                bool available = leader.AvailabilityPeriods.Any(
                    p => p.Start <= journey.Start && p.End >= journey.End);
                if (!available)
                    model.Add(x[l, j] == 0);
            }
        }

        // Constraint C: each leader can be assigned at most MaxTrips journeys
        for (int l = 0; l < L; l++)
        {
            var vars = Enumerable.Range(0, J).Select(j => x[l, j]).ToList();
            model.Add(LinearExpr.Sum(vars) <= leaders[l].MaxTrips);
        }

        // Constraint D: a leader cannot have two overlapping journeys
        for (int l = 0; l < L; l++)
        {
            for (int j1 = 0; j1 < J; j1++)
            {
                for (int j2 = j1 + 1; j2 < J; j2++)
                {
                    var a = journeys[j1];
                    var b = journeys[j2];
                    bool overlaps = a.Start < b.End && b.Start < a.End;
                    if (overlaps)
                        model.Add(x[l, j1] + x[l, j2] <= 1);
                }
            }
        }

        // Objective: minimize total preference cost (rank 1 = cheapest, no preference = 10)
        var obj = LinearExpr.NewBuilder();
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < J; j++)
            {
                int cost = leaders[l].PreferredDestinations.TryGetValue(
                    journeys[j].Name, out int rank) ? rank : 10;
                obj.AddTerm(x[l, j], cost);
            }
        }

        // Fairness: penalise leaving an eligible leader unassigned
        const int UnassignedPenalty = 15;
        for (int l = 0; l < L; l++)
        {
            bool eligible = Enumerable.Range(0, J).Any(j =>
                leaders[l].AvailabilityPeriods.Any(
                    p => p.Start <= journeys[j].Start && p.End >= journeys[j].End));
            if (!eligible) continue;

            var assignedVar = model.NewBoolVar($"assigned_{l}");
            var rowVars     = Enumerable.Range(0, J).Select(j => x[l, j]).ToList();
            model.Add(LinearExpr.Sum(rowVars) >= assignedVar);
            obj.AddTerm(assignedVar, -UnassignedPenalty);
        }

        model.Minimize(obj);

        // Pre-solve diagnostic: check that each journey has enough available leaders
        var journeysWithoutEnoughLeaders = journeys
            .Where(j =>
            {
                int eligible = leaders.Count(l => l.AvailabilityPeriods.Any(
                    p => p.Start <= j.Start && p.End >= j.End));
                return eligible < j.RequiredLeaders;
            })
            .ToList();

        if (journeysWithoutEnoughLeaders.Any())
        {
            var names = string.Join(", ", journeysWithoutEnoughLeaders.Select(j =>
                $"{j.Name} ({j.Start:dd MMM}–{j.End:dd MMM yyyy}, vereist: {j.RequiredLeaders})"));
            result.IsSuccess    = false;
            result.ErrorMessage =
                $"Niet genoeg reisleiders beschikbaar voor: {names}. " +
                "Controleer beschikbaarheidsperiodes of verhoog het aantal actieve reisleiders.";
            return result;
        }

        var solver = new CpSolver();
        solver.StringParameters = "max_time_in_seconds:10";
        var status = solver.Solve(model);

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            result.IsSuccess = true;

            // Collect ALL assigned leaders per journey into a list
            for (int j = 0; j < J; j++)
            {
                var assignments = new List<JourneyAssignmentResult>();
                for (int l = 0; l < L; l++)
                {
                    if (solver.Value(x[l, j]) == 1L)
                    {
                        var leader  = leaders[l];
                        var journey = journeys[j];
                        int? rankMatched = leader.PreferredDestinations
                            .TryGetValue(journey.Name, out int r) ? r : null;

                        assignments.Add(new JourneyAssignmentResult
                        {
                            LeaderId    = leader.Id,
                            LeaderName  = leader.Name,
                            RankMatched = rankMatched
                        });
                    }
                }
                if (assignments.Count > 0)
                    result.JourneyAssignments[journeys[j].Id] = assignments;
            }
        }
        else
        {
            var availableSlots = leaders.Sum(l => l.MaxTrips);
            var totalRequired  = journeys.Sum(j => j.RequiredLeaders);
            result.IsSuccess    = false;
            result.ErrorMessage =
                $"De planning kon niet worden opgesteld. " +
                $"Er zijn {totalRequired} benodigde reisleider-slots maar slechts {availableSlots} beschikbare plaatsen. " +
                "Mogelijke oorzaken: te weinig reisleiders, te lage MaxTrips-instellingen, of te veel overlappende reizen.";
        }

        return result;
    }
}

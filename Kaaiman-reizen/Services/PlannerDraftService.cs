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
        var leaders = await _leaderService.GetTravelLeadersAsync(ct);
        var journeys = await _journeyService.GetJourneysAsync(ct);

        return new PlannerDraftRequest
        {
            Leaders = leaders
                .Where(l => l.IsActive && l.AvailabilityPeriods.Any())
                .Select(l => new PlannerLeaderInput
                {
                    Id = l.Id,
                    Name = l.Name,
                    MaxTrips = l.MaxTrips ?? 0,
                    AvailabilityPeriods = l.AvailabilityPeriods
                        .Select(a => (a.Start, a.End))
                        .ToList(),
                    PreferredDestinations = l.PreferredDestinations.ToDictionary(p => p.Destination, p => p.Rank)
                }).ToList(),
            Journeys = journeys
                .Select(j => new PlannerJourneyInput
                {
                    Id = j.Id,
                    Country = j.Country,
                    Start = DateOnly.FromDateTime(j.Start),
                    End = DateOnly.FromDateTime(j.End),
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

        //:: TODO: Make a button in so the planner can add new reisleiders or reizen.
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

        var model = new CpModel();

        //:: CREATES A 2D BOOLEAN ARRAY (LEADERS, TRAVELS)
        var x = new BoolVar[L, J];
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < J; j++)
            {
                x[l, j] = model.NewBoolVar($"x_{l}_{j}");
            }
        }

        // Constraint A: each journey must have exactly one leader
        for (int j = 0; j < J; j++)
        {
            var vars = Enumerable.Range(0, L).Select(l => (ILiteral)x[l, j]).ToList();
            model.AddExactlyOne(vars);
        }

        // Constraint B: constraint blocks if leader is not available
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < J; j++)
            {
                var leader = leaders[l];
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
            var vars = Enumerable.Range(0, J).Select(j => (ILiteral)x[l, j]).ToList();
            model.Add(LinearExpr.Sum(vars.Cast<BoolVar>()) <= leaders[l].MaxTrips);
        }

        // Constraint D: a leader cannot have two overlapping journeys at the same time.
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
                    {
                        model.Add(x[l, j1] + x[l, j2] <= 1);
                    }
                }
            }
        }

        // Constraint E: Minimize total preference cost
        // gives the highest points if no preference.
        // It creates a plan with the lowest points.
        var obj = LinearExpr.NewBuilder();
        for (int l = 0; l < L; l++)
        {
            for (int j = 0; j < J; j++)
            {
                int cost = leaders[l].PreferredDestinations.TryGetValue(
                    journeys[j].Country, out int rank) ? rank : 10;
                obj.AddTerm(x[l, j], cost);
            }
        }

        // Constraint F: Fairness — penalize leaving an eligible leader unassigned.
        // A leader is "eligible" if their availability covers at least one journey.
        // For each such leader we introduce a boolean "assigned[l]":
        //   - It can only be 1 when at least one journey is assigned (sum >= assigned[l])
        //   - The solver receives a reward of -UnassignedPenalty in the objective for setting it to 1
        // This makes the solver prefer distributing work over piling assignments on fewer leaders,
        // as long as the fairness gain (15) outweighs any preference-cost difference (max 10-1 = 9).
        const int UnassignedPenalty = 15;
        for (int l = 0; l < L; l++)
        {
            bool eligible = Enumerable.Range(0, J).Any(j =>
                leaders[l].AvailabilityPeriods.Any(
                    p => p.Start <= journeys[j].Start && p.End >= journeys[j].End));

            if (!eligible) continue;

            var assignedVar = model.NewBoolVar($"assigned_{l}");
            var rowVars = Enumerable.Range(0, J).Select(j => x[l, j]).ToList();

            // assignedVar can only be 1 if the leader has at least one journey assigned
            model.Add(LinearExpr.Sum(rowVars) >= assignedVar);

            // Reward the solver for assigning this leader (= penalize leaving them idle)
            obj.AddTerm(assignedVar, -UnassignedPenalty);
        }

        model.Minimize(obj);

        // Pre-solve diagnostic: find journeys with no available leader at all.
        // If any exist the solver will always return Infeasible, so we report it clearly.
        var journeysWithoutLeader = journeys
            .Where(j => !leaders.Any(l => l.AvailabilityPeriods.Any(
                p => p.Start <= j.Start && p.End >= j.End)))
            .ToList();

        if (journeysWithoutLeader.Any())
        {
            var names = string.Join(", ", journeysWithoutLeader.Select(j =>
                $"{j.Country} ({j.Start:dd MMM} – {j.End:dd MMM yyyy})"));
            result.IsSuccess = false;
            result.ErrorMessage =
                $"Geen enkele reisleider is beschikbaar voor de volgende {journeysWithoutLeader.Count} rei(s)zen: {names}. " +
                "Controleer of de beschikbaarheidsperiodes van de reisleiders overeenkomen met de reisdatums.";
            return result;
        }

        var solver = new CpSolver();
        solver.StringParameters = "max_time_in_seconds:10";
        var status = solver.Solve(model);

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            result.IsSuccess = true;
            for (int j = 0; j < J; j++)
            {
                for (int l = 0; l < L; l++)
                {
                    if (solver.Value(x[l, j]) == 1L)
                    {
                        var leader = leaders[l];
                        var journey = journeys[j];

                        int? rankMatched = leader.PreferredDestinations.TryGetValue(journey.Country, out int rank) ? rank : null;
                        result.JourneyAssignments[journey.Id] = new JourneyAssignmentResult
                        {
                            LeaderId = leader.Id,
                            LeaderName = leader.Name,
                            RankMatched = rankMatched
                        };
                    }
                }
            }
        }
        else
        {
            // Solver returned Infeasible/Unknown after passing the pre-check.
            // This means constraints conflict in a more complex way (e.g. not enough
            // leaders to cover all journeys given MaxTrips and overlap rules).
            var availableSlots = leaders.Sum(l => l.MaxTrips);
            result.IsSuccess = false;
            result.ErrorMessage =
                $"De planning kon niet worden opgesteld. " +
                $"Er zijn {J} reizen maar slechts {availableSlots} beschikbare plaatsen over alle reisleiders ({L}) samen. " +
                "Mogelijke oorzaken: te weinig reisleiders, te lage MaxTrips instellingen, of te veel overlappende reizen voor dezelfde leiders.";
        }

        return result;
    }
}

using Google.OrTools.Sat;
using Kaaiman_reizen.Models;

namespace Kaaiman_reizen.Services;

public class PlannerDraftService : IPlannerDraftService
{
    public PlannerDraftResult GenerateDraft(PlannerDraftRequest request)
    {
        var result = new PlannerDraftResult();
        var numLeaders = request.AvailableLeaders.Count;
        var numJourneys = request.JourneysToPlan.Count;

        // If there's nothing to plan, exit early
        if (numLeaders == 0 || numJourneys == 0)
        {
            result.ErrorMessage = "No leaders or journeys available for planning.";
            return result;
        }

        // 1. Initialize the Constraint Programming model
        CpModel model = new CpModel();

        // 2. Create Variables
        // x[l, j] is a boolean variable that is true (1) if Leader 'l' is assigned to Journey 'j'.
        BoolVar[,] x = new BoolVar[numLeaders, numJourneys];
        for (int l = 0; l < numLeaders; l++)
        {
            for (int j = 0; j < numJourneys; j++)
            {
                x[l, j] = model.NewBoolVar($"leader_{l}_journey_{j}");
            }
        }

        // 3. Define Constraints
        // Constraint A: Each journey MUST be assigned to exactly ONE leader.
        for (int j = 0; j < numJourneys; j++)
        {
            var journeyGroup = new List<ILiteral>();
            for (int l = 0; l < numLeaders; l++)
            {
                journeyGroup.Add(x[l, j]);
            }
            model.AddExactlyOne(journeyGroup); 
        }

        // Constraint B: Each leader can be assigned to AT MOST ONE journey (for this simple test).
        for (int l = 0; l < numLeaders; l++)
        {
            var leaderGroup = new List<ILiteral>();
            for (int j = 0; j < numJourneys; j++)
            {
                leaderGroup.Add(x[l, j]);
            }
            model.AddAtMostOne(leaderGroup);
        }

        // 4. Objective: Optimize for Leader Preferences
        LinearExprBuilder obj = LinearExpr.NewBuilder();
        for (int l = 0; l < numLeaders; l++)
        {
            for (int j = 0; j < numJourneys; j++)
            {
                var leader = request.AvailableLeaders[l];
                var journey = request.JourneysToPlan[j];
                
                // If a leader has a preference (1, 2, or 3), cost is the rank.
                // If they don't have a preference, assign a high cost (e.g., 10).
                // The solver will try to minimize the total sum of these costs!
                int cost = 10; 
                if (leader.PreferredDestinations.TryGetValue(journey.Country, out int rank))
                {
                    cost = rank;
                }
                
                obj.AddTerm(x[l, j], cost);
            }
        }
        model.Minimize(obj);

        // 5. Solve the model
        CpSolver solver = new CpSolver();
        
        // Optional: you can set a time limit so the solver won't hang on impossible problems
        solver.StringParameters = "max_time_in_seconds:5.0"; 
        
        CpSolverStatus status = solver.Solve(model);

        // 5. Interpret the Results
        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            result.IsSuccess = true;
            for (int j = 0; j < numJourneys; j++)
            {
                for (int l = 0; l < numLeaders; l++)
                {
                    // If the solver decided this boolean is true (1L), it's a match!
                    if (solver.Value(x[l, j]) == 1L) 
                    {
                        var leader = request.AvailableLeaders[l];
                        var journey = request.JourneysToPlan[j];
                        
                        int? rankMatched = null;
                        if (leader.PreferredDestinations.TryGetValue(journey.Country, out int rank))
                        {
                            rankMatched = rank;
                        }

                        if (rankMatched == 1) result.Rank1Matches++;
                        else if (rankMatched == 2) result.Rank2Matches++;
                        else if (rankMatched == 3) result.Rank3Matches++;
                        else result.NoPreferenceMatches++;

                        result.JourneyAssignments[journey.Id] = new JourneyAssignmentResult
                        {
                            LeaderId = leader.Id,
                            RankMatched = rankMatched
                        };
                    }
                }
            }
        }
        else
        {
            // It could be 'Infeasible' (e.g. 5 journeys but only 3 leaders available)
            result.IsSuccess = false; 
            result.ErrorMessage = $"Failed with OR-Tools Status: {status}. Possible reasons: Too many journeys for the available leaders, or constraints are impossible to satisfy.";
        }

        return result;
    }
}

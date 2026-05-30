using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CatchMeIfYouCan.Scripts.AI
{
    public class VirusAI
    {
        // The main entry point for Phase 5
        public void ExecuteSpreadVirus(GameState state)
        {
            OrganGraph graph = state.OrganGraph;
            int threshold = state.Settings.BfsSpreadThreshold;
            string preferred = state.MinimaxRecommendedTarget;

            Queue<string> queue = new Queue<string>();
            HashSet<string> visited = new HashSet<string>();

            // 1. Seed the BFS frontier with all currently infected zones
            foreach (var kvp in graph.Zones.Where(z => z.Value.IsInfected))
            {
                queue.Enqueue(kvp.Key);
                visited.Add(kvp.Key);
            }

            // 2. BFS loop
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                if (!graph.AdjacencyList.ContainsKey(current)) continue;

                // Get unvisited neighbors
                var unvisited = graph.AdjacencyList[current]
                    .Where(k => !visited.Contains(k))
                    .ToList();

                // Sort by defense count ascending.
                // Tiebreaker: if Minimax recommended this zone, float it to the top
                // (we subtract 0.5 from its effective sort key so it wins ties).
                var sorted = unvisited
                    .OrderBy(k =>
                    {
                        float defCount = graph.Zones[k].ActiveDefenseCount;
                        if (k == preferred) defCount -= 0.5f; // tiebreaker boost
                        return defCount;
                    })
                    .ToList();

                foreach (string target in sorted)
                {
                    OrganZone zone = graph.Zones[target];

                    // Only spread if defenses are below the difficulty threshold
                    if (zone.ActiveDefenseCount < threshold)
                    {
                        zone.IsInfected = true;
                        state.InfectionRate += 1;
                        state.SeverityIndex += 5;  // +5% per zone per proposal

                        // Brain infection is catastrophic — extra +25% per proposal
                        if (target == "Brain")
                        {
                            state.SeverityIndex += 25;
                            GD.Print($"[VIRUS] ⚠ Brain infected! Severity jumps by additional 25%!");
                        }

                        GD.Print($"[BFS] Spread to {zone.Name} | Severity: {state.SeverityIndex}%");
                        queue.Enqueue(target);
                    }

                    visited.Add(target);
                }
            }
        }

        // ===================================================================
        // PHASE 4 — Minimax: Pick the best initial spread target
        // ===================================================================
        public string CalculateBestMove(GameState state, int depth)
        {
            float bestScore = float.MinValue;
            string bestTargetKey = null;

            // Look at every infected zone and every uninfected neighbor
            foreach (var kvp in state.OrganGraph.Zones.Where(z => z.Value.IsInfected))
            {
                if (!state.OrganGraph.AdjacencyList.ContainsKey(kvp.Key)) continue;

                foreach (string target in state.OrganGraph.AdjacencyList[kvp.Key])
                {
                    if (state.OrganGraph.Zones[target].IsInfected) continue;

                    // Simulate this spread move on a cloned state
                    GameState sim = state.Clone();
                    sim.OrganGraph.Zones[target].IsInfected = true;
                    sim.SeverityIndex += 5;
                    if (target == "Brain") sim.SeverityIndex += 25;

                    // Evaluate: player responds next (isMaximizing = false)
                    float score = Minimax(sim, depth - 1, float.MinValue, float.MaxValue, false);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTargetKey = target;
                    }
                }
            }

            GD.Print($"[MINIMAX] Best move: {bestTargetKey ?? "none"} (score: {bestScore:F1})");
            return bestTargetKey;
        }

        // -------------------------------------------------------------------
        // Minimax with Alpha-Beta Pruning (recursive)
        // -------------------------------------------------------------------
        private float Minimax(GameState state, int depth, float alpha, float beta, bool isMaximizing)
        {
            // Base case: depth limit or terminal state
            if (depth == 0 || state.IsWinConditionMet() || state.IsLossConditionMet())
                return EvaluateBoard(state);

            if (isMaximizing)
            {
                // ---- VIRUS TURN: maximize severity ----
                float maxEval = float.MinValue;

                // Build the list of all possible virus spread moves
                var possibleMoves = GetVirusMoves(state);

                // If no moves are available, evaluate the current board as-is
                if (possibleMoves.Count == 0)
                    return EvaluateBoard(state);

                foreach (var (fromZone, targetZone) in possibleMoves)
                {
                    GameState sim = state.Clone();
                    sim.OrganGraph.Zones[targetZone].IsInfected = true;
                    sim.SeverityIndex += 5;
                    if (targetZone == "Brain") sim.SeverityIndex += 25;

                    float eval = Minimax(sim, depth - 1, alpha, beta, false);
                    maxEval = Math.Max(maxEval, eval);

                    // Alpha-Beta: update alpha and prune
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha) break; // Beta cutoff — prune remaining branches
                }

                return maxEval;
            }
            else
            {
                // ---- PLAYER TURN: minimize severity ----
                float minEval = float.MaxValue;

                // Simulate the player's best defensive response:
                // The player adds 2 defenses to the most vulnerable infected-adjacent zone.
                // (More realistic than the original's "+1 to first zone".)
                var playerMoves = GetPlayerDefenseMoves(state);

                if (playerMoves.Count == 0)
                    return EvaluateBoard(state);

                foreach (string defendTarget in playerMoves)
                {
                    GameState sim = state.Clone();
                    sim.OrganGraph.Zones[defendTarget].ActiveDefenseCount += 2;
                    sim.PlayerEp -= 10; // WBC cost per proposal

                    float eval = Minimax(sim, depth - 1, alpha, beta, true);
                    minEval = Math.Min(minEval, eval);

                    // Alpha-Beta: update beta and prune
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha) break; // Alpha cutoff — prune remaining branches
                }

                return minEval;
            }
        }

        // -------------------------------------------------------------------
        // Helper: enumerate all zones the virus can spread to this turn
        // -------------------------------------------------------------------
        private List<(string from, string to)> GetVirusMoves(GameState state)
        {
            var moves = new List<(string, string)>();

            foreach (var kvp in state.OrganGraph.Zones.Where(z => z.Value.IsInfected))
            {
                if (!state.OrganGraph.AdjacencyList.ContainsKey(kvp.Key)) continue;

                foreach (string neighbor in state.OrganGraph.AdjacencyList[kvp.Key])
                {
                    if (!state.OrganGraph.Zones[neighbor].IsInfected)
                        moves.Add((kvp.Key, neighbor));
                }
            }

            return moves;
        }

        // -------------------------------------------------------------------
        // Helper: enumerate zones the player would most likely defend
        // Strategy: the player defends uninfected zones adjacent to infection
        // that have the fewest current defenses (most vulnerable).
        // -------------------------------------------------------------------
        private List<string> GetPlayerDefenseMoves(GameState state)
        {
            var candidates = new HashSet<string>();

            foreach (var kvp in state.OrganGraph.Zones.Where(z => z.Value.IsInfected))
            {
                if (!state.OrganGraph.AdjacencyList.ContainsKey(kvp.Key)) continue;

                foreach (string neighbor in state.OrganGraph.AdjacencyList[kvp.Key])
                {
                    if (!state.OrganGraph.Zones[neighbor].IsInfected)
                        candidates.Add(neighbor);
                }
            }

            // Return up to 3 most vulnerable candidates (fewest defenses first)
            return candidates
                .OrderBy(k => state.OrganGraph.Zones[k].ActiveDefenseCount)
                .Take(3)
                .ToList();
        }

        // -------------------------------------------------------------------
        // Board evaluation heuristic
        // Higher score = better for the virus.
        // Components:
        //   + SeverityIndex (direct progress toward 100% game over)
        //   + 3 × number of infected zones (controlling more zones = strategic advantage)
        //   + 2 × number of stacked mutations (harder to counter = good for virus)
        //   - 0.5 × PlayerEP (a well-resourced player is dangerous)
        // -------------------------------------------------------------------
        private float EvaluateBoard(GameState state)
        {
            int infectedCount = state.OrganGraph.Zones.Values.Count(z => z.IsInfected);
            int mutationStacks = state.ActiveMutationStacks.Values.Sum();

            return state.SeverityIndex
                 + (3f * infectedCount)
                 + (2f * mutationStacks)
                 - (0.5f * state.PlayerEp);
        }
    }
}

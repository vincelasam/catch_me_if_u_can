using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace CatchMeIfYouCan.Scripts.AI
{
    public class VirusAI
    {
        // The main entry point for Phase 5
        public void ExecuteSpreadVirus(GameState currentState)
        {
            OrganGraph graph = currentState.OrganGraph;
            Queue<string> queue = new Queue<string>();
            HashSet<string> visited = new HashSet<string>();

            // 1. Initialize the BFS frontier
            // Find all currently infected zones and add their string keys to the queue
            foreach (var kvp in graph.Zones)
            {
                if (kvp.Value.IsInfected)
                {
                    queue.Enqueue(kvp.Key);
                    visited.Add(kvp.Key);
                }
            }

            // 2. The Core BFS Loop
            while (queue.Count > 0)
            {
                string currentZoneKey = queue.Dequeue();

                // Safety check: Does this zone have connections?
                if (!graph.AdjacencyList.ContainsKey(currentZoneKey)) continue;

                // Get the string names of connected neighbors
                List<string> neighborKeys = graph.AdjacencyList[currentZoneKey];

                // Filter out neighbors we have already visited or infected this pass
                List<string> unvisitedNeighborKeys = neighborKeys.Where(k => !visited.Contains(k)).ToList();

                // 3. Sort neighbors by path of least resistance (Fewest Active Defenses)
                // We use LINQ to look up the actual OrganZone object in the Zones dictionary to get its defense count
                var sortedNeighborKeys = unvisitedNeighborKeys
                    .OrderBy(k => graph.Zones[k].ActiveDefenseCount)
                    .ToList();

                // 4. Evaluate spread logic
                foreach (string targetKey in sortedNeighborKeys)
                {
                    OrganZone targetZone = graph.Zones[targetKey];

                    // Example spread rule: Only spread if defenses are lower than 2 (tie this to Difficulty later)
                    if (targetZone.ActiveDefenseCount < 2)
                    {
                        targetZone.IsInfected = true;
                        currentState.SeverityIndex += 5; // Severity Index rises 5%
                        currentState.InfectionRate += 1; // Increase infection tracker

                        GD.Print($"[VIRUS] Spread to {targetZone.Name}! Severity now {currentState.SeverityIndex}%.");

                        queue.Enqueue(targetKey);
                    }

                    visited.Add(targetKey);
                }
            }
        }

        public string CalculateBestMove(GameState currentState, int depth)
        {
            float bestScore = float.MinValue;
            string bestTargetKey = null;

            // The virus looks at all current infected zones to see where it can spread
            foreach (var kvp in currentState.OrganGraph.Zones.Where(z => z.Value.IsInfected))
            {
                string currentZone = kvp.Key;

                // Look at neighbors
                foreach (string target in currentState.OrganGraph.AdjacencyList[currentZone])
                {
                    if (!currentState.OrganGraph.Zones[target].IsInfected)
                    {
                        // 1. CLONE THE STATE (Sandbox mode)
                        GameState simulatedState = currentState.Clone();

                        // 2. SIMULATE THE VIRUS MOVE
                        simulatedState.OrganGraph.Zones[target].IsInfected = true;
                        simulatedState.SeverityIndex += 5;

                        // 3. RUN MINIMAX TO SEE HOW THE PLAYER REACTS
                        // alpha = negative infinity, beta = positive infinity, isMaximizing = false (Player's turn next)
                        float moveScore = Minimax(simulatedState, depth - 1, float.MinValue, float.MaxValue, false);

                        // 4. KEEP THE BEST SCORE
                        if (moveScore > bestScore)
                        {
                            bestScore = moveScore;
                            bestTargetKey = target;
                        }
                    }
                }
            }

            return bestTargetKey;
        }
        private float Minimax(GameState state, int depth, float alpha, float beta, bool isMaximizingPlayer)
        {
            // Base Case: We looked far enough into the future, or the game is over
            if (depth == 0 || state.IsWinConditionMet() || state.IsLossConditionMet())
            {
                return EvaluateBoard(state);
            }

            if (isMaximizingPlayer) // --- VIRUS TURN ---
            {
                float maxEval = float.MinValue;
                // In a full game, you loop through all possible virus mutations/spreads here

                // Alpha-Beta Pruning
                maxEval = Math.Max(maxEval, EvaluateBoard(state)); // Placeholder eval
                alpha = Math.Max(alpha, EvaluateBoard(state));
                if (beta <= alpha) return maxEval; // Prune!

                return maxEval;
            }
            else // --- PLAYER TURN (IMMUNE SYSTEM) ---
            {
                float minEval = float.MaxValue;

                // DUMMY PLAYER LOGIC: We pretend the player always adds +1 defense to the most vulnerable organ
                GameState playerSimulatedState = state.Clone();
                string weakestOrgan = playerSimulatedState.OrganGraph.Zones.Keys.First();
                playerSimulatedState.OrganGraph.Zones[weakestOrgan].ActiveDefenseCount += 1; // Player defends!

                // Pass the turn back to the Virus
                float eval = Minimax(playerSimulatedState, depth - 1, alpha, beta, true);

                minEval = Math.Min(minEval, eval);

                // Alpha-Beta Pruning
                beta = Math.Min(beta, eval);
                if (beta <= alpha) return minEval; // Prune!

                return minEval;
            }
        }

        // The Heuristic: How the AI decides if a board state is "good" or "bad"
        private float EvaluateBoard(GameState state)
        {
            // The Virus wants high severity. 
            // We also subtract player EP because a rich player is dangerous to the virus.
            return state.SeverityIndex - (state.PlayerEp * 0.5f);
        }


    }
}

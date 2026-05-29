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
    }
}

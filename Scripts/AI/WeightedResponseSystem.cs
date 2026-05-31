using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CatchMeIfYouCan.Scripts.AI
{
    public class WeightedResponseSystem
    {
        // -----------------------------------------------------------------------
        // Base effectiveness scores per defense type.
        // These reflect the proposal's EP cost table — more expensive = stronger.
        // WBC (10 EP) = 1.0, Antibodies (25 EP) = 2.5, etc.
        // -----------------------------------------------------------------------
        private static readonly Dictionary<string, float> BaseEffectiveness = new()
        {
            { "WhiteBloodCells",  1.0f },  // 10 EP — broad but cheap
            { "Antibodies",       2.5f },  // 25 EP — targeted, effective
            { "Inflammation",     2.0f },  // 20 EP — quarantine effect
            { "FeverResponse",    3.5f },  // 35 EP — global slowdown, rare
            { "MemoryCells",      1.5f },  // 15 EP — counter known mutations
            { "CytokineBurst",    5.0f }   // 50 EP — nuclear option
        };

        // -----------------------------------------------------------------------
        // Context multipliers: if a mutation is active, certain defenses are
        // MORE effective against it (and should be scored higher).
        // Key = mutation name, Value = dict of defense → multiplier
        // -----------------------------------------------------------------------
        private static readonly Dictionary<string, Dictionary<string, float>> ContextMultipliers = new()
        {
            {
                "Evade_Phagocytosis_Mutation", new Dictionary<string, float>
                {
                    { "Antibodies",  1.5f },  // antibodies bypass phagocytosis evasion
                    { "Inflammation", 1.3f }
                }
            },
            {
                "Antigenic_Drift_Mutation", new Dictionary<string, float>
                {
                    { "MemoryCells",  2.0f },  // memory cells specifically counter drift
                    { "Antibodies",   0.6f }   // antibodies less effective — antigen changed
                }
            },
            {
                "Antigenic_Shift_Mutation", new Dictionary<string, float>
                {
                    { "MemoryCells",  0.5f },  // memory cells nearly useless — totally new antigen
                    { "CytokineBurst", 1.4f }
                }
            },
            {
                "Thermal_Resistance_Mutation", new Dictionary<string, float>
                {
                    { "FeverResponse", 0.3f }, // thermal resistance makes fever nearly useless
                    { "WhiteBloodCells", 1.2f }
                }
            },
            {
                "Heat_Shock_Proteins_Mutation", new Dictionary<string, float>
                {
                    { "FeverResponse",  0.2f }, // heat shock proteins neutralise fever
                    { "Antibodies",     1.3f }
                }
            }
        };

        /// <summary>
        /// Evaluates all player defenses and writes weighted scores to
        /// state.LastUsedDefenses. Also returns the name of the dominant
        /// defense type (highest score) for logging.
        /// </summary>
        public string EvaluateAndScore(GameState state)
        {
            var scores = new Dictionary<string, float>();

            foreach (var kvp in state.PlayerDefenses)
            {
                string defense = kvp.Key;
                int units = kvp.Value;

                if (units <= 0) continue;
                if (!BaseEffectiveness.ContainsKey(defense)) continue;

                // Start with base score
                float score = units * BaseEffectiveness[defense];

                // Apply context multipliers for each active mutation
                foreach (string mutation in state.ActiveMutations)
                {
                    if (ContextMultipliers.ContainsKey(mutation) &&
                        ContextMultipliers[mutation].ContainsKey(defense))
                    {
                        score *= ContextMultipliers[mutation][defense];
                    }
                }

                // Stack penalty: if this was already the dominant defense last round,
                // score it slightly lower (the virus already started adapting)
                if (state.LastUsedDefenses.Count > 0)
                {
                    string lastDominant = state.LastUsedDefenses
                        .OrderByDescending(d => d.Value)
                        .FirstOrDefault().Key;

                    if (defense == lastDominant)
                        score *= 0.85f; // 15% penalty for repeated strategy
                }

                scores[defense] = score;
            }

            // Write results back to state for the Decision Tree to read
            state.LastUsedDefenses = scores;

            // Return the dominant defense name (or null if no defenses deployed)
            string dominant = scores.Count > 0
                ? scores.OrderByDescending(d => d.Value).First().Key
                : null;

            if (dominant != null)
                GD.Print($"[WRS] Dominant defense: {dominant} (score: {scores[dominant]:F2})");
            else
                GD.Print("[WRS] No defenses deployed this round.");

            return dominant;
        }
    }
}
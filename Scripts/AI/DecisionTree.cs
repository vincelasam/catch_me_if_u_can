using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CatchMeIfYouCan.Scripts.Core;

namespace CatchMeIfYouCan.Scripts.AI
{
    public class DecisionTree
    {
        // -----------------------------------------------------------------------
        // All defined mutation names (must match WRS context multiplier keys)
        // -----------------------------------------------------------------------
        public const string Evade_Phagocytosis = "Evade_Phagocytosis_Mutation";
        public const string Antigenic_Drift = "Antigenic_Drift_Mutation";
        public const string Antigenic_Shift = "Antigenic_Shift_Mutation";
        public const string Thermal_Resistance = "Thermal_Resistance_Mutation";
        public const string Heat_Shock_Proteins = "Heat_Shock_Proteins_Mutation";
        public const string Accelerated_Replication = "Accelerated_Replication_Mutation";
        public const string Membrane_Hardening = "Membrane_Hardening_Mutation"; // counters Inflammation
        public const string Cytokine_Suppressor = "Cytokine_Suppressor_Mutation"; // counters CytokineBurst

        public string SelectAndApplyMutation(GameState state)
        {
            var settings = state.Settings;

            // Check mutation interval: Casual mutates every 2 rounds, others every round
            if (state.RoundNumber % settings.MutationIntervalRounds != 0)
            {
                GD.Print($"[DT] No mutation this round (interval: every {settings.MutationIntervalRounds} rounds)");
                return null;
            }

            // Get the dominant defense from WRS output
            string dominantDefense = state.LastUsedDefenses.Count > 0
                ? state.LastUsedDefenses.OrderByDescending(d => d.Value).First().Key
                : null;

            // Traverse the decision tree
            string chosenMutation = Traverse(dominantDefense, state, state.Difficulty);

            if (chosenMutation == null)
            {
                GD.Print("[DT] No mutation selected.");
                return null;
            }

            // Apply the mutation to state
            ApplyMutation(chosenMutation, state);

            // Telegram in Casual mode
            if (settings.TelegraphMutations)
                GD.Print($"[DT] ⚠ TELEGRAPH: Next round the virus will use {chosenMutation}");
            else
                GD.Print($"[DT] Mutation applied: {chosenMutation}");

            return chosenMutation;
        }

        // -----------------------------------------------------------------------
        // Decision tree traversal — mirrors the proposal pseudocode exactly,
        // with compound mutation logic added for Epidemic/Pandemic.
        // -----------------------------------------------------------------------
        private string Traverse(string dominantDefense, GameState state, DifficultyMode difficulty)
        {
            bool canCompound = difficulty >= DifficultyMode.Epidemic;
            bool canDoubleCompound = difficulty == DifficultyMode.Pandemic;

            if (dominantDefense == null)
            {
                // Default: no defenses deployed — virus accelerates replication
                return Accelerated_Replication;
            }

            switch (dominantDefense)
            {
                // -------------------------------------------------------
                // Player is spamming White Blood Cells → evade phagocytosis
                // -------------------------------------------------------
                case "WhiteBloodCells":
                    return Evade_Phagocytosis;

                // -------------------------------------------------------
                // Player is using Antibodies → mutate antigens
                // If Antigenic_Drift is already active AND difficulty allows
                // compounding → upgrade to Antigenic_Shift
                // -------------------------------------------------------
                case "Antibodies":
                    if (canCompound && state.ActiveMutations.Contains(Antigenic_Drift))
                        return Antigenic_Shift;  // compound mutation
                    return Antigenic_Drift;

                // -------------------------------------------------------
                // Player is using Inflammation → develop membrane hardening
                // -------------------------------------------------------
                case "Inflammation":
                    return Membrane_Hardening;

                // -------------------------------------------------------
                // Player is using Fever Response → heat shock proteins
                // Also applies Thermal_Resistance first if not yet active
                // -------------------------------------------------------
                case "FeverResponse":
                    if (canCompound && state.ActiveMutations.Contains(Thermal_Resistance))
                        return Heat_Shock_Proteins;
                    return Thermal_Resistance;

                // -------------------------------------------------------
                // Player used Memory Cells → shift antigens to invalidate memory
                // -------------------------------------------------------
                case "MemoryCells":
                    if (canCompound && state.ActiveMutations.Contains(Antigenic_Drift))
                        return Antigenic_Shift;
                    return Antigenic_Drift;

                // -------------------------------------------------------
                // Player used Cytokine Burst → evolve cytokine suppression
                // Pandemic can double-compound: suppressor + accelerated replication
                // -------------------------------------------------------
                case "CytokineBurst":
                    if (canDoubleCompound && state.ActiveMutations.Contains(Cytokine_Suppressor))
                        return Accelerated_Replication; // double compound
                    return Cytokine_Suppressor;

                default:
                    return Accelerated_Replication;
            }
        }

        // -----------------------------------------------------------------------
        // Apply a mutation to the game state
        // -----------------------------------------------------------------------
        private void ApplyMutation(string mutation, GameState state)
        {
            // Add to active list if not already present
            if (!state.ActiveMutations.Contains(mutation))
                state.ActiveMutations.Add(mutation);

            // Increment stack count
            if (!state.ActiveMutationStacks.ContainsKey(mutation))
                state.ActiveMutationStacks[mutation] = 0;
            state.ActiveMutationStacks[mutation]++;

            // Apply the mutation's mechanical effect
            switch (mutation)
            {
                case Accelerated_Replication:
                    // +1 to InfectionRate each time this mutation fires
                    state.InfectionRate += 1;
                    GD.Print($"[DT] Accelerated Replication → InfectionRate: {state.InfectionRate}");
                    break;

                case Antigenic_Drift:
                    // Antibodies become 40% less effective (tracked via WRS context multipliers)
                    // No direct state change needed — WRS handles this via ContextMultipliers
                    GD.Print("[DT] Antigenic Drift active — Antibodies weakened next round.");
                    break;

                case Antigenic_Shift:
                    // Memory Cells are nearly useless now
                    GD.Print("[DT] Antigenic Shift active — Memory Cells nearly useless.");
                    break;

                case Thermal_Resistance:
                    GD.Print("[DT] Thermal Resistance active — Fever Response weakened.");
                    break;

                case Heat_Shock_Proteins:
                    GD.Print("[DT] Heat Shock Proteins active — Fever Response nearly useless.");
                    break;

                case Evade_Phagocytosis:
                    GD.Print("[DT] Evading Phagocytosis — WBC effectiveness reduced.");
                    break;

                case Membrane_Hardening:
                    // Reduce effectiveness of Inflammation by adding pseudo-defense
                    // We model this as +1 to all infected zones' defense against isolation
                    GD.Print("[DT] Membrane Hardening active — Inflammation less effective.");
                    break;

                case Cytokine_Suppressor:
                    GD.Print("[DT] Cytokine Suppressor active — CytokineBurst effect halved.");
                    break;
            }

            // Uncontested mutation: +3% severity per proposal
            // (This fires every round the mutation is active and uncontested —
            //  tracked in RoundManager.ResolveRound() in Commit 5)
        }

        /// <summary>
        /// Called by RoundManager.ResolveRound() to apply the +3% severity
        /// penalty for each mutation that went uncontested this round.
        /// A mutation is "uncontested" if the player did not deploy the
        /// defense that directly counters it.
        /// </summary>
        public int CalculateUncontestedSeverity(GameState state)
        {
            int penalty = 0;

            // Map of mutation → the defense that counters it
            var counters = new Dictionary<string, string>
            {
                { Evade_Phagocytosis,     "Antibodies"       },
                { Antigenic_Drift,        "MemoryCells"       },
                { Antigenic_Shift,        "CytokineBurst"    },
                { Thermal_Resistance,     "FeverResponse"    },
                { Heat_Shock_Proteins,    "FeverResponse"    },
                { Membrane_Hardening,     "CytokineBurst"    },
                { Cytokine_Suppressor,    "MemoryCells"      },
                { Accelerated_Replication,"WhiteBloodCells"  }
            };

            foreach (string mutation in state.ActiveMutations)
            {
                if (!counters.ContainsKey(mutation)) continue;

                string counter = counters[mutation];

                // Check if the player deployed the counter this round
                bool contested = state.PlayerDefenses.ContainsKey(counter) &&
                                 state.PlayerDefenses[counter] > 0;

                if (!contested)
                {
                    penalty += 3; // +3% per uncontested mutation per proposal
                    GD.Print($"[DT] {mutation} uncontested → +3% severity");
                }
            }

            return penalty;
        }
    }
}
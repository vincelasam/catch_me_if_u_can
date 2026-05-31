using System;
using System.Linq;
using Godot;

namespace CatchMeIfYouCan.Scripts.AI
{
    public static class PlayerActionResolver
    {
        /// <summary>
        /// EP cost table — matches the proposal's balance sheet.
        /// </summary>
        public static int GetCost(string actionName) => actionName switch
        {
            "WhiteBloodCells" => 10,
            "Antibodies" => 25,
            "Inflammation" => 20,
            "FeverResponse" => 35,
            "MemoryCells" => 15,
            "CytokineBurst" => 50,
            _ => 0
        };

        /// <summary>
        /// Validates EP, deducts cost, tracks defense in PlayerDefenses,
        /// and applies the mechanical effect to the target zone.
        /// Returns true if the action was applied, false if EP was insufficient.
        /// </summary>
        public static bool Apply(GameState state, string actionName, string targetZone)
        {
            int cost = GetCost(actionName);

            if (state.PlayerEp < cost)
            {
                GD.Print($"[PLAYER] Not enough EP for {actionName} " +
                         $"(need {cost}, have {state.PlayerEp})");
                return false;
            }

            state.PlayerEp -= cost;

            // Track for WRS scoring
            if (!state.PlayerDefenses.ContainsKey(actionName))
                state.PlayerDefenses[actionName] = 0;
            state.PlayerDefenses[actionName]++;

            if (!state.OrganGraph.Zones.ContainsKey(targetZone))
            {
                GD.PrintErr($"[PLAYER] Unknown target zone: {targetZone}");
                return false;
            }

            var zone = state.OrganGraph.Zones[targetZone];

            switch (actionName)
            {
                case "WhiteBloodCells":
                    zone.ActiveDefenseCount += 1;
                    GD.Print($"[PLAYER] WBC → {targetZone} (+1 defense) | EP: {state.PlayerEp}");
                    break;

                case "Antibodies":
                    zone.ActiveDefenseCount += 2;
                    GD.Print($"[PLAYER] Antibodies → {targetZone} (+2 defense) | EP: {state.PlayerEp}");
                    break;

                case "Inflammation":
                    zone.ActiveDefenseCount += 3;
                    state.SeverityIndex += 1; // slight host damage
                    GD.Print($"[PLAYER] Inflammation → {targetZone} (+3 defense, +1% severity) | EP: {state.PlayerEp}");
                    break;

                case "FeverResponse":
                    // Raises defense on all currently infected zones
                    foreach (var z in state.OrganGraph.Zones.Values.Where(z => z.IsInfected))
                        z.ActiveDefenseCount += 1;
                    GD.Print($"[PLAYER] Fever Response — all infected zones +1 defense | EP: {state.PlayerEp}");
                    break;

                case "MemoryCells":
                    zone.ActiveDefenseCount += 2;
                    GD.Print($"[PLAYER] Memory Cells → {targetZone} (+2 defense) | EP: {state.PlayerEp}");
                    break;

                case "CytokineBurst":
                    foreach (var z in state.OrganGraph.Zones.Values.Where(z => z.IsInfected))
                        z.ActiveDefenseCount += 5;
                    state.SeverityIndex += 10;
                    state.InfectionRate = Math.Max(0, state.InfectionRate - 2);
                    GD.Print($"[PLAYER] CYTOKINE BURST — all infected +5 defense, " +
                             $"-2 infection, +10% severity | EP: {state.PlayerEp}");
                    break;
            }

            return true;
        }
    }
}
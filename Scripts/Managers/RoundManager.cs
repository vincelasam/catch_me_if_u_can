using CatchMeIfYouCan.Scripts.AI;
using CatchMeIfYouCan.Scripts.Core;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class RoundManager : Node
{
    // -----------------------------------------------------------------------
    // Public state — UI nodes and other systems read from here
    // -----------------------------------------------------------------------
    public GameState CurrentState { get; private set; }

    public DifficultyMode SelectedDifficulty { get; set; } = DifficultyMode.Casual;

    // -----------------------------------------------------------------------
    // Internal tracking
    // -----------------------------------------------------------------------
    private VirusAI _virusAI;
    private VirusRL _virusRL;
    private DecisionTree _decisionTree;
    private WeightedResponseSystem _wrs;
    private Random _rng = new Random();

    // -----------------------------------------------------------------------
    // Godot lifecycle
    // -----------------------------------------------------------------------
    public override void _Ready()
    {
        // Phase 1: Initialize everything
        InitializeGame();

        // Kick off the first round
        ExecuteRoundSequence();
    }

    // -----------------------------------------------------------------------
    // Main round loop — called at the start of each new round
    // -----------------------------------------------------------------------
    public void ExecuteRoundSequence()
    {
        GD.Print($"\n========== ROUND {CurrentState.RoundNumber} | {CurrentState.Difficulty} ==========");

        // Phase 2: EP regeneration based on healthy organs
        RegenerateEP();

        // Phase 3 & 4: RL picks a strategy target, Minimax validates it
        DetermineVirusStrategy();

        // Phase 5: BFS executes the actual spatial spread
        ExecuteBFSVirusSpread();

        // Phase 6: Decision Tree selects a counter-mutation
        ExecuteVirusMutation();

        // Phase 7: Win condition check (infection rate = 0 → player wins)
        if (CurrentState.IsWinConditionMet())
        {
            GD.Print("✓ PLAYER WINS — Virus fully eradicated!");
            return;
        }

        // Phase 8: Loss condition check (severity >= 100 → game over)
        if (CurrentState.IsLossConditionMet())
        {
            GD.Print("✗ GAME OVER — Severity reached 100%.");
            return;
        }

        // Phase 9: WRS scores player defenses (feeds into next round's Decision Tree)
        EvaluatePlayerDefenses();

        // Phase 10: Wait for player to choose and confirm an action
        WaitForPlayerAction();

        // Phase 11 is triggered by OnPlayerActionConfirmed() after player commits
    }

    public void OnPlayerActionConfirmed()
    {
        // Phase 11a: Apply all pending damage/healing/costs
        ResolveRound();

        // Phase 11b: RL updates its Q-table based on this round's outcome
        UpdateRLQTable();

        // Advance round counter and loop
        CurrentState.RoundNumber++;
        ExecuteRoundSequence();
    }

    // =========================================================================
    // PHASE 1 — Game Initialization
    // =========================================================================
    private void InitializeGame()
    {
        CurrentState = new GameState
        {
            Difficulty = SelectedDifficulty,
            PlayerEp = 50,   // starting EP — enough for a couple of actions
            InfectionRate = 1,
            SeverityIndex = 0,
            RoundNumber = 1
        };

        _virusAI = new VirusAI();
        _virusRL = new VirusRL(SelectedDifficulty);
        _decisionTree = new DecisionTree();
        _wrs = new WeightedResponseSystem();

        BuildOrganGraph();
        SetStartingInfection();

        GD.Print($"[INIT] Game initialized — Difficulty: {CurrentState.Difficulty}");
        GD.Print($"[INIT] Minimax depth: {CurrentState.Settings.MinimaxDepth} | " +
                 $"BFS threshold: {CurrentState.Settings.BfsSpreadThreshold} | " +
                 $"EP/organ: {CurrentState.Settings.EpPerHealthyOrgan}");
    }


    private void BuildOrganGraph()
    {
        var g = CurrentState.OrganGraph;

        // --- Add all 6 zones ---
        // OrganZone(name, epGeneration)
        g.Zones["Lungs"] = new OrganZone("Lungs", 5);   // entry point
        g.Zones["Bloodstream"] = new OrganZone("Bloodstream", 10);  // virus highway
        g.Zones["LymphNodes"] = new OrganZone("LymphNodes", 15);  // main EP source
        g.Zones["Gut"] = new OrganZone("Gut", 8);
        g.Zones["Heart"] = new OrganZone("Heart", 6);
        g.Zones["Brain"] = new OrganZone("Brain", 3);   // critical — +25% severity if infected

        // --- Wire the anatomical connections ---
        // Lungs ↔ Bloodstream (oxygen enters blood here)
        g.AddConnection("Lungs", "Bloodstream");
        // Lungs ↔ LymphNodes (lymphatic drainage from lungs)
        g.AddConnection("Lungs", "LymphNodes");
        // Bloodstream ↔ Heart (heart pumps the blood)
        g.AddConnection("Bloodstream", "Heart");
        // Bloodstream ↔ LymphNodes (lymph returns to bloodstream via thoracic duct)
        g.AddConnection("Bloodstream", "LymphNodes");
        // Bloodstream ↔ Gut (portal circulation — gut absorbs nutrients into blood)
        g.AddConnection("Bloodstream", "Gut");
        // Heart ↔ Brain (carotid arteries supply brain)
        g.AddConnection("Heart", "Brain");

        GD.Print("[INIT] Organ graph built: Lungs→Bloodstream→Heart→Brain | " +
                 "Bloodstream→LymphNodes | Bloodstream→Gut | Lungs→LymphNodes");
    }


    private void SetStartingInfection()
    {
        var settings = CurrentState.Settings;
        var zones = CurrentState.OrganGraph.Zones;

        if (settings.StartingInfectionZones == 1)
        {
            // Always start in Lungs
            zones["Lungs"].IsInfected = true;
            GD.Print("[INIT] Infection started in: Lungs");
        }
        else
        {
            // Pandemic: pick 3 random zones
            var allKeys = zones.Keys.ToList();
            var shuffled = allKeys.OrderBy(_ => _rng.Next()).Take(settings.StartingInfectionZones).ToList();
            foreach (var key in shuffled)
            {
                zones[key].IsInfected = true;
                CurrentState.InfectionRate++;
            }
            // Adjust starting infection rate (we initialised it to 1 above)
            CurrentState.InfectionRate = shuffled.Count;
            GD.Print($"[INIT] Pandemic — Infection started in: {string.Join(", ", shuffled)}");
        }
    }

    // =========================================================================
    // PHASE 2 — EP Regeneration
    // =========================================================================
    private void RegenerateEP()
    {
        int healthyCount = CurrentState.OrganGraph.Zones.Values
            .Count(z => !z.IsInfected);

        int epGained = healthyCount * CurrentState.Settings.EpPerHealthyOrgan;
        CurrentState.PlayerEp += epGained;

        GD.Print($"[EP] {healthyCount} healthy organs × {CurrentState.Settings.EpPerHealthyOrgan} " +
                 $"= +{epGained} EP | Total: {CurrentState.PlayerEp} EP");
    }

    // =========================================================================
    // PHASE 3 & 4 — RL Strategy Selection + Minimax Validation
    // =========================================================================
    private void DetermineVirusStrategy()
    {
        GD.Print($"[AI] Phase 3 & 4 — RL + Minimax (depth: {CurrentState.Settings.MinimaxDepth})");

        // Phase 4 first: Minimax calculates the strategically best target
        // (RL uses this as a fallback / comparison point)
        string minimaxTarget = _virusAI.CalculateBestMove(
            CurrentState,
            depth: CurrentState.Settings.MinimaxDepth
        );

        // Phase 3: RL selects its action using epsilon-greedy
        // It compares against the Minimax suggestion and may override it
        string rlTarget = _virusRL.SelectAction(CurrentState, minimaxTarget);

        // The final recommended target is the RL choice
        // (RL logs when it agrees with or overrides Minimax)
        CurrentState.MinimaxRecommendedTarget = rlTarget;
    }

    // =========================================================================
    // PHASE 5 — BFS Virus Spread
    // =========================================================================
    private void ExecuteBFSVirusSpread()
    {
        GD.Print("[AI] Phase 5 — BFS Spread");
        _virusAI.ExecuteSpreadVirus(CurrentState);
    }

    // =========================================================================
    // PHASE 6 — Decision Tree Mutation
    // =========================================================================
    private void ExecuteVirusMutation()
    {
        GD.Print("[AI] Phase 6 — Decision Tree Mutation");
        _decisionTree.SelectAndApplyMutation(CurrentState);
    }

    // =========================================================================
    // PHASE 9 — Weighted Response System
    // =========================================================================
    private void EvaluatePlayerDefenses()
    {
        GD.Print("[AI] Phase 9 — Weighted Response System");
        _wrs.EvaluateAndScore(CurrentState);
    }

    // =========================================================================
    // PHASE 10 — Player Action Resolver
    // =========================================================================

    /// Defines all valid immune actions, their EP costs, and their effects.
    /// In a full game, the UI calls OnPlayerActionConfirmed(actionName, targetZone).
    /// For testing/simulation, we auto-pick the best action the player can afford.
    private void WaitForPlayerAction()
    {
        GD.Print("[PLAYER] Phase 10 — Awaiting player action");

        // In a full Godot game, this method returns here and the UI takes over.
        // The UI calls OnPlayerActionConfirmed(action, zone) when the player commits.

        // For automated testing: simulate a player action so the round can complete.
        // Remove or gate this with an #if DEBUG block when the UI is connected.
        SimulatePlayerAction();
    }

    /// Simulates a basic player decision for testing purposes.
    /// Picks the most cost-effective action the player can afford,
    /// targeting the most vulnerable uninfected zone adjacent to infection.
    private void SimulatePlayerAction()
    {
        string bestTarget = CurrentState.OrganGraph.Zones
            .Where(z => !z.Value.IsInfected)
            .OrderBy(z => z.Value.ActiveDefenseCount)
            .Select(z => z.Key)
            .FirstOrDefault();

        if (bestTarget == null)
        {
            GD.Print("[PLAYER-SIM] No uninfected zones to defend.");
            return;
        }

        // Pick best affordable action
        if (CurrentState.PlayerEp >= 10)
        {
            ApplyPlayerAction("WhiteBloodCells", bestTarget);
        }
        else
        {
            GD.Print("[PLAYER-SIM] Not enough EP for any action.");
        }
    }

    /// Applies a named immune action to a target zone.
    /// Call this from the UI when the player confirms their choice.
    ///
    /// Action names must match the keys in WeightedResponseSystem.BaseEffectiveness.
    public void ApplyPlayerAction(string actionName, string targetZone)
    {
        // EP costs per the proposal's table
        int cost = actionName switch
        {
            "WhiteBloodCells" => 10,
            "Antibodies" => 25,
            "Inflammation" => 20,
            "FeverResponse" => 35,
            "MemoryCells" => 15,
            "CytokineBurst" => 50,
            _ => 0
        };

        if (CurrentState.PlayerEp < cost)
        {
            GD.Print($"[PLAYER] Not enough EP for {actionName} (need {cost}, have {CurrentState.PlayerEp})");
            return;
        }

        CurrentState.PlayerEp -= cost;

        // Track deployed defenses for WRS
        if (!CurrentState.PlayerDefenses.ContainsKey(actionName))
            CurrentState.PlayerDefenses[actionName] = 0;
        CurrentState.PlayerDefenses[actionName]++;

        // Apply mechanical effect
        if (CurrentState.OrganGraph.Zones.ContainsKey(targetZone))
        {
            var zone = CurrentState.OrganGraph.Zones[targetZone];

            switch (actionName)
            {
                case "WhiteBloodCells":
                    zone.ActiveDefenseCount += 1;
                    GD.Print($"[PLAYER] WBC deployed to {targetZone} (+1 defense) | EP: {CurrentState.PlayerEp}");
                    break;

                case "Antibodies":
                    zone.ActiveDefenseCount += 2;
                    GD.Print($"[PLAYER] Antibodies deployed to {targetZone} (+2 defense) | EP: {CurrentState.PlayerEp}");
                    break;

                case "Inflammation":
                    zone.ActiveDefenseCount += 3;
                    CurrentState.SeverityIndex += 1; // slight host HP damage per proposal
                    GD.Print($"[PLAYER] Inflammation on {targetZone} (+3 defense, +1% severity) | EP: {CurrentState.PlayerEp}");
                    break;

                case "FeverResponse":
                    // Global virus slowdown: raise ALL zones' effective defense by 1 for 2 rounds
                    // We model this by raising defense on all infected zones
                    foreach (var z in CurrentState.OrganGraph.Zones.Values.Where(z => z.IsInfected))
                        z.ActiveDefenseCount += 1;
                    GD.Print($"[PLAYER] Fever Response — all infected zones +1 defense | EP: {CurrentState.PlayerEp}");
                    break;

                case "MemoryCells":
                    // Doubles the defense increment for a previously seen mutation
                    zone.ActiveDefenseCount += 2;
                    GD.Print($"[PLAYER] Memory Cells on {targetZone} (+2 defense vs known mutation) | EP: {CurrentState.PlayerEp}");
                    break;

                case "CytokineBurst":
                    // Massive damage to all infected zones, but +10% severity
                    foreach (var z in CurrentState.OrganGraph.Zones.Values.Where(z => z.IsInfected))
                        z.ActiveDefenseCount += 5;
                    CurrentState.SeverityIndex += 10;
                    CurrentState.InfectionRate = Math.Max(0, CurrentState.InfectionRate - 2);
                    GD.Print($"[PLAYER] CYTOKINE BURST — all infected zones +5 defense, -2 infection, +10% severity | EP: {CurrentState.PlayerEp}");
                    break;
            }
        }
    }

    // =========================================================================
    // PHASE 11a — Resolve Round
    // =========================================================================
    private void ResolveRound()
    {
        GD.Print("[RESOLVE] Phase 11a — Resolving round");

        // 1. Apply uncontested mutation severity penalty (+3% per uncontested mutation)
        int mutationPenalty = _decisionTree.CalculateUncontestedSeverity(CurrentState);
        if (mutationPenalty > 0)
        {
            CurrentState.SeverityIndex += mutationPenalty;
            GD.Print($"[RESOLVE] Uncontested mutations → +{mutationPenalty}% severity | " +
                     $"Total: {CurrentState.SeverityIndex}%");
        }

        // 2. Clamp severity to [0, 100]
        CurrentState.SeverityIndex = Math.Clamp(CurrentState.SeverityIndex, 0, 100);

        // 3. Reset per-round player defenses (defenses are deployed fresh each round)
        // Note: ActiveDefenseCount on zones persists (it accumulates across rounds)
        // but the PlayerDefenses tracking dict resets so WRS scores each round fresh
        CurrentState.PlayerDefenses.Clear();

        GD.Print($"[RESOLVE] Round {CurrentState.RoundNumber} complete | " +
                 $"Severity: {CurrentState.SeverityIndex}% | " +
                 $"Infection: {CurrentState.InfectionRate} | " +
                 $"EP: {CurrentState.PlayerEp}");
    }

    // =========================================================================
    // PHASE 11b — RL Q-Table Update
    // =========================================================================
    private void UpdateRLQTable()
    {
        GD.Print("[RL] Phase 11b — Updating Q-table");
        _virusRL.UpdateQTable(CurrentState);
    }
}
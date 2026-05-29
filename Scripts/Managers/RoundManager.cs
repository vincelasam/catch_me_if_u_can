using CatchMeIfYouCan.Scripts.AI;
using Godot;
using System;

public partial class RoundManager : Node
{
    public GameState CurrentState { get; set; }

    public override void _Ready()
    {
        // Phase 1: Initialize Game State
        InitializeGame();
        ExecuteRoundSequence();
    }

    public void ExecuteRoundSequence()
    {
        // Phase 2: New Round Begins
        RegenerateEP();

        // Phase 3 & 4: Virus Phase - RL Strategy & Minimax
        DetermineVirusStrategy();

        // Phase 5: Virus Phase - BFS Spread
        ExecuteBFSVirusSpread();

        // Phase 6: Virus Phase - Decision Tree Mutation
        ExecuteVirusMutation();

        // Phase 7: Infection Rate Check (Win Condition)
        if (CurrentState.IsWinConditionMet())
        {
            GD.Print("Player Wins! Virus eradicated.");
            return;
        }

        // Phase 8: Severity Index Check (Loss Condition)
        if (CurrentState.IsLossConditionMet())
        {
            GD.Print("Game Over. Severity reached 100%.");
            return;
        }

        // Phase 9: Immune System Phase - Weighted Response System
        EvaluatePlayerDefenses();

        // Phase 10: Immune System Phase - Deploy Action
        WaitForPlayerAction();

        // Phase 11 is triggered after player confirms action
    }

    public void OnPlayerActionConfirmed()
    {
        // Phase 11: Resolve Round & RL Q-Table Update
        ResolveRound();
        UpdateRLQTable();

        // Trigger next round loop here
    }

    // Stub functions for individual system logic
    private void InitializeGame() {
        CurrentState = new GameState();

        // Build the Organ Graph for testing
        CurrentState.OrganGraph.Zones.Add("Lungs", new OrganZone("Lungs", 5));
        CurrentState.OrganGraph.Zones.Add("Bloodstream", new OrganZone("Bloodstream", 10));
        CurrentState.OrganGraph.Zones.Add("LymphNodes", new OrganZone("LymphNodes", 10));

        // Connect BOTH the Bloodstream and Lymph Nodes to the Lungs
        CurrentState.OrganGraph.AddConnection("Lungs", "Bloodstream");
        CurrentState.OrganGraph.AddConnection("Lungs", "LymphNodes");

        // Start the infection in the Lungs
        CurrentState.OrganGraph.Zones["Lungs"].IsInfected = true;
        CurrentState.InfectionRate = 1;

        // THE TEST: Give the Bloodstream a high defense count
        CurrentState.OrganGraph.Zones["Bloodstream"].ActiveDefenseCount = 5;
        CurrentState.OrganGraph.Zones["LymphNodes"].ActiveDefenseCount = 0;

        GD.Print("Game Initialized. Lungs are infected.");
    }
        
    private void RegenerateEP() { }
    private void DetermineVirusStrategy() { }
    private void ExecuteBFSVirusSpread() {
        GD.Print("--- Phase 5: Executing BFS Virus Spread ---");

        VirusAI virusBrain = new VirusAI();
        virusBrain.ExecuteSpreadVirus(CurrentState);
    }
    private void ExecuteVirusMutation() { }
    private void EvaluatePlayerDefenses() { }
    private void WaitForPlayerAction() { }
    private void ResolveRound() { }
    private void UpdateRLQTable() { }
}
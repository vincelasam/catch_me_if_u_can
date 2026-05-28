using Godot;
using System;

public partial class GameIntegrationTest : Node
{
	public override void _Ready()
	{
		GD.Print("=== STARTING INTEGRATION TESTS ===");

		TestOrganGraphAndCloning();
		TestWinLossLogic();

		GD.Print("=== ALL INITIAL TESTS PASSED ===");
	}

	private void TestOrganGraphAndCloning()
	{
		// 1. I-setup ang graph batay sa setup specifications ng proposal
		OrganGraph originalGraph = new OrganGraph();
		originalGraph.Zones.Add("Lungs", new OrganZone("Lungs", 10));
		originalGraph.Zones.Add("Bloodstream", new OrganZone("Bloodstream", 5));
		originalGraph.AddConnection("Lungs", "Bloodstream");

		// 2. Subukan ang Deep Clone function (Crucial para sa Minimax Phase 4)
		GameState originalState = new GameState
		{
			OrganGraph = originalGraph,
			PlayerEp = 100,
			InfectionRate = 50,
			SeverityIndex = 10
		};

		GameState clonedState = originalState.Clone();

		// 3. I-modify ang clone para masiguradong hindi madadamay ang original state
		clonedState.OrganGraph.Zones["Lungs"].IsInfected = true;
		clonedState.OrganGraph.Zones["Lungs"].ActiveDefenseCount = 5;
		clonedState.SeverityIndex = 95;

		// Assertions (Dapat magkaiba ang values ng original sa clone)
		System.Diagnostics.Debug.Assert(originalState.OrganGraph.Zones["Lungs"].IsInfected == false, "BUG: Original graph was modified by clone changes!");
		System.Diagnostics.Debug.Assert(originalState.OrganGraph.Zones["Lungs"].ActiveDefenseCount == 0, "BUG: Original defense count updated by clone!");
		System.Diagnostics.Debug.Assert(originalState.SeverityIndex == 10, "BUG: Original severity index changed!");

		GD.Print("[PASSED] GameState.Clone() isolation and deep copying verified.");
	}

	private void TestWinLossLogic()
	{
		GameState testState = new GameState();

		// Test Phase 7: Win Condition Priority (Infection Rate is 0, Severity is high)
		testState.InfectionRate = 0;
		testState.SeverityIndex = 90;
		System.Diagnostics.Debug.Assert(testState.IsWinConditionMet() == true, "BUG: Failed to recognize win when infection rate is 0.");
		System.Diagnostics.Debug.Assert(testState.IsLossConditionMet() == false, "BUG: Marked as loss even when virus is fully eradicated.");

		// Test Phase 8: Loss Condition Trigger (Infection rate > 0, Severity reaches 100)
		testState.InfectionRate = 10;
		testState.SeverityIndex = 100;
		System.Diagnostics.Debug.Assert(testState.IsWinConditionMet() == false, "BUG: Marked as win but virus is still active.");
		System.Diagnostics.Debug.Assert(testState.IsLossConditionMet() == true, "BUG: Failed to trigger loss at 100% severity.");

		GD.Print("[PASSED] Win/Loss metrics and phase execution order verified.");
	}
}

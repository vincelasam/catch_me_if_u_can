using System.Collections.Generic;
using CatchMeIfYouCan.Scripts.Core;

public class GameState
{
    // -----------------------------------------------------------------------
    // Core meters (unchanged from original)
    // -----------------------------------------------------------------------
    public OrganGraph OrganGraph { get; set; }
    public int PlayerEp { get; set; }
    public int InfectionRate { get; set; }
    public int SeverityIndex { get; set; }

    // -----------------------------------------------------------------------
    // Difficulty — set once at game start, read by every AI system
    // -----------------------------------------------------------------------
    public DifficultyMode Difficulty { get; set; } = DifficultyMode.Casual;
    public DifficultySettings Settings => DifficultySettings.For(Difficulty);

    // -----------------------------------------------------------------------
    // Round tracking
    // RoundNumber starts at 1 and increments in ResolveRound().
    // Used by the Decision Tree to respect MutationIntervalRounds.
    // -----------------------------------------------------------------------
    public int RoundNumber { get; set; } = 1;

    // -----------------------------------------------------------------------
    // Active mutations (original list, kept for save/load compatibility)
    // ActiveMutationStacks: tracks how many times each mutation name has been
    // applied. The Decision Tree uses this to decide whether to "compound"
    // (e.g. if Antigenic_Drift is already active, upgrade to Antigenic_Shift).
    // -----------------------------------------------------------------------
    public List<string> ActiveMutations { get; set; }
    public Dictionary<string, int> ActiveMutationStacks { get; set; }

    // -----------------------------------------------------------------------
    // Weighted Response System output (Phase 9 → Phase 6 bridge)
    // Key   = defense action name (e.g. "WhiteBloodCells", "Antibodies")
    // Value = weighted effectiveness score this round
    // The Decision Tree in Phase 6 reads the highest-scoring entry.
    // -----------------------------------------------------------------------
    public Dictionary<string, float> LastUsedDefenses { get; set; }

    // -----------------------------------------------------------------------
    // Player defense tracking — used by WRS and player action resolver
    // Key   = defense action name
    // Value = number of units of that defense currently deployed
    // -----------------------------------------------------------------------
    public Dictionary<string, int> PlayerDefenses { get; set; }

    // -----------------------------------------------------------------------
    // Minimax → BFS handoff
    // DetermineVirusStrategy() writes the Minimax-recommended target here.
    // ExecuteBFSVirusSpread() can prioritize this zone when sorting neighbors.
    // -----------------------------------------------------------------------
    public string MinimaxRecommendedTarget { get; set; } = null;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------
    public GameState()
    {
        OrganGraph = new OrganGraph();
        ActiveMutations = new List<string>();
        ActiveMutationStacks = new Dictionary<string, int>();
        LastUsedDefenses = new Dictionary<string, float>();
        PlayerDefenses = new Dictionary<string, int>();
    }

    // -----------------------------------------------------------------------
    // Win / Loss conditions (unchanged — Phase 7 evaluated FIRST)
    // -----------------------------------------------------------------------

    /// <summary>Phase 7: Player wins if InfectionRate hits zero.</summary>
    public bool IsWinConditionMet() => InfectionRate <= 0;

    /// <summary>Phase 8: Player loses if SeverityIndex hits 100.</summary>
    public bool IsLossConditionMet() => SeverityIndex >= 100;

    // -----------------------------------------------------------------------
    // Deep clone — required by Minimax to sandbox future states
    // -----------------------------------------------------------------------
    public GameState Clone()
    {
        var clone = new GameState
        {
            OrganGraph = this.OrganGraph.Clone(),
            PlayerEp = this.PlayerEp,
            InfectionRate = this.InfectionRate,
            SeverityIndex = this.SeverityIndex,
            Difficulty = this.Difficulty,
            RoundNumber = this.RoundNumber,
            ActiveMutations = new List<string>(this.ActiveMutations),
            ActiveMutationStacks = new Dictionary<string, int>(this.ActiveMutationStacks),
            LastUsedDefenses = new Dictionary<string, float>(this.LastUsedDefenses),
            PlayerDefenses = new Dictionary<string, int>(this.PlayerDefenses),
            MinimaxRecommendedTarget = this.MinimaxRecommendedTarget
        };
        return clone;
    }
}
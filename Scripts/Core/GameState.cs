using System.Collections.Generic;
using CatchMeIfYouCan.Scripts.Core;

public class GameState
{
    public OrganGraph OrganGraph { get; set; }
    public int PlayerEp { get; set; }
    public int InfectionRate { get; set; }
    public int SeverityIndex { get; set; }
    public DifficultyMode Difficulty { get; set; } = DifficultyMode.Casual;
    public DifficultySettings Settings => DifficultySettings.For(Difficulty);
    public int RoundNumber { get; set; } = 1;


    public List<string> ActiveMutations { get; set; }
    public Dictionary<string, int> ActiveMutationStacks { get; set; }
    public Dictionary<string, float> LastUsedDefenses { get; set; }
    public Dictionary<string, int> PlayerDefenses { get; set; }
    public string MinimaxRecommendedTarget { get; set; } = null;



    public GameState()
    {
        OrganGraph = new OrganGraph();
        ActiveMutations = new List<string>();
        ActiveMutationStacks = new Dictionary<string, int>();
        LastUsedDefenses = new Dictionary<string, float>();
        PlayerDefenses = new Dictionary<string, int>();

    }

    // Phase 7: Evaluated FIRST
    public bool IsWinConditionMet()
    {
        return InfectionRate <= 0;
    }

    // Phase 8: Evaluated SECOND
    public bool IsLossConditionMet()
    {
        return SeverityIndex >= 100;
    }

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
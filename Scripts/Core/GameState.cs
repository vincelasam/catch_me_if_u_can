using System.Collections.Generic;

public class GameState
{
    public OrganGraph OrganGraph { get; set; }
    public int PlayerEp { get; set; }
    public int InfectionRate { get; set; }
    public int SeverityIndex { get; set; }
    public List<string> ActiveMutations { get; set; }

    public GameState()
    {
        OrganGraph = new OrganGraph();
        ActiveMutations = new List<string>();
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
        return new GameState
        {
            OrganGraph = this.OrganGraph.Clone(),
            PlayerEp = this.PlayerEp,
            InfectionRate = this.InfectionRate,
            SeverityIndex = this.SeverityIndex,
            ActiveMutations = new List<string>(this.ActiveMutations)
        };
    }
}
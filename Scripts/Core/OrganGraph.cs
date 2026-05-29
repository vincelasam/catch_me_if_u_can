using System.Collections.Generic;

public class OrganZone
{
    public string Name { get; set; }
    public int ActiveDefenseCount { get; set; }
    public bool IsInfected { get; set; }
    public int EpGeneration { get; set; }

    public OrganZone(string name, int epGen)
    {
        Name = name;
        EpGeneration = epGen;
        ActiveDefenseCount = 0;
        IsInfected = false;
    }

    // Required for GameState.Clone()
    public OrganZone Clone()
    {
        return new OrganZone(Name, EpGeneration)
        {
            ActiveDefenseCount = this.ActiveDefenseCount,
            IsInfected = this.IsInfected
        };
    }
}

public class OrganGraph
{
    public Dictionary<string, OrganZone> Zones { get; private set; }
    public Dictionary<string, List<string>> AdjacencyList { get; private set; }

    public OrganGraph()
    {
        Zones = new Dictionary<string, OrganZone>();
        AdjacencyList = new Dictionary<string, List<string>>();
    }

    public void AddConnection(string zoneA, string zoneB)
    {
        if (!AdjacencyList.ContainsKey(zoneA)) AdjacencyList[zoneA] = new List<string>();
        if (!AdjacencyList.ContainsKey(zoneB)) AdjacencyList[zoneB] = new List<string>();

        AdjacencyList[zoneA].Add(zoneB);
        AdjacencyList[zoneB].Add(zoneA);
    }

    // Deep clone implementation for Minimax
    public OrganGraph Clone()
    {
        var clone = new OrganGraph();
        foreach (var kvp in Zones)
        {
            clone.Zones.Add(kvp.Key, kvp.Value.Clone());
        }
        foreach (var kvp in AdjacencyList)
        {
            clone.AdjacencyList.Add(kvp.Key, new List<string>(kvp.Value));
        }
        return clone;
    }
}
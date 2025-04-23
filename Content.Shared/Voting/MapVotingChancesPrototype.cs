using Robust.Shared.Prototypes;

namespace Content.Shared.Voting.Prototypes;

[Prototype("mapvotingchances")]
public sealed partial class MapVotingChancesPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
    
    [DataField("chances")] 
    public Dictionary<string, float> Chances { get; private set; } = new();
    
    private MapVotingChancesPrototype() { }
    
    public MapVotingChancesPrototype(string id, Dictionary<string, float> chances)
    {
        ID = id;
        Chances = chances;
    }
}
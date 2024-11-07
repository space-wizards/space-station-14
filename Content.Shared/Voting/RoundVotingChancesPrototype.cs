using Robust.Shared.Prototypes;

namespace Content.Shared.Voting.Prototypes;

[Prototype("roundvotingchances")]
public sealed partial class RoundVotingChancesPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
    
    [DataField("chances")] 
    public Dictionary<string, float> Chances { get; private set; } = new();
    
    private RoundVotingChancesPrototype() { }
    
    public RoundVotingChancesPrototype(string id, Dictionary<string, float> chances)
    {
        ID = id;
        Chances = chances;
    }
}
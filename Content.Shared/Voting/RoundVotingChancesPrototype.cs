using Robust.Shared.Prototypes;
using Content.Shared._Starlight.Voting; // Starlight-edit

namespace Content.Shared.Voting.Prototypes;

[Prototype("roundvotingchances")]
public sealed partial class RoundVotingChancesPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
    
    [DataField("chances")] 
    public List<GameModeChance> Chances { get; private set; } = new(); // Starlight-edit
    
    private RoundVotingChancesPrototype() { }
    
    public RoundVotingChancesPrototype(string id, List<GameModeChance> chances) // Starlight-edit
    {
        ID = id;
        Chances = chances;
    }
}
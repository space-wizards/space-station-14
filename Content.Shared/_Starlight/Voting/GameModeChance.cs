using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Voting;

public sealed class GameModeChance
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public float Chance { get; set; } = default!;

    [DataField("memorizeCount")]
    public int MemorizeRoundCount { get; set; } = 0;
}
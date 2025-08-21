using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Voting;

[DataDefinition]
public sealed class GameModeChance
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public float Chance { get; set; } = default!;

    [DataField]
    public int MemorizeCount { get; set; } = 0;
}
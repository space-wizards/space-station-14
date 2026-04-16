using Robust.Shared.Prototypes;

namespace Content.Shared._Remnants.Looting;

[RegisterComponent]
public sealed partial class LootCrateComponent : Component
{
    [DataField]
    public TimeSpan CooldownMin = TimeSpan.FromSeconds(600);

    [DataField]
    public TimeSpan CooldownMax = TimeSpan.FromSeconds(1200);

    [DataField]
    public TimeSpan NextUseTime;

    [DataField]
    public EntProtoId? CommonLootTable;

    [DataField]
    public EntProtoId? RareLootTable;

    [DataField]
    public EntProtoId? LegendaryLootTable;
}

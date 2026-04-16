namespace Content.Shared._Remnants.Looting;

using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class LootTableComponent : Component
{
    [DataField]
    public List<EntProtoId> Entries = new();
}

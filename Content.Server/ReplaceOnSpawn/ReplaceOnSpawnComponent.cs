using Robust.Shared.Prototypes;

namespace Content.Server.ReplaceOnSpawn;

[RegisterComponent]
public sealed partial class ReplaceOnSpawnComponent : Component
{
    [DataField]
    public EntProtoId? Prototype;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Chance = 0.01f;
}

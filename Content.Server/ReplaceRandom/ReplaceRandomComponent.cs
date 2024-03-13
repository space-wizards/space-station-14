using Robust.Shared.Prototypes;

namespace Content.Server.ReplaceRandom;

[RegisterComponent]
public sealed partial class ReplaceRandomComponent : Component
{
    [DataField]
    public EntProtoId? Prototype;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Chance = 0.001f;
}

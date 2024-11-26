using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Energy.Supermatter;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Activated = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccHeat = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccRadiation = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccLighting = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccBreak = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 RadiationStability = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Durability = 100f;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 LastSendedDurability = 100f;
}

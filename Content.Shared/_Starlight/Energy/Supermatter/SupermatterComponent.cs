using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Energy.Supermatter;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccHeat = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccRadiation = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccLighting = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 AccBreak = 0f;
}

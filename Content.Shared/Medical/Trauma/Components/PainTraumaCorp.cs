using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Trauma.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class PainTraumaComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 PainDelta = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 PainCapDelta = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 PainMultiplierDelta = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 PainMitigationDelta = 0;
}

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Trauma.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class PainTraumaComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PainDecrease = 0;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PainCapDecrease = 0;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PainMultiplierDecrease = 0;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PainMitigationDecrease = 0;
}

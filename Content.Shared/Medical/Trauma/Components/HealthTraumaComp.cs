using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Trauma.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class HealthTraumaComponent : Component
{
    /// <summary>
    /// How much are we decreasing our woundables health cap, expressed as a percentage (/100) of maximum
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 HealthCapDelta = 0;
}

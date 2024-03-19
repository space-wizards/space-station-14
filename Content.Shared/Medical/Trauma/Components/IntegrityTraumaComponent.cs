using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Trauma.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class IntegrityTraumaComponent : Component
{
    /// <summary>
    /// How much are we decreasing our woundables integrity cap, expressed as a percentage (/100) of maximum
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 IntegrityCapDecrease = 0;

    /// <summary>
    /// How much damage are we applying to integrity, expressed as a percentage (/100) of maximum integrity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 IntegrityDamage = 0;
}

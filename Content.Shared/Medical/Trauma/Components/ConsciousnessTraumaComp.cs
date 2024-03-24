using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Trauma.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class ConsciousnessTraumaComponent : Component
{
    /// <summary>
    /// How much are we decreasing our consciousness cap
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 CapDelta = 0;

    /// <summary>
    /// How much are we decreasing our consciousness
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Delta = 0;

    /// <summary>
    /// How much we should change the consciousness multiplier by
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MultiplierDelta = 0;
}

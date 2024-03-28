using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Trauma.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class ConsciousnessTraumaComponent : Component
{
    /// <summary>
    /// How much are we decreasing our consciousness cap
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CapDecrease= 0;

    /// <summary>
    /// How much are we decreasing our consciousness
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Decrease = 0;

    /// <summary>
    /// How much we should change the consciousness multiplier by
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MultiplierDecrease = 0;

    /// <summary>
    /// How much we should change the consciousness multiplier by
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ModifierDecrease = 0;
}

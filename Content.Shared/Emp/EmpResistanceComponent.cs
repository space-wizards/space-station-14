using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Emp;

/// <summary>
/// An entity with this component resists or is fully immune to EMPs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedEmpSystem))]
public sealed partial class EmpResistanceComponent : Component
{
    /// <summary>
    ///     The proportion of the EMP effect that is resisted. 1.00 indicates full immunity while 0.00 indicates no resistance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Resistance = FixedPoint2.Zero;
}

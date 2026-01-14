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
    /// The strength of the EMP gets multiplied by this value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 1f;
}

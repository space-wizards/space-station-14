using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// This is used to apply a friction modifier to an entity temporarily
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunSystem))]
public sealed partial class FrictionStatusComponent : Component
{
    /// <summary>
    /// Friction modifier applied as a status.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionModifier = 1f;

    /// <summary>
    /// Acceleration modifier applied as a status.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AccelerationModifier = 1f;
}

using Content.Shared.Stunnable;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to apply a friction modifier to an entity temporarily
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunSystem))]
public sealed partial class FrictionModifierComponent : Component
{
    /// <summary>
    /// Friction modifier applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionModifier = 1f;

    /// <summary>
    /// Acceleration modifier applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AccelerationModifier = 1f;
}

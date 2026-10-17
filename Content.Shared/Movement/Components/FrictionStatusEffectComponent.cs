using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to apply a friction modifier to an entity temporarily
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MovementModStatusSystem))]
public sealed partial class FrictionStatusEffectComponent : Component
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

using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to apply a friction modifier to an entity temporarily
/// To be used only in conjunction with <see cref="FrictionStatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MovementModStatusSystem))]
public sealed partial class FrictionStatusModifierComponent : Component
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

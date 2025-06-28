using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to apply a movement speed modifier to an entity temporarily
/// To be used only in conjunction with <see cref="MovementModStatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MovementModStatusSystem))]
public sealed partial class MovementModStatusComponent : Component
{
    /// <summary>
    /// Multiplicative sprint modifier, with bounds of [0, 1)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprintSpeedModifier = 0.5f;

    /// <summary>
    /// Multiplicative walk modifier, with bounds of [0, 1)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WalkSpeedModifier = 0.5f;
}

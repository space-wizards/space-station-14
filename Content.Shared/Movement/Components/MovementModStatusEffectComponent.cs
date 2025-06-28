using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to store a movement speed modifier attached to a status effect entity so it can be applied via statuses.
/// To be used in conjunction with <see cref="MovementModStatusSystem"/>.
/// See <see cref="MovementModStatusComponent"/> for the component applied to the entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MovementModStatusSystem))]
public sealed partial class MovementModStatusEffectComponent : Component
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

using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to store a movement speed modifier attached to a status effect entity so it can be applied via statuses.
/// To be used in conjunction with <see cref="SlowedStatusSystem"/>.
/// See <see cref="SlowedDownComponent"/> for the component applied to the entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlowdownStatusEffectComponent : Component
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

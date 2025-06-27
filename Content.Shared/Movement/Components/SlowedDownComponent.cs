using Content.Shared.Stunnable;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This is used to apply a movement speed modifier to an entity temporarily
/// To be used only in conjunction with <see cref="Movement.Components.SlowdownStatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SlowedStatusSystem))]
public sealed partial class SlowedDownComponent : Component
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

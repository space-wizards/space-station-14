using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Emp;

/// <summary>
/// While entity has this component it is "disabled" by EMP.
/// Add desired behaviour in other systems.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedEmpSystem))]
public sealed partial class EmpDisabledComponent : Component
{
    /// <summary>
    /// Moment of time when the component is removed and entity stops being "disabled".
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan DisabledUntil = TimeSpan.Zero;

    /// <summary>
    /// Default time between visual effect spawns.
    /// This gets a random multiplier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EffectCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// When next effect will be spawned.
    /// TODO: Particle system.
    /// </summary>
    [AutoPausedField]
    public TimeSpan TargetTime = TimeSpan.Zero;
}

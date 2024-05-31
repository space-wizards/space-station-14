using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Emp;

/// <summary>
/// While entity has this component it is "disabled" by EMP.
/// Add desired behaviour in other systems
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedEmpSystem))]
public sealed partial class EmpDisabledComponent : Component
{
    /// <summary>
    /// Moment of time when component is removed and entity stops being "disabled"
    /// </summary>
    [DataField("timeLeft", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan DisabledUntil;

    [DataField("effectCoolDown"), ViewVariables(VVAccess.ReadWrite)]
    public float EffectCooldown = 3f;

    /// <summary>
    /// When next effect will be spawned
    /// </summary>
    [AutoPausedField]
    public TimeSpan TargetTime = TimeSpan.Zero;
}

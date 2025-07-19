using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.KillTome;

/// <summary>
/// Entity with this component is a Kill Tome target.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class KillTomeTargetComponent : Component
{
    /// <summary>
    /// The time when the target is killed.
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan KillTime;
}

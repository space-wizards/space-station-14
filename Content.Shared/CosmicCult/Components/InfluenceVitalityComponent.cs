using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class InfluenceVitalityComponent : Component
{
    /// <summary>
    /// the timer used for ticking healing from vacuous vitality
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CheckTimer;

    /// <summary>
    /// the amount of time between the above timer's ticks
    /// </summary>
    [DataField]
    public TimeSpan CheckWait = TimeSpan.FromSeconds(5);
}

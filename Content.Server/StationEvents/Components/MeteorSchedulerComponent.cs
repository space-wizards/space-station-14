using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// This is used for running meteor swarm events at regular intervals.
/// </summary>
[RegisterComponent, Access(typeof(MeteorSchedulerSystem)), AutoGenerateComponentPause]
public sealed partial class MeteorSchedulerComponent : Component
{
    /// <summary>
    /// The weights for which swarms will be selected.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> Config = "DefaultConfig";

    /// <summary>
    /// The time at which the next swarm occurs.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextSwarmTime = TimeSpan.Zero;

    /// <summary>
    /// The minimum time between swarms
    /// </summary>
    [DataField]
    public TimeSpan MinSwarmDelay = TimeSpan.FromMinutes(7.5f);

    /// <summary>
    /// The maximum time between swarms
    /// </summary>
    [DataField]
    public TimeSpan MaxSwarmDelay = TimeSpan.FromMinutes(12.5f);
}

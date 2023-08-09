using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Access(typeof(FluidSpreaderSystem))]
public sealed class FluidMapDataComponent : Component
{
    /// <summary>
    /// At what time will <see cref="FluidSpreaderSystem"/> be checked next
    /// </summary>
    [DataField("goalTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan GoalTime;

    /// <summary>
    /// Delay between two runs of <see cref="FluidSpreaderSystem"/>
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Puddles to be expanded.
    /// </summary>
    [DataField("puddles")] public HashSet<EntityUid> Puddles = new();

    /// <summary>
    /// Convenience method for setting GoalTime to <paramref name="start"/> + <see cref="Delay"/>
    /// </summary>
    /// <param name="start">Time to which to add <see cref="Delay"/>, defaults to current <see cref="GoalTime"/></param>
    public void UpdateGoal(TimeSpan? start = null)
    {
        GoalTime = (start ?? GoalTime) + Delay;
    }
}

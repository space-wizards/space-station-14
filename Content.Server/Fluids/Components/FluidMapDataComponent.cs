using Content.Server.Fluids.EntitySystems;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Access(typeof(FluidSpreaderSystem))]
public sealed class FluidMapDataComponent : Component
{
    /// <summary>
    /// At what time will <see cref="FluidSpreaderSystem"/> be checked next
    /// </summary>
    public TimeSpan GoalTime;

    /// <summary>
    /// Delay between two runs of <see cref="FluidSpreaderSystem"/>
    /// </summary>
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// MapUid to which this component belongs, must be set to valid MapUid
    /// </summary>
    public EntityUid MapUid = EntityUid.Invalid;

    /// <summary>
    /// Entities needing to be expanded. Maps Puddle Uid to an edge presenting frontier of a BFS (Breadth First Search)
    /// </summary>
    public  Dictionary<EntityUid, OverflowEdgeComponent> FluidSpread = new();

    /// <summary>
    /// Convenience method for setting GoalTime to <paramref name="start"/> + <see cref="Delay"/>
    /// </summary>
    /// <param name="start">Time to which to add <see cref="Delay"/>, defaults to current <see cref="GoalTime"/></param>
    public void UpdateGoal(TimeSpan? start = null)
    {
        GoalTime = (start ?? GoalTime) + Delay;
    }
}

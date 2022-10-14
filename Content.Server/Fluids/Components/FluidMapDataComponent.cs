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
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Puddles to be expanded.
    /// </summary>
    public  HashSet<EntityUid> Puddles = new();

    /// <summary>
    /// Convenience method for setting GoalTime to <paramref name="start"/> + <see cref="Delay"/>
    /// </summary>
    /// <param name="start">Time to which to add <see cref="Delay"/>, defaults to current <see cref="GoalTime"/></param>
    public void UpdateGoal(TimeSpan? start = null)
    {
        GoalTime = (start ?? GoalTime) + Delay;
    }
}

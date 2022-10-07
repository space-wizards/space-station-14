using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Access(typeof(FluidSpreaderSystem))]
public sealed class FluidSpreaderComponent : Component
{
    [ViewVariables] public Solution OverflownSolution = default!;
    [ViewVariables] public EntityUid MapUid = EntityUid.Invalid;

    public bool Enabled { get; set; }
}

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
    /// Entities needing to be expanded.
    /// </summary>
    public readonly HashSet<EntityUid> FluidSpread = new();

    /// <summary>
    /// Convenience method for setting GoalTime to <paramref name="start"/> + <see cref="Delay"/>
    /// </summary>
    /// <param name="start">Time to which to add <see cref="Delay"/>, defaults to current <see cref="GoalTime"/></param>
    public void UpdateGoal(TimeSpan? start = null)
    {
        GoalTime = (start ?? GoalTime) + Delay;
    }
}

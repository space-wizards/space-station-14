using Robust.Shared.Timing;

namespace Content.Shared.Movement.Components;

public abstract class MoverComponent : Component
{
    // TODO: Make a SimpleMoverComponent that's abstract that vehicles and mobmover can inherit from
    // TODO: Move the accel movement cvars over to mobmover.

    /// <summary>
    /// Last whole tick we were running subtick inputs for.
    /// </summary>
    public GameTick _lastInputTick;

    /// <summary>
    /// Last subtick that we ran inputs for.
    /// </summary>
    public ushort _lastInputSubTick;
}

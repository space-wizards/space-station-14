using Robust.Shared.Timing;

namespace Content.Shared.Movement.Components;

public abstract class MoverComponent : Component
{
    /// <summary>
    /// Last whole tick we were running subtick inputs for.
    /// </summary>
    public GameTick LastInputTick;

    /// <summary>
    /// Last subtick that we ran inputs for.
    /// </summary>
    public ushort LastInputSubTick;
}

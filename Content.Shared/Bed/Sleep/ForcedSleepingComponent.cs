using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep
{
    [NetworkedComponent, RegisterComponent]
    /// <summary>
    /// Status effect that prevents waking.
    /// </summary>
    public sealed class ForcedSleepingComponent : Component
    {}
}

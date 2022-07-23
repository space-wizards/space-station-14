using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep
{
    [NetworkedComponent, RegisterComponent]
    /// <summary>
    /// Prevents waking up. Use as a status effect.
    /// </summary>
    public sealed class ForcedSleepingComponent : Component
    {}
}

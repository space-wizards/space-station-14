using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep
{
    /// <summary>
    /// Prevents waking up. Use as a status effect.
    /// </summary>
    [NetworkedComponent, RegisterComponent]
    public sealed class ForcedSleepingComponent : Component
    {}
}

using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep
{
    [NetworkedComponent, RegisterComponent]
    /// <summary>
    /// Prevents waking up. Ticks up and removes itself when it hits the target duration.
    /// </summary>
    public sealed class ForcedSleepingComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("targetDuration")]
        public TimeSpan TargetDuration = TimeSpan.FromSeconds(5);
    }
}

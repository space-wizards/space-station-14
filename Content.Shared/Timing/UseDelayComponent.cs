using System.Threading;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Timing
{
    /// <summary>
    /// Timer that creates a cooldown each time an object is activated/used
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class UseDelayComponent : Component
    {
        public TimeSpan LastUseTime;

        public TimeSpan? DelayEndTime;

        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Delay = TimeSpan.FromSeconds(1);

        /// <summary>
        ///     Stores remaining delay pausing (and eventually, serialization).
        /// </summary>
        [DataField("remainingDelay")]
        public TimeSpan? RemainingDelay;

        public bool ActiveDelay => DelayEndTime != null;
    }

    [Serializable, NetSerializable]
    public sealed class UseDelayComponentState : ComponentState
    {
        public readonly TimeSpan LastUseTime;
        public readonly TimeSpan Delay;
        public readonly TimeSpan? DelayEndTime;

        public UseDelayComponentState(TimeSpan lastUseTime, TimeSpan delay, TimeSpan? delayEndTime)
        {
            LastUseTime = lastUseTime;
            Delay = delay;
            DelayEndTime = delayEndTime;
        }
    }
}

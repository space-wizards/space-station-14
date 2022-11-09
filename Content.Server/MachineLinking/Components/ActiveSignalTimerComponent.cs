
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class ActiveSignalTimerComponent : Component
    {
        /// <summary>
        ///     The time the timer triggers.
        /// </summary>
        [DataField("triggerTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan TriggerTime;
    }
}

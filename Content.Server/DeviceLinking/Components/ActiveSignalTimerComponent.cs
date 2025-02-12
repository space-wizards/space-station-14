
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed partial class ActiveSignalTimerComponent : Component
    {
        /// <summary>
        ///     The time the timer triggers.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)))]
        public TimeSpan TriggerTime;
    }
}

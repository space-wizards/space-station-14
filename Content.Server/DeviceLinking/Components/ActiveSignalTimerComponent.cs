
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    [AutoGenerateComponentPause]
    public sealed partial class ActiveSignalTimerComponent : Component
    {
        /// <summary>
        ///     The time the timer triggers.
        /// </summary>
        [DataField("triggerTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
        public TimeSpan TriggerTime;
    }
}

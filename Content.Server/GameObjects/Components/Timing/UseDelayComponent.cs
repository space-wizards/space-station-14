using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.Timing
{
    /// <summary>
    /// Timer that creates a cooldown each time an object is activated/used
    /// </summary>
    [RegisterComponent]
    public class UseDelayComponent : Component
    {
        public override string Name => "UseDelay";

        /// <summary>
        /// The time, in milliseconds, between an object's use and when it can be used again
        /// </summary>
        public int _delay;
        public bool _activeDelay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _delay, "delay", 1);
        }

        public void BeginDelay()
        {
            if (_activeDelay)
            {
                return;
            }

            _activeDelay = true;
            Timer.Spawn(_delay, () => _activeDelay = false);
        }
    }
}

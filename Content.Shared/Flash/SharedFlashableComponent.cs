using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Flash
{
    [NetworkedComponent, Friend(typeof(SharedFlashSystem))]
    public abstract class SharedFlashableComponent : Component
    {
        public float Duration { get; set; }
        public TimeSpan LastFlash { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class FlashableComponentState : ComponentState
    {
        public float Duration { get; }
        public TimeSpan Time { get; }

        public FlashableComponentState(float duration, TimeSpan time)
        {
            Duration = duration;
            Time = time;
        }
    }
}

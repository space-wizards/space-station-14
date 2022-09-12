using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Flash
{
    [NetworkedComponent, Access(typeof(SharedFlashSystem))]
    public abstract class SharedFlashableComponent : Component
    {
        public float Duration { get; set; }
        public TimeSpan LastFlash { get; set; }

        public override bool SendOnlyToOwner => true;
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

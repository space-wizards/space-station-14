using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Flash
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FlashableComponent : Component
    {
        public float Duration;
        public TimeSpan LastFlash;

        public override bool SendOnlyToOwner => true;
    }

    [Serializable, NetSerializable]
    public sealed partial class FlashableComponentState : ComponentState
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

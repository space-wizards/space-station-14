#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Flash
{
    public class SharedFlashableComponent : Component
    {
        public override string Name => "Flashable";
        public override uint? NetID => ContentNetIDs.FLASHABLE;
    }

    [Serializable, NetSerializable]
    public class FlashComponentState : ComponentState
    {
        public double Duration { get; }
        public TimeSpan Time { get; }

        public FlashComponentState(double duration, TimeSpan time) : base(ContentNetIDs.FLASHABLE)
        {
            Duration = duration;
            Time = time;
        }
    }
}

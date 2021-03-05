#nullable enable
using System;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public abstract class SharedHyposprayComponent : Component
    {
        public sealed override string Name => "Hypospray";
        public sealed override uint? NetID => ContentNetIDs.HYPOSPRAY;

        [Serializable, NetSerializable]
        protected sealed class HyposprayComponentState : ComponentState
        {
            public ReagentUnit CurVolume { get; }
            public ReagentUnit MaxVolume { get; }

            public HyposprayComponentState(ReagentUnit curVolume, ReagentUnit maxVolume) : base(ContentNetIDs.HYPOSPRAY)
            {
                CurVolume = curVolume;
                MaxVolume = maxVolume;
            }
        }
    }
}

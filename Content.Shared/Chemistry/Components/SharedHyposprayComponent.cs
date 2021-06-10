#nullable enable
using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
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

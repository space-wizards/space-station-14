using System;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    [NetworkedComponent()]
    public abstract class SharedHyposprayComponent : Component
    {
        public sealed override string Name => "Hypospray";

        [Serializable, NetSerializable]
        protected sealed class HyposprayComponentState : ComponentState
        {
            public ReagentUnit CurVolume { get; }
            public ReagentUnit MaxVolume { get; }

            public HyposprayComponentState(ReagentUnit curVolume, ReagentUnit maxVolume)
            {
                CurVolume = curVolume;
                MaxVolume = maxVolume;
            }
        }
    }
}

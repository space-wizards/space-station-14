using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    [NetworkedComponent()]
    public abstract class SharedHyposprayComponent : Component
    {
        public sealed override string Name => "Hypospray";
        public const string SolutionName = "hypospray";

        [Serializable, NetSerializable]
        protected sealed class HyposprayComponentState : ComponentState
        {
            public FixedPoint2 CurVolume { get; }
            public FixedPoint2 MaxVolume { get; }

            public HyposprayComponentState(FixedPoint2 curVolume, FixedPoint2 maxVolume)
            {
                CurVolume = curVolume;
                MaxVolume = maxVolume;
            }
        }
    }
}

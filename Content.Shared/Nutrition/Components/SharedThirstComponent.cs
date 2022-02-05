using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    [NetworkedComponent()]
    public abstract class SharedThirstComponent : Component
    {
        [ViewVariables]
        public abstract ThirstThreshold CurrentThirstThreshold { get; }

        [Serializable, NetSerializable]
        protected sealed class ThirstComponentState : ComponentState
        {
            public ThirstThreshold CurrentThreshold { get; }

            public ThirstComponentState(ThirstThreshold currentThreshold)
            {
                CurrentThreshold = currentThreshold;
            }
        }

    }

    [NetSerializable, Serializable]
    public enum ThirstThreshold : byte
    {
        // Hydrohomies
        OverHydrated,
        Okay,
        Thirsty,
        Parched,
        Dead,
    }
}

using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    [NetworkedComponent()]
    public abstract class SharedThirstComponent : Component, IMoveSpeedModifier
    {
        public sealed override string Name => "Thirst";

        [ViewVariables]
        public abstract ThirstThreshold CurrentThirstThreshold { get; }

        float IMoveSpeedModifier.SprintSpeedModifier
        {
            get
            {
                if (CurrentThirstThreshold == ThirstThreshold.Parched)
                {
                    return 0.75f;
                }
                return 1.0f;
            }
        }
        float IMoveSpeedModifier.WalkSpeedModifier
        {
            get
            {
                if (CurrentThirstThreshold == ThirstThreshold.Parched)
                {
                    return 0.75f;
                }
                return 1.0f;
            }
        }

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

using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    [NetworkedComponent()]
    public abstract class SharedHungerComponent : Component, IMoveSpeedModifier
    {
        public sealed override string Name => "Hunger";

        [ViewVariables]
        public abstract HungerThreshold CurrentHungerThreshold { get; }


        float IMoveSpeedModifier.WalkSpeedModifier
        {
            get
            {
                if (CurrentHungerThreshold == HungerThreshold.Starving)
                {
                    return 0.75f;
                }
                return 1.0f;
            }
        }
        float IMoveSpeedModifier.SprintSpeedModifier
        {
            get
            {
                if (CurrentHungerThreshold == HungerThreshold.Starving)
                {
                    return 0.75f;
                }
                return 1.0f;
            }
        }

        [Serializable, NetSerializable]
        protected sealed class HungerComponentState : ComponentState
        {
            public HungerThreshold CurrentThreshold { get; }

            public HungerComponentState(HungerThreshold currentThreshold)
            {
                CurrentThreshold = currentThreshold;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum HungerThreshold : byte
    {
        Overfed,
        Okay,
        Peckish,
        Starving,
        Dead,
    }
}

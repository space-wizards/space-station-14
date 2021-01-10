using System;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Nutrition
{
    public abstract class SharedHungerComponent : Component, IMoveSpeedModifier
    {
        public sealed override string Name => "Hunger";

        public sealed override uint? NetID => ContentNetIDs.HUNGER;

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

            public HungerComponentState(HungerThreshold currentThreshold) : base(ContentNetIDs.HUNGER)
            {
                CurrentThreshold = currentThreshold;
            }
        }
    }

    public enum HungerThreshold : byte
    {
        Overfed,
        Okay,
        Peckish,
        Starving,
        Dead,
    }
}

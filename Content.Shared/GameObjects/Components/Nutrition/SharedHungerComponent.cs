using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Nutrition
{
    [NetSerializable, Serializable]
    public enum HungerThreshold
    {
        Overfed,
        Okay,
        Peckish,
        Starving,
        Dead,
    }

    [NetSerializable, Serializable]
    public class HungerStateMessage : ComponentMessage
    {
        public readonly HungerThreshold Threshold;

        public HungerStateMessage(HungerThreshold threshold)
        {
            Threshold = threshold;
        }
    }
    public class SharedHungerComponent : Component
    {
        public override string Name => "Hunger";
        public override uint? NetID => ContentNetIDs.HUNGER;
    }
}

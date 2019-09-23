using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Nutrition
{
    [NetSerializable, Serializable]
    public class ThirstStateMessage : ComponentMessage
    {
        public readonly ThirstThreshold Threshold;

        public ThirstStateMessage(ThirstThreshold threshold)
        {
            Threshold = threshold;
        }
    }

    [NetSerializable, Serializable]
    public enum ThirstThreshold
    {
        // Hydrohomies
        OverHydrated,
        Okay,
        Thirsty,
        Parched,
        Dead,
    }

    public class SharedThirstComponent : Component
    {
        public override string Name => "Thirst";
        public override uint? NetID => ContentNetIDs.THIRST;
    }
}

using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Nutrition
{
    public class SharedSmokingComponent : Component
    {
        public override string Name => "Smoking";
    }

    [Serializable, NetSerializable]
    public enum SmokingVisuals
    {
        Smoking,
    }

    [Serializable, NetSerializable]
    public enum SmokingStates
    {
        Unlit,
        Lit,
        Burnt,
    }
}

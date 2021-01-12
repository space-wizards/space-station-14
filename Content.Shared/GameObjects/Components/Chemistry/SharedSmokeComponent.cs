using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SharedSmokeComponent : Component
    {
        public override string Name => "Smoke";
    }
    [Serializable, NetSerializable]
    public enum SmokeVisuals
    {
        Color
    }
}

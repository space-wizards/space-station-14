using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedGasSprayerComponent : Component
    {
        public sealed override string Name => "GasSprayer";
    }

    [Serializable, NetSerializable]
    public enum ExtinguisherVisuals
    {
        Rotation
    }
}

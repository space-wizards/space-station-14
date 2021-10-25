using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Extinguisher
{
    public abstract class SharedFireExtinguisherComponent : Component
    {
        public override string Name => "FireExtinguisher";
    }

    [Serializable, NetSerializable]
    public enum FireExtinguisherVisuals : byte
    {
        Safety
    }
}

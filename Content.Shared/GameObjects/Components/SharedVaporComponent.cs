using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedVaporComponent : Component
    {
        public override string Name => "Vapor";
    }

    [Serializable, NetSerializable]
    public enum VaporVisuals
    {
        Rotation,
        Color,
        State,
    }
}

#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Vapor
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

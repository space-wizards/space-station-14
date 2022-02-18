using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Vapor
{
    [Virtual]
    public class SharedVaporComponent : Component
    {
        public const string SolutionName = "vapor";
    }

    [Serializable, NetSerializable]
    public enum VaporVisuals
    {
        Rotation,
        Color,
        State,
    }
}

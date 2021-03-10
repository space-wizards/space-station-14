#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedComputerComponent : Component
    {
        public override string Name => "Computer";
    }

    [Serializable, NetSerializable]
    public enum ComputerVisuals
    {
        // Bool
        Powered,

        // Bool
        Broken
    }
}

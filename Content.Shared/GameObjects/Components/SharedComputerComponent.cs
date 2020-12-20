using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum ComputerVisuals
    {
        // Bool
        Powered,

        // Bool
        Broken
    }
}

using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.WallStorage
{
    [Serializable, NetSerializable]
    public enum WallStorageStatus
    {
        Full,
        Empty,
    }
}
